import AVFoundation
import AVKit
import CoreMedia
import CoreVideo
import Foundation
import UIKit

// MARK: - 约定

/// 画中画悬浮课表的 C ABI。所有 ci_pip_* 函数都必须在主线程调用（Avalonia iOS 的 UI 线程即主线程）。
/// 返回码必须与 C# 侧 IosPipNativeBridge 中的常量保持一致。
private enum PipError: Int32 {
    case ok = 0
    case unsupported = -1
    case invalidArgument = -2
    case notPrepared = -3
    case notPossible = -4
    case internalFailure = -5
    case wrongThread = -6
}

private enum PipConstants {
    static let errorBufferSize = 1024
    /// miniaudio 等组件可能改写音频会话类别；定期确认仍为 .playback，避免画中画在后台被系统收起。
    static let audioSessionCheckInterval: TimeInterval = 5
    static let startTimeout: TimeInterval = 5
    /// 有限的播放时间范围；无限范围会让 AVKit 的内部计时器持续空转。
    static let playbackRangeSeconds: Int64 = 24 * 3600
}

// MARK: - 错误信息

private let pipErrorBuffer: UnsafeMutablePointer<CChar> = {
    let buffer = UnsafeMutablePointer<CChar>.allocate(capacity: PipConstants.errorBufferSize)
    buffer.initialize(repeating: 0, count: PipConstants.errorBufferSize)
    return buffer
}()

private func pipSetLastError(_ message: String) {
    memset(pipErrorBuffer, 0, PipConstants.errorBufferSize)
    message.withCString { source in
        _ = strncpy(pipErrorBuffer, source, PipConstants.errorBufferSize - 1)
    }
}

// MARK: - 控制器

private final class ClassIslandPipController: NSObject,
    AVPictureInPictureControllerDelegate,
    AVPictureInPictureSampleBufferPlaybackDelegate {
    static let shared = ClassIslandPipController()

    private let displayLayer = AVSampleBufferDisplayLayer()
    private var containerView: UIView?
    private var controller: AVPictureInPictureController?
    private var pixelBufferPool: CVPixelBufferPool?
    private var poolWidth = 0
    private var poolHeight = 0
    private var formatDescription: CMVideoFormatDescription?
    private var isAudioSessionActive = false
    private var lastAudioSessionCheck = Date.distantPast
    private var possibleObservation: NSKeyValueObservation?
    private(set) var isActive = false

    var isPossible: Bool { controller?.isPictureInPicturePossible == true }

    func prepare() -> PipError {
        if controller != nil, containerView?.window != nil {
            return .ok
        }

        guard let window = Self.findKeyWindow() else {
            pipSetLastError("找不到可用的应用窗口。")
            return .notPrepared
        }

        if let error = activateAudioSession() {
            pipSetLastError("无法配置音频会话：\(error.localizedDescription)")
            return .internalFailure
        }

        if containerView == nil {
            let view = UIView(frame: window.bounds)
            view.isUserInteractionEnabled = false
            displayLayer.videoGravity = .resizeAspect
            view.layer.addSublayer(displayLayer)
            containerView = view
        }
        if containerView?.window !== window {
            containerView?.removeFromSuperview()
            // 放在根视图下方，被应用界面完全遮住，用户看不到它。
            window.insertSubview(containerView!, at: 0)
        }
        syncContainerFrame()

        if controller == nil {
            let source = AVPictureInPictureController.ContentSource(
                sampleBufferDisplayLayer: displayLayer,
                playbackDelegate: self)
            let pip = AVPictureInPictureController(contentSource: source)
            pip.delegate = self
            pip.requiresLinearPlayback = true
            pip.canStartPictureInPictureAutomaticallyFromInline = true
            controller = pip
        }

        return .ok
    }

    func setAutoStart(_ enabled: Bool) {
        controller?.canStartPictureInPictureAutomaticallyFromInline = enabled
    }

    func start() -> PipError {
        guard let controller else {
            pipSetLastError("画中画尚未准备。")
            return .notPrepared
        }

        possibleObservation = nil
        // 系统只接受由用户操作触发的开启请求；在同一次操作的下一个主线程循环中调用，
        // 同步调用会被系统静默忽略。
        if controller.isPictureInPicturePossible {
            DispatchQueue.main.async {
                controller.startPictureInPicture()
            }
            return .ok
        }

        // 刚创建的控制器需要收到几帧内容后才会变为可开启，等它就绪后再开启。
        pipSetLastError("等待系统允许开启画中画。")
        possibleObservation = controller.observe(
            \.isPictureInPicturePossible,
            options: [.new]) { [weak self] observed, change in
            guard change.newValue == true else {
                return
            }
            DispatchQueue.main.async {
                guard let self, self.possibleObservation != nil else {
                    return
                }
                self.possibleObservation = nil
                observed.startPictureInPicture()
            }
        }
        // 超时后放弃，避免用户已经离开开关很久之后才突然弹出画中画。
        DispatchQueue.main.asyncAfter(deadline: .now() + PipConstants.startTimeout) { [weak self] in
            guard let self, self.possibleObservation != nil else {
                return
            }
            self.possibleObservation = nil
            pipSetLastError("系统在 \(Int(PipConstants.startTimeout)) 秒内未允许开启画中画。")
        }
        return .ok
    }

    func stop() {
        controller?.stopPictureInPicture()
    }

    func teardown() {
        possibleObservation = nil
        controller?.stopPictureInPicture()
        controller?.delegate = nil
        controller = nil
        displayLayer.flushAndRemoveImage()
        containerView?.removeFromSuperview()
        containerView = nil
        pixelBufferPool = nil
        formatDescription = nil
        poolWidth = 0
        poolHeight = 0
        isActive = false
        deactivateAudioSession()
    }

    func enqueue(_ pixels: UnsafeRawPointer, width: Int, height: Int, stride: Int) -> PipError {
        guard width > 0, height > 0, stride >= width * 4 else {
            pipSetLastError("帧尺寸无效：\(width)x\(height)，stride=\(stride)。")
            return .invalidArgument
        }

        reassertAudioSessionIfNeeded()
        syncContainerFrame()

        guard let pixelBuffer = makePixelBuffer(width: width, height: height) else {
            return .internalFailure
        }

        CVPixelBufferLockBaseAddress(pixelBuffer, [])
        if let destination = CVPixelBufferGetBaseAddress(pixelBuffer) {
            let destinationStride = CVPixelBufferGetBytesPerRow(pixelBuffer)
            let rowBytes = width * 4
            for row in 0..<height {
                memcpy(destination + row * destinationStride, pixels + row * stride, rowBytes)
            }
        }
        CVPixelBufferUnlockBaseAddress(pixelBuffer, [])

        if formatDescription == nil ||
            !CMVideoFormatDescriptionMatchesImageBuffer(formatDescription!, imageBuffer: pixelBuffer) {
            var description: CMVideoFormatDescription?
            let status = CMVideoFormatDescriptionCreateForImageBuffer(
                allocator: kCFAllocatorDefault,
                imageBuffer: pixelBuffer,
                formatDescriptionOut: &description)
            guard status == noErr, let description else {
                pipSetLastError("无法创建视频格式描述：\(status)。")
                return .internalFailure
            }
            formatDescription = description
        }

        var timing = CMSampleTimingInfo(
            duration: .invalid,
            presentationTimeStamp: CMClockGetTime(CMClockGetHostTimeClock()),
            decodeTimeStamp: .invalid)
        var sampleBuffer: CMSampleBuffer?
        let status = CMSampleBufferCreateReadyWithImageBuffer(
            allocator: kCFAllocatorDefault,
            imageBuffer: pixelBuffer,
            formatDescription: formatDescription!,
            sampleTiming: &timing,
            sampleBufferOut: &sampleBuffer)
        guard status == noErr, let sampleBuffer else {
            pipSetLastError("无法创建帧：\(status)。")
            return .internalFailure
        }

        // 帧没有连续时间轴，要求显示层收到后立即呈现。
        if let attachments = CMSampleBufferGetSampleAttachmentsArray(sampleBuffer, createIfNecessary: true),
           CFArrayGetCount(attachments) > 0 {
            let dictionary = unsafeBitCast(CFArrayGetValueAtIndex(attachments, 0), to: CFMutableDictionary.self)
            CFDictionarySetValue(
                dictionary,
                Unmanaged.passUnretained(kCMSampleAttachmentKey_DisplayImmediately).toOpaque(),
                Unmanaged.passUnretained(kCFBooleanTrue).toOpaque())
        }

        if displayLayer.status == .failed {
            displayLayer.flush()
        }
        displayLayer.enqueue(sampleBuffer)
        return .ok
    }

    /// 离开应用时自动进入画中画要求源视图足够大：至少与安全区同宽，且不能是一个很小的视图，
    /// 否则系统会判定为不合格而不自动开启。因此容器始终铺满窗口，并跟随旋转更新尺寸。
    private func syncContainerFrame() {
        guard let containerView, let window = containerView.window else {
            return
        }
        if containerView.frame != window.bounds {
            containerView.frame = window.bounds
        }
        if displayLayer.frame != containerView.bounds {
            CATransaction.begin()
            CATransaction.setDisableActions(true)
            displayLayer.frame = containerView.bounds
            CATransaction.commit()
        }
    }

    private func makePixelBuffer(width: Int, height: Int) -> CVPixelBuffer? {
        if pixelBufferPool == nil || poolWidth != width || poolHeight != height {
            let attributes: [CFString: Any] = [
                kCVPixelBufferPixelFormatTypeKey: kCVPixelFormatType_32BGRA,
                kCVPixelBufferWidthKey: width,
                kCVPixelBufferHeightKey: height,
                kCVPixelBufferIOSurfacePropertiesKey: [CFString: Any]()
            ]
            var pool: CVPixelBufferPool?
            let status = CVPixelBufferPoolCreate(kCFAllocatorDefault, nil, attributes as CFDictionary, &pool)
            guard status == kCVReturnSuccess, let pool else {
                pipSetLastError("无法创建像素缓冲池：\(status)。")
                return nil
            }
            pixelBufferPool = pool
            poolWidth = width
            poolHeight = height
            formatDescription = nil
        }

        var pixelBuffer: CVPixelBuffer?
        let status = CVPixelBufferPoolCreatePixelBuffer(kCFAllocatorDefault, pixelBufferPool!, &pixelBuffer)
        guard status == kCVReturnSuccess, let pixelBuffer else {
            pipSetLastError("无法分配像素缓冲：\(status)。")
            return nil
        }
        return pixelBuffer
    }

    private func activateAudioSession() -> Error? {
        do {
            // 不能使用 mixWithOthers：可混音的会话无法成为系统的“正在播放”应用，
            // 离开应用时系统不会自动进入画中画。
            try AVAudioSession.sharedInstance().setCategory(.playback, mode: .moviePlayback)
            try AVAudioSession.sharedInstance().setActive(true)
            isAudioSessionActive = true
            lastAudioSessionCheck = Date()
            return nil
        } catch {
            return error
        }
    }

    private func reassertAudioSessionIfNeeded() {
        guard isAudioSessionActive,
              Date().timeIntervalSince(lastAudioSessionCheck) >= PipConstants.audioSessionCheckInterval else {
            return
        }
        lastAudioSessionCheck = Date()
        let session = AVAudioSession.sharedInstance()
        if session.category != .playback || session.mode != .moviePlayback {
            _ = activateAudioSession()
        }
    }

    private func deactivateAudioSession() {
        guard isAudioSessionActive else {
            return
        }
        isAudioSessionActive = false
        // 其他组件可能仍在播放声音，停用失败不影响关闭画中画。
        try? AVAudioSession.sharedInstance().setActive(false, options: [.notifyOthersOnDeactivation])
    }

    private static func findKeyWindow() -> UIWindow? {
        let windows = UIApplication.shared.connectedScenes
            .compactMap { $0 as? UIWindowScene }
            .flatMap { $0.windows }
        return windows.first { $0.isKeyWindow } ?? windows.first
    }

    // MARK: AVPictureInPictureControllerDelegate

    // 生命周期事件也写入 last_error，C# 侧据此记录日志，便于在真机上判断系统走到了哪一步。
    func pictureInPictureControllerWillStartPictureInPicture(_ controller: AVPictureInPictureController) {
        pipSetLastError("系统即将开启画中画。")
    }

    func pictureInPictureControllerDidStartPictureInPicture(_ controller: AVPictureInPictureController) {
        isActive = true
        pipSetLastError("画中画已开启。")
    }

    func pictureInPictureControllerDidStopPictureInPicture(_ controller: AVPictureInPictureController) {
        isActive = false
        pipSetLastError("画中画已关闭。")
    }

    func pictureInPictureController(
        _ controller: AVPictureInPictureController,
        failedToStartPictureInPictureWithError error: Error) {
        isActive = false
        pipSetLastError("画中画启动失败：\(error.localizedDescription)")
    }

    func pictureInPictureController(
        _ controller: AVPictureInPictureController,
        restoreUserInterfaceForPictureInPictureStopWithCompletionHandler completionHandler: @escaping (Bool) -> Void) {
        completionHandler(true)
    }

    // MARK: AVPictureInPictureSampleBufferPlaybackDelegate

    func pictureInPictureController(_ controller: AVPictureInPictureController, setPlaying playing: Bool) {
    }

    func pictureInPictureControllerTimeRangeForPlayback(_ controller: AVPictureInPictureController) -> CMTimeRange {
        CMTimeRange(start: .zero, duration: CMTime(value: PipConstants.playbackRangeSeconds, timescale: 1))
    }

    func pictureInPictureControllerIsPlaybackPaused(_ controller: AVPictureInPictureController) -> Bool {
        false
    }

    func pictureInPictureController(
        _ controller: AVPictureInPictureController,
        didTransitionToRenderSize newRenderSize: CMVideoDimensions) {
    }

    func pictureInPictureController(
        _ controller: AVPictureInPictureController,
        skipByInterval skipInterval: CMTime,
        completion completionHandler: @escaping () -> Void) {
        completionHandler()
    }
}

// MARK: - C ABI

private func pipController(_ function: String) -> (ClassIslandPipController?, PipError) {
    guard Thread.isMainThread else {
        pipSetLastError("\(function) 必须在主线程调用。")
        return (nil, .wrongThread)
    }
    guard AVPictureInPictureController.isPictureInPictureSupported() else {
        pipSetLastError("此设备不支持画中画。")
        return (nil, .unsupported)
    }
    return (ClassIslandPipController.shared, .ok)
}

@_cdecl("ci_pip_is_supported")
public func ci_pip_is_supported() -> Int32 {
    AVPictureInPictureController.isPictureInPictureSupported() ? 1 : 0
}

@_cdecl("ci_pip_prepare")
public func ci_pip_prepare() -> Int32 {
    let (controller, error) = pipController("ci_pip_prepare")
    guard let controller else {
        return error.rawValue
    }
    return controller.prepare().rawValue
}

@_cdecl("ci_pip_set_auto_start")
public func ci_pip_set_auto_start(_ enabled: Int32) {
    let (controller, _) = pipController("ci_pip_set_auto_start")
    controller?.setAutoStart(enabled != 0)
}

@_cdecl("ci_pip_enqueue_bgra")
public func ci_pip_enqueue_bgra(
    _ pixels: UnsafeRawPointer?,
    _ width: Int32,
    _ height: Int32,
    _ stride: Int32
) -> Int32 {
    let (controller, error) = pipController("ci_pip_enqueue_bgra")
    guard let controller else {
        return error.rawValue
    }
    guard let pixels else {
        pipSetLastError("ci_pip_enqueue_bgra: pixels 不能为 NULL。")
        return PipError.invalidArgument.rawValue
    }
    return controller.enqueue(pixels, width: Int(width), height: Int(height), stride: Int(stride)).rawValue
}

@_cdecl("ci_pip_start")
public func ci_pip_start() -> Int32 {
    let (controller, error) = pipController("ci_pip_start")
    guard let controller else {
        return error.rawValue
    }
    return controller.start().rawValue
}

@_cdecl("ci_pip_stop")
public func ci_pip_stop() {
    let (controller, _) = pipController("ci_pip_stop")
    controller?.stop()
}

@_cdecl("ci_pip_teardown")
public func ci_pip_teardown() {
    let (controller, _) = pipController("ci_pip_teardown")
    controller?.teardown()
}

@_cdecl("ci_pip_is_active")
public func ci_pip_is_active() -> Int32 {
    let (controller, _) = pipController("ci_pip_is_active")
    return controller?.isActive == true ? 1 : 0
}

@_cdecl("ci_pip_is_possible")
public func ci_pip_is_possible() -> Int32 {
    let (controller, _) = pipController("ci_pip_is_possible")
    return controller?.isPossible == true ? 1 : 0
}

@_cdecl("ci_pip_last_error")
public func ci_pip_last_error() -> UnsafePointer<CChar>? {
    UnsafePointer(pipErrorBuffer)
}
