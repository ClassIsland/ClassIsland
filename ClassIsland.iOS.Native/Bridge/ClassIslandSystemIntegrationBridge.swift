import AppIntents
import CoreSpotlight
import Foundation
import UIKit
import UniformTypeIdentifiers
import WidgetKit

public typealias ClassIslandSystemNavigationCallback = @convention(c) (UnsafePointer<CChar>?) -> Void

private let systemIntegrationLock = NSLock()
private var systemIntegrationError: UnsafeMutablePointer<CChar>?
private var systemPendingCommand: UnsafeMutablePointer<CChar>?
private var systemNavigationCallback: ClassIslandSystemNavigationCallback?
private let systemIndexQueue = DispatchQueue(label: "cn.classisland.system-search")
private let systemIndexGate = DispatchSemaphore(value: 1)
private var systemIndexSnapshot: ClassIslandSystemSnapshot?
private var systemIndexNeedsRetry = true
private var systemShortcutClassPlans: [ClassIslandSystemClassPlan]?
private var mainMenuStatus = "not-installed"
private var mainMenuStatusPointer: UnsafeMutablePointer<CChar>?

private func setSystemIntegrationError(_ message: String?) {
    systemIntegrationLock.lock()
    defer { systemIntegrationLock.unlock() }
    free(systemIntegrationError)
    systemIntegrationError = message.flatMap { strdup($0) }
}

@_cdecl("ci_system_integration_last_error")
public func ci_system_integration_last_error() -> UnsafePointer<CChar>? {
    systemIntegrationError.map { UnsafePointer($0) }
}

/// 返回 1 表示快照已写入共享容器；索引失败仅影响搜索，不阻止小组件更新。
@_cdecl("ci_system_integration_publish")
public func ci_system_integration_publish(_ json: UnsafePointer<CChar>?) -> Int32 {
    guard let json, let data = String(validatingCString: json)?.data(using: .utf8),
          data.count <= 2_097_152 else {
        setSystemIntegrationError("System integration payload is invalid or exceeds 2 MB.")
        return 0
    }
    do {
        let snapshot = try JSONDecoder().decode(ClassIslandSystemSnapshot.self, from: data)
        try snapshot.validate()
        let previous = ClassIslandSystemSnapshotStore.load()
        let url = try ClassIslandSystemSnapshotStore.fileURL(
            named: ClassIslandSystemSnapshotStore.snapshotFileName)
        try data.write(to: url, options: [.atomic, .completeFileProtectionUntilFirstUserAuthentication])
        if previous?.lessons != snapshot.lessons || previous?.classPlans != snapshot.classPlans
            || previous?.notificationsEnabled != snapshot.notificationsEnabled
            || previous?.validUntil != snapshot.validUntil {
            WidgetCenter.shared.reloadTimelines(ofKind: "ClassIslandSchedule")
        }
        systemIntegrationLock.lock()
        let shouldIndex = systemIndexNeedsRetry || systemIndexSnapshot?.lessons != snapshot.lessons
            || systemIndexSnapshot?.classPlans != snapshot.classPlans
        if shouldIndex {
            systemIndexSnapshot = snapshot
            systemIndexNeedsRetry = false
        }
        let shouldRefreshShortcutParameters = systemShortcutClassPlans != snapshot.classPlans
        systemShortcutClassPlans = snapshot.classPlans
        systemIntegrationLock.unlock()
        if shouldIndex {
            systemIndexQueue.async { indexSystemSnapshot(snapshot) }
        }
        if #available(iOS 16.0, *), shouldRefreshShortcutParameters {
            ClassIslandActionShortcuts.updateAppShortcutParameters()
        }
        setSystemIntegrationError(nil)
        return 1
    } catch ClassIslandSystemSnapshotError.sharedContainerUnavailable {
        let groups = ClassIslandSystemSnapshotStore.candidateGroupIdentifiers.joined(separator: ", ")
        setSystemIntegrationError(
            "App Group container is unavailable. The signing entitlements must include one of: \(groups).")
        return 0
    } catch {
        setSystemIntegrationError(String(describing: error))
        return 0
    }
}

/// 小组件可能缓存了读取失败时的时间线；快照内容未变时 publish 不会刷新，应用回到前台时由宿主显式刷新。
@_cdecl("ci_system_integration_reload_widgets")
public func ci_system_integration_reload_widgets() {
    WidgetCenter.shared.reloadTimelines(ofKind: "ClassIslandSchedule")
}

/// iPadOS 26 起，使用场景生命周期的应用不再调用 App Delegate 的 buildMenu(with:)，
/// 菜单栏中的“课表”菜单必须通过 UIMainMenuSystem 注册。命令的选择器由宿主 AppDelegate 实现。
/// 返回 1 表示已注册；旧系统或旧 SDK 返回 0，由宿主继续使用 buildMenu(with:)。
@_cdecl("ci_system_integration_install_main_menu")
public func ci_system_integration_install_main_menu() -> Int32 {
#if compiler(>=6.2)
    guard #available(iOS 26.0, *) else {
        mainMenuStatus = "skipped: system older than iOS 26"
        return 0
    }
    guard Thread.isMainThread else {
        mainMenuStatus = "skipped: not on main thread"
        return 0
    }
    MainActor.assumeIsolated {
        UIMainMenuSystem.shared.setBuildConfiguration(UIMainMenuSystem.Configuration()) { builder in
            builder.insertSibling(classIslandScheduleMenu(), afterMenu: .view)
            builder.insertChild(classIslandViewMenu(), atStartOfMenu: .view)
            let hasMenu = builder.menu(for: UIMenu.Identifier("cn.classisland.schedule")) != nil
            let target = UIApplication.shared.target(forAction: Selector(("ciToday:")), withSender: nil)
            mainMenuStatus = "built (schedule menu present: \(hasMenu), command target: "
                + "\(target.map { String(describing: type(of: $0)) } ?? "none"))"
        }
        // 菜单可能已按默认配置构建过一次，显式请求重建以应用自定义配置。
        UIMainMenuSystem.shared.setNeedsRebuild()
    }
    mainMenuStatus = "installed, waiting for build"
    return 1
#else
    mainMenuStatus = "skipped: built with Swift older than 6.2"
    return 0
#endif
}

/// 返回菜单栏注册与构建状态，仅用于启动诊断日志。指针在下次调用前有效。
@_cdecl("ci_system_integration_main_menu_status")
public func ci_system_integration_main_menu_status() -> UnsafePointer<CChar>? {
    free(mainMenuStatusPointer)
    mainMenuStatusPointer = strdup(mainMenuStatus)
    return mainMenuStatusPointer.map { UnsafePointer($0) }
}

private func menuCommand(
    _ title: String, _ input: String, _ modifiers: UIKeyModifierFlags, _ selector: String) -> UIKeyCommand {
    UIKeyCommand(title: title, action: Selector(selector), input: input, modifierFlags: modifiers)
}

// 以下标题、快捷键和选择器需与 AppDelegate.BuildMenu 保持一致。
private func classIslandViewMenu() -> UIMenu {
    UIMenu(title: "", identifier: UIMenu.Identifier("cn.classisland.view"), options: .displayInline, children: [
        menuCommand("今天", "1", .command, "ciShowTodayPage:"),
        menuCommand("本周", "2", .command, "ciShowWeekPage:"),
        menuCommand("更多", "3", .command, "ciShowMorePage:"),
        menuCommand("显示或隐藏侧栏", "s", [.command, .control], "ciToggleSidebar:")
    ])
}

private func classIslandScheduleMenu() -> UIMenu {
    let command = menuCommand
    return UIMenu(title: "课表", identifier: UIMenu.Identifier("cn.classisland.schedule"), children: [
        command("今日课表", "t", .command, "ciToday:"),
        command("前一天", "[", .command, "ciPreviousDay:"),
        command("后一天", "]", .command, "ciNextDay:"),
        command("换课…", "k", [.command, .shift], "ciClassSwap:"),
        command("加载临时课表…", "t", [.command, .shift], "ciTemporaryClassPlan:"),
        command("编辑档案…", "e", [.command, .shift], "ciProfile:"),
        command("设置…", ",", .command, "ciSettings:")
    ])
}

@_cdecl("ci_system_integration_register_navigation")
public func ci_system_integration_register_navigation(_ callback: ClassIslandSystemNavigationCallback?) {
    systemIntegrationLock.lock()
    systemNavigationCallback = callback
    systemIntegrationLock.unlock()
}

/// 指针由原生层保管，调用方读取 UTF-8 字符串后不应释放。
@_cdecl("ci_system_integration_consume_pending")
public func ci_system_integration_consume_pending() -> UnsafePointer<CChar>? {
    systemIntegrationLock.lock()
    defer { systemIntegrationLock.unlock() }
    free(systemPendingCommand)
    systemPendingCommand = nil
    guard let url = try? ClassIslandSystemSnapshotStore.fileURL(
        named: ClassIslandSystemSnapshotStore.pendingCommandFileName),
          let command = try? String(contentsOf: url, encoding: .utf8),
          let attributes = try? FileManager.default.attributesOfItem(atPath: url.path),
          let modifiedAt = attributes[.modificationDate] as? Date else { return nil }
    try? FileManager.default.removeItem(at: url)
    guard Date.now.timeIntervalSince(modifiedAt) < 120 else { return nil }
    systemPendingCommand = strdup(command)
    return systemPendingCommand.map { UnsafePointer($0) }
}

enum ClassIslandSystemCommandHandoff {
    @MainActor
    static func request(_ command: String) throws {
        let url = try ClassIslandSystemSnapshotStore.fileURL(
            named: ClassIslandSystemSnapshotStore.pendingCommandFileName)
        try Data(command.utf8).write(
            to: url, options: [.atomic, .completeFileProtectionUntilFirstUserAuthentication])
        systemIntegrationLock.lock()
        let callback = systemNavigationCallback
        systemIntegrationLock.unlock()
        if let callback {
            command.withCString { callback($0) }
            try? FileManager.default.removeItem(at: url)
        }
    }
}

private func indexSystemSnapshot(_ snapshot: ClassIslandSystemSnapshot) {
    systemIndexGate.wait()
    guard CSSearchableIndex.isIndexingAvailable() else {
        finishSystemIndexUpdate(succeeded: false)
        return
    }
    let domain = "cn.classisland.schedule"
    let lessonItems = snapshot.lessons.filter { $0.endDate > .now }.map { lesson in
        let attributes = CSSearchableItemAttributeSet(contentType: .text)
        attributes.title = lesson.subject
        attributes.contentDescription = "\(lesson.startDate.formatted(date: .abbreviated, time: .shortened)) – \(lesson.endDate.formatted(date: .omitted, time: .shortened))\(lesson.location.isEmpty ? "" : " · \(lesson.location)")"
        attributes.keywords = ["ClassIsland", "课程", "课表", lesson.subject, lesson.location]
        attributes.contentURL = lesson.deepLink
        let item = CSSearchableItem(uniqueIdentifier: lesson.url, domainIdentifier: domain, attributeSet: attributes)
        item.expirationDate = lesson.endDate
        return item
    }
    let planItems = snapshot.classPlans.map { plan in
        let attributes = CSSearchableItemAttributeSet(contentType: .text)
        attributes.title = plan.name
        attributes.contentDescription = "在 ClassIsland 中查看课表"
        attributes.keywords = ["ClassIsland", "课表", plan.name]
        attributes.contentURL = URL(string: plan.url)
        let item = CSSearchableItem(uniqueIdentifier: plan.url, domainIdentifier: domain, attributeSet: attributes)
        item.expirationDate = Date(timeIntervalSince1970: snapshot.validUntil)
        return item
    }
    let index = CSSearchableIndex.default()
    index.deleteSearchableItems(withDomainIdentifiers: [domain]) { error in
        guard error == nil else {
            NSLog("ClassIsland: Failed to replace schedule search index: %@", String(describing: error))
            finishSystemIndexUpdate(succeeded: false)
            return
        }
        index.indexSearchableItems(lessonItems + planItems) { error in
            if let error {
                NSLog("ClassIsland: Failed to index schedule: %@", String(describing: error))
            }
            finishSystemIndexUpdate(succeeded: error == nil)
        }
    }
}

private func finishSystemIndexUpdate(succeeded: Bool) {
    systemIntegrationLock.lock()
    systemIndexNeedsRetry = !succeeded
    systemIntegrationLock.unlock()
    systemIndexGate.signal()
}
