import AppIntents
import Foundation

struct ClassIslandNextLessonIntent: AppIntent {
    static let title: LocalizedStringResource = "查询下一节课"
    static let description = IntentDescription("无需打开应用，查询下一节课的科目、时间和教室。")

    func perform() async throws -> some IntentResult & ReturnsValue<String> & ProvidesDialog {
        let now = Date.now
        let snapshot = try ClassIslandSystemSnapshotStore.loadCurrent(at: now)
        let lesson = snapshot.lessons.first { $0.startDate > now }
        let answer: String
        if let lesson {
            answer = "下一节是\(lesson.subject)，\(lesson.startDate.formatted(date: .abbreviated, time: .shortened))开始\(lesson.location.isEmpty ? "。" : "，地点：\(lesson.location)。")"
        } else {
            answer = "已同步的课表中没有接下来的课程。"
        }
        return .result(value: answer, dialog: "\(answer)")
    }
}

struct ClassIslandTodayLessonsIntent: AppIntent {
    static let title: LocalizedStringResource = "查询今日课表"
    static let description = IntentDescription("无需打开应用，查询今天的课程和上课时间。")

    func perform() async throws -> some IntentResult & ReturnsValue<String> & ProvidesDialog {
        let now = Date.now
        let snapshot = try ClassIslandSystemSnapshotStore.loadCurrent(at: now)
        let lessons = snapshot.lessons(on: now)
        let answer = lessons.isEmpty ? "今天没有课程。" : lessons.map { lesson in
            "\(lesson.startDate.formatted(date: .omitted, time: .shortened)) \(lesson.subject)\(lesson.location.isEmpty ? "" : "（\(lesson.location)）")"
        }.joined(separator: "\n")
        return .result(value: answer, dialog: "\(answer)")
    }
}

struct ClassIslandQueryShortcuts: AppShortcutsProvider {
    static var appShortcuts: [AppShortcut] {
        AppShortcut(
            intent: ClassIslandNextLessonIntent(),
            phrases: ["查询\(.applicationName)下一节课", "问\(.applicationName)下一节什么课"],
            shortTitle: "查询下一节课", systemImageName: "clock")
        AppShortcut(
            intent: ClassIslandTodayLessonsIntent(),
            phrases: ["查询\(.applicationName)今日课表"],
            shortTitle: "查询今日课表", systemImageName: "list.bullet")
    }
}
