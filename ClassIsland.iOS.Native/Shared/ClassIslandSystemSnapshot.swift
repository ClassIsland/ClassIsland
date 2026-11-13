import Foundation

struct ClassIslandSystemLesson: Codable, Hashable, Identifiable, Sendable {
    let id: String
    let subject: String
    let location: String
    let startAt: Double
    let endAt: Double
    let url: String

    var startDate: Date { Date(timeIntervalSince1970: startAt) }
    var endDate: Date { Date(timeIntervalSince1970: endAt) }
    var deepLink: URL? { URL(string: url) }
}

struct ClassIslandSystemClassPlan: Codable, Hashable, Identifiable, Sendable {
    let id: String
    let name: String
    let url: String
}

/// 主应用负责排课，系统扩展只读取已经展开的课程，避免产生两套排课规则。
struct ClassIslandSystemSnapshot: Codable, Sendable {
    let generatedAt: Double
    let validUntil: Double
    let notificationsEnabled: Bool
    let lessons: [ClassIslandSystemLesson]
    let classPlans: [ClassIslandSystemClassPlan]

    func isCurrent(at date: Date) -> Bool {
        date.timeIntervalSince1970 >= generatedAt - 300 && date.timeIntervalSince1970 < validUntil
    }

    func currentOrNextLesson(at date: Date) -> ClassIslandSystemLesson? {
        guard isCurrent(at: date) else { return nil }
        return lessons.first { $0.endAt > date.timeIntervalSince1970 }
    }

    func lessons(on date: Date, calendar: Calendar = .current) -> [ClassIslandSystemLesson] {
        guard isCurrent(at: date) else { return [] }
        return lessons.filter { calendar.isDate($0.startDate, inSameDayAs: date) }
    }

    func timelineDates(after date: Date, calendar: Calendar = .current) -> [Date] {
        guard isCurrent(at: date) else { return [date] }
        let expiry = Date(timeIntervalSince1970: validUntil)
        let horizon = min(expiry, date.addingTimeInterval(86_400))
        var dates: Set<Date> = [date, expiry]
        for lesson in lessons {
            if lesson.startDate > date && lesson.startDate <= horizon { dates.insert(lesson.startDate) }
            if lesson.endDate > date && lesson.endDate <= horizon { dates.insert(lesson.endDate) }
        }
        if let midnight = calendar.nextDate(
            after: date, matching: DateComponents(hour: 0, minute: 0), matchingPolicy: .nextTime),
           midnight <= horizon {
            dates.insert(midnight)
        }
        return dates.sorted()
    }

    func validate() throws {
        guard generatedAt.isFinite, validUntil.isFinite, generatedAt >= 0,
              validUntil <= 253_402_300_799, validUntil > generatedAt,
              validUntil - generatedAt <= 15 * 86_400,
              lessons.count <= 2_048, classPlans.count <= 512,
              Set(lessons.map(\.id)).count == lessons.count,
              Set(classPlans.map(\.id)).count == classPlans.count else {
            throw ClassIslandSystemSnapshotError.invalidSnapshot
        }
        for lesson in lessons {
            guard !lesson.id.isEmpty, !lesson.subject.isEmpty,
                  lesson.startAt.isFinite, lesson.endAt.isFinite,
                  lesson.startAt >= 0, lesson.endAt <= 253_402_300_799,
                  lesson.endAt > lesson.startAt,
                  Self.isAppURL(lesson.url) else {
                throw ClassIslandSystemSnapshotError.invalidSnapshot
            }
        }
        for plan in classPlans {
            guard UUID(uuidString: plan.id) != nil, !plan.name.isEmpty,
                  Self.isAppURL(plan.url) else {
                throw ClassIslandSystemSnapshotError.invalidSnapshot
            }
        }
        guard lessons == lessons.sorted(by: { $0.startAt < $1.startAt }) else {
            throw ClassIslandSystemSnapshotError.invalidSnapshot
        }
    }

    private static func isAppURL(_ value: String) -> Bool {
        guard let url = URL(string: value) else { return false }
        return url.scheme == "classisland" && url.host == "app"
            && url.user == nil && url.password == nil && url.port == nil
            && url.path.hasPrefix("/system/")
    }
}

enum ClassIslandSystemSnapshotError: LocalizedError {
    case sharedContainerUnavailable
    case invalidSnapshot
    case refreshRequired

    var errorDescription: String? {
        switch self {
        case .sharedContainerUnavailable:
            return "无法读取课表：当前签名未启用共享数据（App Group），请检查侧载签名设置。"
        case .invalidSnapshot, .refreshRequired:
            return "课表需要更新，请打开 ClassIsland 后重试。"
        }
    }
}

enum ClassIslandSystemSnapshotStore {
    static let snapshotFileName = "system-schedule.json"
    static let pendingCommandFileName = "system-pending-command.txt"
    static let scheduleURL = URL(string: "classisland://app/system/schedule")!

    static var groupIdentifier: String {
        let bundleIdentifier = Bundle.main.bundleIdentifier ?? "cn.classisland.ios"
        let suffix = ".LiveActivityExtension"
        let hostIdentifier = bundleIdentifier.hasSuffix(suffix)
            ? String(bundleIdentifier.dropLast(suffix.count)) : bundleIdentifier
        return "group.\(hostIdentifier).shared"
    }

    /// 依次尝试的 App Group：先用按 Bundle ID 推导的组，再用描述文件实际授予的组。
    /// 侧载签名工具可能改写 Bundle ID 或 App Group，宿主与扩展都优先读取宿主描述文件，保证选中同一个组。
    static var candidateGroupIdentifiers: [String] {
        var identifiers = [groupIdentifier]
        let bundles = [hostBundleURL, Bundle.main.bundleURL]
        for bundle in bundles {
            for identifier in provisionedAppGroups(in: bundle) where !identifiers.contains(identifier) {
                identifiers.append(identifier)
            }
        }
        return identifiers
    }

    static var containerURL: URL? { resolvedContainerURL }

    private static let resolvedContainerURL: URL? = candidateGroupIdentifiers.lazy.compactMap {
        FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: $0)
    }.first

    private static var hostBundleURL: URL {
        let bundle = Bundle.main.bundleURL
        guard bundle.pathExtension == "appex" else { return bundle }
        return bundle.deletingLastPathComponent().deletingLastPathComponent()
    }

    static func provisionedAppGroups(in bundle: URL) -> [String] {
        guard let data = try? Data(contentsOf: bundle.appendingPathComponent("embedded.mobileprovision")) else {
            return []
        }
        return provisionedAppGroups(fromProfile: data)
    }

    /// 描述文件是 CMS 签名包裹的 XML plist，只需取出其中的 plist 读取 Entitlements。
    static func provisionedAppGroups(fromProfile data: Data) -> [String] {
        guard let start = data.range(of: Data("<?xml".utf8)),
              let end = data.range(of: Data("</plist>".utf8), in: start.lowerBound..<data.endIndex),
              let plist = try? PropertyListSerialization.propertyList(
                  from: data[start.lowerBound..<end.upperBound], format: nil) as? [String: Any],
              let entitlements = plist["Entitlements"] as? [String: Any],
              let groups = entitlements["com.apple.security.application-groups"] as? [String] else {
            return []
        }
        return groups.sorted()
    }

    static func fileURL(named name: String) throws -> URL {
        guard let container = containerURL else {
            throw ClassIslandSystemSnapshotError.sharedContainerUnavailable
        }
        return container.appendingPathComponent(name, isDirectory: false)
    }

    static func load() -> ClassIslandSystemSnapshot? {
        guard let url = try? fileURL(named: snapshotFileName),
              let data = try? Data(contentsOf: url), data.count <= 2_097_152,
              let snapshot = try? JSONDecoder().decode(ClassIslandSystemSnapshot.self, from: data),
              (try? snapshot.validate()) != nil else {
            return nil
        }
        return snapshot
    }

    static func loadCurrent(at date: Date = .now) throws -> ClassIslandSystemSnapshot {
        guard let snapshot = load(), snapshot.isCurrent(at: date) else {
            throw ClassIslandSystemSnapshotError.refreshRequired
        }
        return snapshot
    }
}
