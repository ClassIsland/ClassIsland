import CoreFoundation
import Darwin
import Foundation
import ObjectiveC
import UIKit

// MARK: - C ABI

// 私有 libSystem 符号。用 @_silgen_name 声明而不是链接私有 framework：
// 这些符号位于 libSystem 中，公开 SDK 头文件里没有声明。
@_silgen_name("posix_spawnattr_set_persona_np")
private func ci_posix_spawnattr_set_persona_np(
    _ attributes: UnsafeMutablePointer<posix_spawnattr_t?>,
    _ personaId: uid_t,
    _ flags: UInt32
) -> Int32

@_silgen_name("posix_spawnattr_set_persona_uid_np")
private func ci_posix_spawnattr_set_persona_uid_np(
    _ attributes: UnsafeMutablePointer<posix_spawnattr_t?>,
    _ personaId: uid_t
) -> Int32

@_silgen_name("posix_spawnattr_set_persona_gid_np")
private func ci_posix_spawnattr_set_persona_gid_np(
    _ attributes: UnsafeMutablePointer<posix_spawnattr_t?>,
    _ personaId: uid_t
) -> Int32

@_silgen_name("proc_pidpath")
private func ci_proc_pidpath(
    _ pid: Int32,
    _ buffer: UnsafeMutableRawPointer?,
    _ bufferSize: UInt32
) -> Int32

@_silgen_name("notify_post")
private func ci_notify_post(_ name: UnsafePointer<CChar>) -> UInt32

@_silgen_name("notify_register_dispatch")
private func ci_notify_register_dispatch(
    _ name: UnsafePointer<CChar>,
    _ outToken: UnsafeMutablePointer<Int32>,
    _ queue: DispatchQueue?,
    _ handler: @convention(block) (Int32) -> Void
) -> UInt32

// MARK: - 约定

/// 必须与 ClassIslandHudBridge.h 中的 CI_HUD_ERR_* 常量保持一致。
private enum HudError: Int32 {
    case ok = 0
    case invalidArgument = -1
    case executableUnavailable = -2
    case spawnFailed = -3
    case rootPersonaUnavailable = -4
    case terminationForbidden = -5
    case internalFailure = -6
}

private enum HudConstants {
    static let markerFileName = "classisland-hud.pid"
    /// persona 99 是 XNU 的 superuser persona；root spawn 的公开示例（TrollStore/
    /// TrollRecorder/FloatingBall）都使用该 id。
    static let superuserPersonaID: uid_t = 99
    static let superuserPersonaFlags: UInt32 = 1
    static let targetWindowLevel: CGFloat = 10_000_001
    static let springBoardServicesPath =
        "/System/Library/PrivateFrameworks/SpringBoardServices.framework/SpringBoardServices"
    static let hostingControllerClassName = "SBSAccessibilityWindowHostingController"
    static let stopGracePeriod: TimeInterval = 0.5
    static let killGracePeriod: TimeInterval = 0.2
    static let dismissalGracePeriod: TimeInterval = 0.6
    static let stopHelperGracePeriod: TimeInterval = 2.0
    static let pollInterval: TimeInterval = 0.02
    static let parentPollInterval: TimeInterval = 0.5
    static let errorBufferSize = 1024
    static let processPathBufferSize = 4096
}

// MARK: - 错误信息

private let hudErrorBuffer: UnsafeMutablePointer<CChar> = {
    let buffer = UnsafeMutablePointer<CChar>.allocate(capacity: HudConstants.errorBufferSize)
    buffer.initialize(repeating: 0, count: HudConstants.errorBufferSize)
    return buffer
}()
private let hudErrorLock = NSLock()

/// 覆盖 last_error。消息为 UTF-8 诊断文案，调用方不负责释放。
private func hudSetLastError(_ message: String) {
    hudErrorLock.lock()
    defer { hudErrorLock.unlock() }
    memset(hudErrorBuffer, 0, HudConstants.errorBufferSize)
    message.withCString { source in
        _ = strncpy(hudErrorBuffer, source, HudConstants.errorBufferSize - 1)
    }
}

private func hudClearLastError() {
    hudErrorLock.lock()
    defer { hudErrorLock.unlock() }
    hudErrorBuffer.pointee = 0
}

// MARK: - PID marker 与进程状态

private let hudStateLock = NSLock()
private var hudChildPid: pid_t = 0

private func hudMarkerURL() -> URL {
    URL(fileURLWithPath: NSTemporaryDirectory(), isDirectory: true)
        .appendingPathComponent(HudConstants.markerFileName)
}

private func hudRemoveMarker() {
    try? FileManager.default.removeItem(at: hudMarkerURL())
}

/// 读取 marker 中的 pid。跨重启遗留的 marker 会按文件创建时间识别并清理，
/// 避免复用重启前的 pid 误伤无关进程。
private func hudReadMarkerPid() -> pid_t? {
    let url = hudMarkerURL()
    guard let attributes = try? FileManager.default.attributesOfItem(atPath: url.path),
          let creationDate = attributes[.creationDate] as? Date else {
        return nil
    }

    let bootDate = Date(timeIntervalSinceNow: -ProcessInfo.processInfo.systemUptime)
    if creationDate < bootDate.addingTimeInterval(-60) {
        hudRemoveMarker()
        return nil
    }

    guard let data = try? Data(contentsOf: url),
          let text = String(data: data, encoding: .utf8)?
            .trimmingCharacters(in: .whitespacesAndNewlines),
          let pid = Int32(text), pid > 0 else {
        return nil
    }
    return pid
}

/// 写入 marker。返回非 nil 表示启动成功但 marker 不可写（非致命警告）。
private func hudWriteMarker(_ pid: pid_t) -> String? {
    do {
        try Data("\(pid)\n".utf8).write(to: hudMarkerURL(), options: .atomic)
        return nil
    } catch {
        return "HUD 已启动 (pid=\(pid))，但 pid marker 写入失败：\(error.localizedDescription)。"
    }
}

/// 回收已经退出的子进程（避免僵尸），返回 true 表示该 pid 已退出并被回收。
private func hudReapChildIfExited(_ pid: pid_t) -> Bool {
    var status: Int32 = 0
    return waitpid(pid, &status, WNOHANG) == pid
}

/// kill(pid, 0) 判定：EPERM 说明进程存在但当前进程无权动它（root HUD 的常态）。
private func hudIsProcessAlive(_ pid: pid_t) -> Bool {
    if hudReapChildIfExited(pid) {
        return false
    }
    if kill(pid, 0) == 0 {
        return true
    }
    return errno == EPERM
}

/// 用 proc_pidpath 做 best-effort 身份核对。宿主 App 可能无权读取 root 子进程信息
/// （返回 <= 0），此时按“是”处理，宁可复用也不重复 spawn。
private func hudProcessLooksLikeCurrentExecutable(_ pid: pid_t) -> Bool {
    guard let executablePath = Bundle.main.executablePath, !executablePath.isEmpty else {
        return true
    }
    let executableName = URL(fileURLWithPath: executablePath).lastPathComponent

    var buffer = [CChar](repeating: 0, count: HudConstants.processPathBufferSize)
    let length = buffer.withUnsafeMutableBytes { rawBuffer in
        ci_proc_pidpath(pid, rawBuffer.baseAddress, UInt32(rawBuffer.count))
    }
    guard length > 0 else {
        return true
    }
    return URL(fileURLWithPath: String(cString: buffer)).lastPathComponent == executableName
}

/// 必须在持有 hudStateLock 时调用。
private func hudResolveRunningPidLocked() -> pid_t {
    var candidate = hudChildPid
    if let markerPid = hudReadMarkerPid() {
        candidate = markerPid
    }

    guard candidate > 0 else {
        hudChildPid = 0
        return 0
    }

    if hudIsProcessAlive(candidate), hudProcessLooksLikeCurrentExecutable(candidate) {
        hudChildPid = candidate
        return candidate
    }

    hudRemoveMarker()
    if hudChildPid == candidate {
        hudChildPid = 0
    }
    return 0
}

// MARK: - 启动 HUD 子进程

@_cdecl("ci_hud_start")
public func ci_hud_start(_ spawnAsRoot: Int32, _ dataDirUTF8: UnsafePointer<CChar>?) -> Int32 {
    guard let dataDirUTF8 else {
        hudSetLastError("ci_hud_start: data_dir_utf8 不能为 NULL。")
        return HudError.invalidArgument.rawValue
    }
    let dataDir = String(cString: dataDirUTF8)
    guard !dataDir.isEmpty else {
        hudSetLastError("ci_hud_start: data_dir_utf8 不能为空字符串。")
        return HudError.invalidArgument.rawValue
    }

    hudStateLock.lock()
    defer { hudStateLock.unlock() }

    if hudResolveRunningPidLocked() > 0 {
        hudClearLastError()
        return HudError.ok.rawValue
    }
    return hudStartProcessLocked(spawnAsRoot: spawnAsRoot != 0, dataDir: dataDir)
}

/// 必须在持有 hudStateLock 时调用。
private func hudStartProcessLocked(spawnAsRoot: Bool, dataDir: String) -> Int32 {
    guard let executablePath = Bundle.main.executablePath, !executablePath.isEmpty else {
        hudSetLastError("ci_hud_start: 无法取得当前 App 的可执行文件路径 (Bundle.main.executablePath)。")
        return HudError.executableUnavailable.rawValue
    }

    var isDirectory: ObjCBool = false
    guard FileManager.default.fileExists(atPath: executablePath, isDirectory: &isDirectory),
          !isDirectory.boolValue else {
        hudSetLastError("ci_hud_start: 可执行文件不可用：\(executablePath)。")
        return HudError.executableUnavailable.rawValue
    }

    var warnings: [String] = []
    do {
        // HUD 子进程以 root 运行，子进程自己的容器目录未必可写；数据目录由宿主指定，
        // 这里提前创建，避免 HUD 启动早期因为目录不存在而失败。
        try FileManager.default.createDirectory(
            atPath: dataDir,
            withIntermediateDirectories: true
        )
    } catch {
        warnings.append("HUD 数据目录创建失败 (\(dataDir))：\(error.localizedDescription)。")
    }

    var attributes: posix_spawnattr_t?
    guard posix_spawnattr_init(&attributes) == 0 else {
        hudSetLastError("ci_hud_start: posix_spawnattr_init 失败。")
        return HudError.internalFailure.rawValue
    }
    defer { posix_spawnattr_destroy(&attributes) }

    if spawnAsRoot {
        let personaResult = ci_posix_spawnattr_set_persona_np(
            &attributes,
            HudConstants.superuserPersonaID,
            HudConstants.superuserPersonaFlags
        )
        let uidResult = ci_posix_spawnattr_set_persona_uid_np(&attributes, 0)
        let gidResult = ci_posix_spawnattr_set_persona_gid_np(&attributes, 0)
        guard personaResult == 0, uidResult == 0, gidResult == 0 else {
            hudSetLastError(
                "ci_hud_start: 设置 root persona 失败 (persona=\(personaResult), "
                    + "uid=\(uidResult), gid=\(gidResult))；该能力需要 "
                    + "com.apple.private.persona-mgmt entitlement。"
            )
            return HudError.rootPersonaUnavailable.rawValue
        }
    }

    posix_spawnattr_setpgroup(&attributes, 0)
    posix_spawnattr_setflags(&attributes, Int16(POSIX_SPAWN_SETPGROUP))

    // HUD 子进程与宿主 App 是同一个可执行文件，靠 argv/env 区分身份。
    var environment = ProcessInfo.processInfo.environment
    environment["CLASSISLAND_HUD"] = "1"
    environment["CLASSISLAND_HUD_DATA_DIR"] = dataDir
    environment["CLASSISLAND_HUD_PARENT_PID"] = String(getpid())
    // 子进程由 posix_spawn 直接执行，极端情况下 Bundle.main 识别不到 App bundle；
    // 把宿主计算出的通知名传下去，保证 root 子进程无法被 kill 时的兜底退出通道一致。
    environment["CLASSISLAND_HUD_DISMISSAL_NOTIFY"] = hudDismissalNotificationName

    let argumentStrings = [executablePath, "--hud"]
    let environmentStrings = environment
        .map { "\($0.key)=\($0.value)" }
        .sorted()
    let arguments = argumentStrings.map { strdup($0) }
    let environmentPointers = environmentStrings.map { strdup($0) }
    defer {
        for case let pointer? in arguments {
            free(pointer)
        }
        for case let pointer? in environmentPointers {
            free(pointer)
        }
    }

    var pid: pid_t = 0
    let spawnResult = executablePath.withCString { path in
        posix_spawn(
            &pid,
            path,
            nil,
            &attributes,
            arguments + [nil],
            environmentPointers + [nil]
        )
    }
    guard spawnResult == 0, pid > 0 else {
        let failureReason = String(cString: strerror(spawnResult))
        hudSetLastError(
            "ci_hud_start: posix_spawn 失败 (errno=\(spawnResult): \(failureReason))，"
                + "spawn_as_root=\(spawnAsRoot)。"
        )
        return HudError.spawnFailed.rawValue
    }

    hudChildPid = pid
    if warnings.isEmpty {
        hudClearLastError()
    } else {
        hudSetLastError(warnings.joined(separator: " "))
    }
    if let markerWarning = hudWriteMarker(pid) {
        hudSetLastError((warnings + [markerWarning]).joined(separator: " "))
    }
    return HudError.ok.rawValue
}

// MARK: - 停止 HUD 子进程

private enum HudTerminationResult {
    case terminated
    case alreadyGone
    case forbidden(String)
}

@_cdecl("ci_hud_stop")
public func ci_hud_stop() -> Int32 {
    hudStateLock.lock()
    defer { hudStateLock.unlock() }

    let pid = hudResolveRunningPidLocked()
    guard pid > 0 else {
        hudRemoveMarker()
        hudChildPid = 0
        hudClearLastError()
        return 0
    }

    switch hudTerminateProcess(pid) {
    case .terminated, .alreadyGone:
        hudRemoveMarker()
        hudChildPid = 0
        hudClearLastError()
        return 1
    case let .forbidden(reason):
        hudSetLastError("ci_hud_stop: HUD 进程 (pid=\(pid)) 无法终止：\(reason)。")
        return HudError.terminationForbidden.rawValue
    }
}

private func hudWaitForProcessExit(_ pid: pid_t, timeout: TimeInterval) -> Bool {
    let deadline = Date().addingTimeInterval(timeout)
    repeat {
        if !hudIsProcessAlive(pid) {
            return true
        }
        Thread.sleep(forTimeInterval: HudConstants.pollInterval)
    } while Date() < deadline
    return !hudIsProcessAlive(pid)
}

private func hudPostDismissalNotification() -> Bool {
    let notificationName = hudDismissalNotificationName
    return notificationName.withCString { ci_notify_post($0) == 0 }
}

/// 以 root persona 启动一个极小的同包 stop helper，处理旧版 HUD 未安装退出通知的情况。
private func hudSpawnRootStopHelper(_ targetPid: pid_t) -> Bool {
    guard let executablePath = Bundle.main.executablePath, !executablePath.isEmpty else {
        return false
    }

    var attributes: posix_spawnattr_t?
    guard posix_spawnattr_init(&attributes) == 0 else {
        return false
    }
    defer { posix_spawnattr_destroy(&attributes) }

    let personaResult = ci_posix_spawnattr_set_persona_np(
        &attributes,
        HudConstants.superuserPersonaID,
        HudConstants.superuserPersonaFlags
    )
    let uidResult = ci_posix_spawnattr_set_persona_uid_np(&attributes, 0)
    let gidResult = ci_posix_spawnattr_set_persona_gid_np(&attributes, 0)
    guard personaResult == 0, uidResult == 0, gidResult == 0 else {
        return false
    }

    let argumentStrings = [executablePath, "--hud-stop", String(targetPid)]
    let arguments = argumentStrings.map { strdup($0) }
    defer {
        for case let pointer? in arguments {
            free(pointer)
        }
    }

    var helperPid: pid_t = 0
    let spawnResult = executablePath.withCString { path in
        posix_spawn(
            &helperPid,
            path,
            nil,
            &attributes,
            arguments + [nil],
            environ
        )
    }
    guard spawnResult == 0, helperPid > 0 else {
        return false
    }

    // root helper 需要加载一次 .NET runtime；必须设置上限，避免主 App 的 UI 线程
    // 因旧 HUD 回收失败而永久卡在 EarlyLoading。
    if hudWaitForProcessExit(helperPid, timeout: HudConstants.stopHelperGracePeriod) {
        return true
    }

    // helper 自身卡住时尽力终止，但不能再等待它，否则会重新引入无限阻塞。
    _ = kill(helperPid, SIGTERM)
    return false
}

private func hudTerminateProcess(_ pid: pid_t) -> HudTerminationResult {
    if !hudIsProcessAlive(pid) {
        return .alreadyGone
    }

    var permissionDenied = false
    if kill(pid, SIGTERM) == 0 {
        if hudWaitForProcessExit(pid, timeout: HudConstants.stopGracePeriod) {
            return .terminated
        }
    } else if errno == ESRCH {
        return .alreadyGone
    } else {
        permissionDenied = errno == EPERM
    }

    if hudIsProcessAlive(pid) {
        if kill(pid, SIGKILL) == 0 {
            if hudWaitForProcessExit(pid, timeout: HudConstants.killGracePeriod) {
                return .terminated
            }
        } else if errno == ESRCH {
            return .alreadyGone
        } else if errno == EPERM {
            permissionDenied = true
        }
    } else {
        return .terminated
    }

    // 宿主 App 以 mobile 运行、HUD 以 root 运行时 kill 必然 EPERM。此时通过 darwin
    // notification 让 HUD 子进程自行退出（TrollRecorder TRHud 的退出机制）。
    if hudPostDismissalNotification() {
        if hudWaitForProcessExit(pid, timeout: HudConstants.dismissalGracePeriod) {
            return .terminated
        }
    }

    if hudSpawnRootStopHelper(pid), hudWaitForProcessExit(pid, timeout: HudConstants.killGracePeriod) {
        return .terminated
    }

    if permissionDenied {
        return .forbidden(
            "当前进程无权向 root HUD 发送信号 (EPERM)，且 HUD 未响应 dismissal 通知"
        )
    }
    return .forbidden("HUD 在 SIGTERM/SIGKILL 与 dismissal 通知后仍未退出")
}

// MARK: - 查询接口

@_cdecl("ci_hud_is_running")
public func ci_hud_is_running() -> Int32 {
    hudStateLock.lock()
    defer { hudStateLock.unlock() }
    return hudResolveRunningPidLocked() > 0 ? 1 : 0
}

@_cdecl("ci_hud_get_pid")
public func ci_hud_get_pid() -> Int32 {
    hudStateLock.lock()
    defer { hudStateLock.unlock() }
    return hudResolveRunningPidLocked()
}

@_cdecl("ci_hud_last_error")
public func ci_hud_last_error() -> UnsafePointer<CChar>? {
    UnsafePointer(hudErrorBuffer)
}

// MARK: - SpringBoard accessibility window 注册

/// 窗口在分配时就是 HUD 子类（由 ci_hud_create_window 创建），UIKit 会按系统窗口处理：
/// - `_ignoresHitTest` 返回 true：窗口不参与命中测试，点击穿透到下面的 App；
/// - `_isWindowServerHostingManaged` 返回 false：窗口上下文不归本进程的
///   WindowServer 托管，而是交给 SpringBoard 的 accessibility window hosting；
/// - `_isSystemWindow` 返回 true：按系统窗口处理（悬浮于其他 App 之上）。
/// 注意不要声明 `_isSecure` / `_shouldCreateContextAsSecure`：secure CAContext 会
/// 禁止 Metal drawable，Avalonia（Metal/OpenGL 渲染）会画不出内容；XExternalHUD 的
/// 经验也是“secure context = no Metal drawable”。悬浮层不需要防截屏。
/// 私有方法不在 SDK 头文件中，Swift 无法用 override 声明，直接声明同 selector 的
/// @objc 方法即可在运行时被 ObjC 消息派发命中。
@objc(ClassIslandHudPassthroughWindow)
private final class ClassIslandHudPassthroughWindow: UIWindow {
    @objc func _ignoresHitTest() -> Bool { true }

    @objc func _isWindowServerHostingManaged() -> Bool { false }

    @objc class func _isSystemWindow() -> Bool { true }
}

/// 完成窗口属性设置。系统窗口/非托管窗口标志在上屏前生效，UIKit 才会在窗口上屏时
/// 建立可被 SpringBoard accessibility window hosting 托管的 CAContext。
private func hudPrepareWindow(_ window: UIWindow) {
    if object_getClass(window) != ClassIslandHudPassthroughWindow.self {
        object_setClass(window, ClassIslandHudPassthroughWindow.self)
    }
    window.windowLevel = UIWindow.Level(rawValue: HudConstants.targetWindowLevel)
    window.backgroundColor = .clear
    window.isOpaque = false
    window.clipsToBounds = true
}

@_cdecl("ci_hud_prepare_window")
public func ci_hud_prepare_window(_ uiWindow: UnsafeMutableRawPointer?) {
    guard let uiWindow else {
        hudSetLastError("ci_hud_prepare_window: ui_window 不能为 NULL。")
        return
    }
    hudRunOnMainThread {
        let object = Unmanaged<AnyObject>.fromOpaque(uiWindow).takeUnretainedValue()
        guard let window = object as? UIWindow else {
            hudSetLastError("ci_hud_prepare_window: 传入的指针不是 UIWindow。")
            return
        }
        hudPrepareWindow(window)
        hudClearLastError()
    }
}

private var hudCreatedWindow: UIWindow?

/// 直接以 ClassIslandHudPassthroughWindow 类 alloc/init 出 HUD 窗口，而不是先创建
/// 普通 UIWindow 再 object_setClass。参考实现（TrollSpeed/TRHud/XExternalHUD）都在
/// 分配时就使用 HUD 窗口子类，UIKit 才会在窗口上屏时按系统窗口建立可托管的 CAContext。
@_cdecl("ci_hud_create_window")
public func ci_hud_create_window(
    _ frameX: Double,
    _ frameY: Double,
    _ width: Double,
    _ height: Double
) -> UnsafeMutableRawPointer? {
    var result: UnsafeMutableRawPointer?
    hudRunOnMainThread {
        let window = ClassIslandHudPassthroughWindow(
            frame: CGRect(x: frameX, y: frameY, width: width, height: height)
        )
        hudPrepareWindow(window)
        hudCreatedWindow = window
        result = Unmanaged<UIWindow>.passUnretained(window).toOpaque()
        hudClearLastError()
    }
    return result
}

private let hudRegistrationLock = NSLock()
private var hudRegisteredWindow: UIWindow?
private var hudRegisteredWindowPointer: UnsafeMutableRawPointer?
private var hudWindowHostingController: NSObject?
private var hudSpringBoardServicesHandle: UnsafeMutableRawPointer?

private typealias HudObjectGetter = @convention(c) (AnyObject, Selector) -> AnyObject?
private typealias HudVoidBoolSetter = @convention(c) (AnyObject, Selector, Bool) -> Void
private typealias HudUInt32Getter = @convention(c) (AnyObject, Selector) -> UInt32
private typealias HudRegisterWindow = @convention(c) (AnyObject, Selector, UInt32, CGFloat) -> Void

private func hudMethodIMP(_ object: AnyObject, _ selector: Selector) -> IMP? {
    guard let objectClass = object_getClass(object),
          class_respondsToSelector(objectClass, selector) else {
        return nil
    }
    return class_getMethodImplementation(objectClass, selector)
}

private func hudObjectProperty(_ object: AnyObject, selector name: String) -> AnyObject? {
    let selector = NSSelectorFromString(name)
    guard let imp = hudMethodIMP(object, selector) else {
        return nil
    }
    return unsafeBitCast(imp, to: HudObjectGetter.self)(object, selector)
}

private func hudUInt32Property(_ object: AnyObject, selector name: String) -> UInt32? {
    let selector = NSSelectorFromString(name)
    guard let imp = hudMethodIMP(object, selector) else {
        return nil
    }
    return unsafeBitCast(imp, to: HudUInt32Getter.self)(object, selector)
}

@discardableResult
private func hudCallVoidBool(
    on object: AnyObject,
    selector name: String,
    value: Bool
) -> Bool {
    let selector = NSSelectorFromString(name)
    guard let imp = hudMethodIMP(object, selector) else {
        return false
    }
    unsafeBitCast(imp, to: HudVoidBoolSetter.self)(object, selector, value)
    return true
}

/// 优先取 UIWindow 的 `_contextId`（TrollRecorder/FloatingBall 的用法），
/// 某些系统版本上退回 CAContext 的 `contextId`。
private func hudWindowContextID(_ window: UIWindow) -> UInt32? {
    if let contextID = hudUInt32Property(window, selector: "_contextId"), contextID != 0 {
        return contextID
    }
    if let boundContext = hudObjectProperty(window, selector: "_boundContext"),
       let contextID = hudUInt32Property(boundContext, selector: "contextId"),
       contextID != 0 {
        return contextID
    }
    return nil
}

private func hudLoadSpringBoardServices() -> Bool {
    if hudSpringBoardServicesHandle != nil {
        return true
    }
    guard let handle = dlopen(HudConstants.springBoardServicesPath, RTLD_NOW) else {
        let reason = dlerror().map { String(cString: $0) } ?? "unknown dlopen error"
        hudSetLastError("ci_hud_register_window: dlopen SpringBoardServices 失败：\(reason)。")
        return false
    }
    hudSpringBoardServicesHandle = handle
    return true
}

private func hudRunOnMainThread(_ body: () -> Void) {
    if Thread.isMainThread {
        body()
        return
    }
    DispatchQueue.main.sync(execute: body)
}

@_cdecl("ci_hud_register_window")
public func ci_hud_register_window(_ uiWindow: UnsafeMutableRawPointer?) {
    guard let uiWindow else {
        hudSetLastError("ci_hud_register_window: ui_window 不能为 NULL。")
        return
    }
    hudRunOnMainThread {
        hudRegisterWindowOnMainThread(uiWindow)
    }
}

private func hudRegisterWindowOnMainThread(_ windowPointer: UnsafeMutableRawPointer) {
    hudInstallHudChildLifecycleObserversIfNeeded()

    let object = Unmanaged<AnyObject>.fromOpaque(windowPointer).takeUnretainedValue()
    guard let window = object as? UIWindow else {
        hudSetLastError("ci_hud_register_window: 传入的指针不是 UIWindow。")
        return
    }

    hudRegistrationLock.lock()
    defer { hudRegistrationLock.unlock() }

    if hudRegisteredWindowPointer == windowPointer, hudWindowHostingController != nil {
        hudClearLastError()
        return
    }

    if object_getClass(window) != ClassIslandHudPassthroughWindow.self {
        object_setClass(window, ClassIslandHudPassthroughWindow.self)
    }

    hudPrepareWindow(window)

    // 只保留 _boundContext 用作诊断信息：secure CAContext 会禁止 Metal drawable，
    // 不调用 setSecure:（Avalonia 需要非 secure 上下文才能渲染）。
    let hasBoundContext = hudObjectProperty(window, selector: "_boundContext") != nil

    guard hudLoadSpringBoardServices() else {
        return
    }
    guard let controllerClass =
        NSClassFromString(HudConstants.hostingControllerClassName) as? NSObject.Type else {
        hudSetLastError(
            "ci_hud_register_window: 找不到 "
                + "\(HudConstants.hostingControllerClassName)（当前系统不支持 accessibility "
                + "window hosting）。"
        )
        return
    }
    let hostingController = controllerClass.init()

    guard let contextID = hudWindowContextID(window) else {
        hudSetLastError(
            "ci_hud_register_window: 无法取得 UIWindow 的 CAContext ID"
                + "（窗口可能尚未上屏），_boundContext=\(hasBoundContext ? "有" : "无")。"
        )
        return
    }

    let registerSelector = NSSelectorFromString("registerWindowWithContextID:atLevel:")
    guard let registerIMP = hudMethodIMP(hostingController, registerSelector) else {
        hudSetLastError(
            "ci_hud_register_window: "
                + "\(HudConstants.hostingControllerClassName) 不支持 "
                + "registerWindowWithContextID:atLevel:。"
        )
        return
    }
    let register = unsafeBitCast(registerIMP, to: HudRegisterWindow.self)
    register(hostingController, registerSelector, contextID, window.windowLevel.rawValue)

    hudRegisteredWindow = window
    hudRegisteredWindowPointer = windowPointer
    hudWindowHostingController = hostingController
    hudClearLastError()
}

// MARK: - HUD 子进程生命周期

private let hudLifecycleQueue = DispatchQueue(
    label: "cn.classisland.ios.hud.lifecycle"
)
private var hudChildLifecycleObserversInstalled = false
private var hudDismissalNotificationToken: Int32 = 0
private var hudParentWatchdogTimer: DispatchSourceTimer?

private var hudDismissalNotificationName: String {
    (Bundle.main.bundleIdentifier ?? "cn.classisland.ios") + ".hud.dismissal"
}

/// 仅在 HUD 子进程（env CLASSISLAND_HUD=1）内安装：
/// - 响应宿主的 <bundle-id>.hud.dismissal 通知后 exit(0)（宿主 mobile、HUD root
///   时宿主无法 kill 子进程，这是 ci_hud_stop 的兜底路径）；
/// - 轮询宿主 pid，宿主退出后 HUD 随父进程退出，避免残留 root 进程。
private func hudInstallHudChildLifecycleObserversIfNeeded() {
    guard !hudChildLifecycleObserversInstalled else {
        return
    }
    hudChildLifecycleObserversInstalled = true

    let environment = ProcessInfo.processInfo.environment
    guard environment["CLASSISLAND_HUD"] == "1" else {
        return
    }

    let notificationName = environment["CLASSISLAND_HUD_DISMISSAL_NOTIFY"]
        ?? hudDismissalNotificationName
    var token: Int32 = 0
    notificationName.withCString { name in
        // _exit 而不是 exit：HUD 是被宿主强行终止的一方，跳过 atexit/托管运行时清理，
        // 保证 ci_hud_stop 的等待窗口内进程一定消失（宿主随后会重新 spawn 新的 HUD）。
        let status = ci_notify_register_dispatch(name, &token, hudLifecycleQueue) { _ in
            _exit(EXIT_SUCCESS)
        }
        if status == 0 {
            hudDismissalNotificationToken = token
        }
    }

    guard let parentPidValue = environment["CLASSISLAND_HUD_PARENT_PID"],
          let parentPid = Int32(parentPidValue),
          parentPid > 0 else {
        return
    }

    let timer = DispatchSource.makeTimerSource(queue: hudLifecycleQueue)
    timer.schedule(
        deadline: .now() + HudConstants.parentPollInterval,
        repeating: HudConstants.parentPollInterval,
        leeway: .milliseconds(200)
    )
    timer.setEventHandler {
        if kill(parentPid, 0) != 0, errno == ESRCH {
            _exit(EXIT_SUCCESS)
        }
    }
    timer.resume()
    hudParentWatchdogTimer = timer
}

/// 在 HUD 子进程进入 UIKit/Avalonia 初始化前安装生命周期观察者。
/// 不能等到窗口注册后再安装，否则 HUD 若卡在建窗阶段将无法被宿主终止。
@_cdecl("ci_hud_initialize_child_lifecycle")
public func ci_hud_initialize_child_lifecycle() {
    hudInstallHudChildLifecycleObserversIfNeeded()
}

@_cdecl("ci_hud_stop_target")
public func ci_hud_stop_target(_ pid: Int32) -> Int32 {
    guard pid > 0, pid != getpid() else {
        return HudError.invalidArgument.rawValue
    }

    if kill(pid_t(pid), SIGKILL) == 0 || errno == ESRCH {
        return HudError.ok.rawValue
    }

    return HudError.terminationForbidden.rawValue
}

// MARK: - HUD 子进程手工 UIKit 启动（run as plugin）

// HUD 子进程由主 App 通过 posix_spawn 拉起，没有 SpringBoard/launchd 分配的
// UIApplication 启动上下文。直接调用 UIApplicationMain 只能得到一个没有 WindowServer
// 连接的“空壳”应用：UIWindow 永远拿不到 CAContext（_boundContext），因此无法注册到
// SpringBoard，悬浮窗口也就永远不会出现。
//
// TrollSpeed / TRHud / XExternalHUD 等已验证实现的做法是手工初始化
// GraphicsServices/BackBoard，用 __completeAndRunAsPlugin 完成启动，再自行跑 run loop。
// 本函数按同样流程启动（输入事件部分省略：ClassIsland 的悬浮岛是只读、点击穿透的）。
private typealias HudVoidProc = @convention(c) () -> Void
private typealias HudAnyClassProc = @convention(c) (AnyClass) -> Void

private let hudPrivateFrameworkPaths = [
    "/System/Library/PrivateFrameworks/GraphicsServices.framework/GraphicsServices",
    "/System/Library/PrivateFrameworks/BackBoardServices.framework/BackBoardServices",
    "/System/Library/PrivateFrameworks/SpringBoardServices.framework/SpringBoardServices",
]

/// 解析私有 framework 导出的 C 符号；UIKit 已在进程中，最后回退到 RTLD_DEFAULT。
private func hudResolvePrivateSymbol(_ name: String) -> UnsafeMutableRawPointer? {
    for path in hudPrivateFrameworkPaths {
        if let handle = dlopen(path, RTLD_LAZY), let symbol = dlsym(handle, name) {
            return symbol
        }
    }
    // RTLD_DEFAULT 等价于 (void *)-2
    return dlsym(UnsafeMutableRawPointer(bitPattern: -2), name)
}

@_cdecl("ci_hud_run_as_plugin")
public func ci_hud_run_as_plugin(_ appDelegatePointer: UnsafeMutableRawPointer?) -> Int32 {
    guard let appDelegatePointer else {
        hudSetLastError("ci_hud_run_as_plugin: app_delegate 不能为 NULL。")
        return HudError.invalidArgument.rawValue
    }

    let requiredSymbols = [
        "GSInitialize",
        "BKSDisplayServicesStart",
        "UIApplicationInitialize",
        "UIApplicationInstantiateSingleton",
    ]
    var symbols: [String: UnsafeMutableRawPointer] = [:]
    var missing: [String] = []
    for name in requiredSymbols {
        if let symbol = hudResolvePrivateSymbol(name) {
            symbols[name] = symbol
        } else {
            missing.append(name)
        }
    }
    let completionSelector = NSSelectorFromString("__completeAndRunAsPlugin")
    if class_getInstanceMethod(UIApplication.self, completionSelector) == nil {
        missing.append("-[UIApplication __completeAndRunAsPlugin]")
    }
    guard missing.isEmpty else {
        // 预检查失败时还没有初始化 UIKit，调用方可以安全回退到标准 UIApplicationMain。
        hudSetLastError("ci_hud_run_as_plugin: 缺少私有符号：\(missing.joined(separator: ", "))。")
        return HudError.internalFailure.rawValue
    }

    let delegateObject = Unmanaged<AnyObject>.fromOpaque(appDelegatePointer).takeUnretainedValue()
    guard let appDelegate = delegateObject as? UIApplicationDelegate else {
        hudSetLastError("ci_hud_run_as_plugin: 传入的 AppDelegate 不实现 UIApplicationDelegate。")
        return HudError.invalidArgument.rawValue
    }

    // 与 TrollSpeed/TRHud/XExternalHUD 一致的手工启动顺序。
    _ = UIScreen.main
    _ = CFRunLoopGetCurrent()

    unsafeBitCast(symbols["GSInitialize"]!, to: HudVoidProc.self)()
    unsafeBitCast(symbols["BKSDisplayServicesStart"]!, to: HudVoidProc.self)()
    unsafeBitCast(symbols["UIApplicationInitialize"]!, to: HudVoidProc.self)()
    unsafeBitCast(
        symbols["UIApplicationInstantiateSingleton"]!,
        to: HudAnyClassProc.self
    )(UIApplication.self)

    UIApplication.shared.delegate = appDelegate
    let accessibilitySelector = NSSelectorFromString("_accessibilityInit")
    if UIApplication.shared.responds(to: accessibilitySelector) {
        _ = UIApplication.shared.perform(accessibilitySelector)
    }

    _ = RunLoop.current
    _ = UIApplication.shared.perform(completionSelector)
    hudClearLastError()
    CFRunLoopRun()
    return HudError.ok.rawValue
}
