import AppIntents
import Foundation

@available(iOS 16.0, *)
struct ClassIslandClassPlanEntity: AppEntity {
    static let typeDisplayRepresentation: TypeDisplayRepresentation = "课表"
    static let defaultQuery = ClassIslandClassPlanQuery()

    let id: String
    let name: String

    var displayRepresentation: DisplayRepresentation {
        DisplayRepresentation(title: "\(name)")
    }
}

@available(iOS 16.0, *)
struct ClassIslandClassPlanQuery: EntityStringQuery {
    func entities(for identifiers: [String]) async throws -> [ClassIslandClassPlanEntity] {
        try entities().filter { identifiers.contains($0.id) }
    }

    func entities(matching string: String) async throws -> [ClassIslandClassPlanEntity] {
        try entities().filter { $0.name.localizedCaseInsensitiveContains(string) }
    }

    func suggestedEntities() async throws -> [ClassIslandClassPlanEntity] {
        try entities()
    }

    private func entities() throws -> [ClassIslandClassPlanEntity] {
        try ClassIslandSystemSnapshotStore.loadCurrent().classPlans.map {
            ClassIslandClassPlanEntity(id: $0.id, name: $0.name)
        }
    }
}

@available(iOS 16.0, *)
struct ClassIslandOpenScheduleIntent: AppIntent {
    static let title: LocalizedStringResource = "打开今日课表"
    static let description = IntentDescription("打开 ClassIsland 查看今天的课表。")
    static let openAppWhenRun = true

    @MainActor
    func perform() async throws -> some IntentResult {
        try ClassIslandSystemCommandHandoff.request("classisland://app/system/today")
        return .result()
    }
}

@available(iOS 16.0, *)
struct ClassIslandSetNotificationsIntent: AppIntent {
    static let title: LocalizedStringResource = "设置课程提醒"
    static let description = IntentDescription("打开 ClassIsland 设置课程提醒；需要验证时会在应用中继续。")
    static let openAppWhenRun = true

    @Parameter(title: "启用提醒", default: true)
    var enabled: Bool

    static var parameterSummary: some ParameterSummary {
        Summary("将课程提醒设为\(\.$enabled)")
    }

    @MainActor
    func perform() async throws -> some IntentResult {
        try ClassIslandSystemCommandHandoff.request(
            "classisland://app/system/notifications?enabled=\(enabled ? "true" : "false")")
        return .result()
    }
}

@available(iOS 16.0, *)
struct ClassIslandSelectClassPlanIntent: AppIntent {
    static let title: LocalizedStringResource = "选择临时课表"
    static let description = IntentDescription("打开 ClassIsland 选择临时课表；现有临时课表和管理验证由应用处理。")
    static let openAppWhenRun = true

    @Parameter(title: "课表")
    var classPlan: ClassIslandClassPlanEntity

    static var parameterSummary: some ParameterSummary {
        Summary("选择\(\.$classPlan)作为临时课表")
    }

    @MainActor
    func perform() async throws -> some IntentResult {
        guard UUID(uuidString: classPlan.id) != nil else {
            throw ClassIslandSystemSnapshotError.refreshRequired
        }
        try ClassIslandSystemCommandHandoff.request(
            "classisland://app/system/class-plan?id=\(classPlan.id)")
        return .result()
    }
}

@available(iOS 16.0, *)
struct ClassIslandActionShortcuts: AppShortcutsProvider {
    static var appShortcuts: [AppShortcut] {
        AppShortcut(
            intent: ClassIslandOpenScheduleIntent(),
            phrases: ["打开\(.applicationName)今日课表", "在\(.applicationName)查看课表"],
            shortTitle: "打开今日课表", systemImageName: "calendar")
        AppShortcut(
            intent: ClassIslandSetNotificationsIntent(),
            phrases: ["设置\(.applicationName)课程提醒"],
            shortTitle: "设置课程提醒", systemImageName: "bell")
        AppShortcut(
            intent: ClassIslandSelectClassPlanIntent(),
            phrases: ["选择\(.applicationName)临时课表"],
            shortTitle: "选择临时课表", systemImageName: "calendar.badge.clock")
    }
}
