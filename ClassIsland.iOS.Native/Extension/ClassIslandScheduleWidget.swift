import SwiftUI
import WidgetKit

struct ClassIslandScheduleEntry: TimelineEntry {
    let date: Date
    let snapshot: ClassIslandSystemSnapshot?

    var lesson: ClassIslandSystemLesson? { snapshot?.currentOrNextLesson(at: date) }
    var isStale: Bool { snapshot.map { !$0.isCurrent(at: date) } ?? false }
}

struct ClassIslandScheduleProvider: TimelineProvider {
    func placeholder(in context: Context) -> ClassIslandScheduleEntry {
        previewEntry
    }

    func getSnapshot(in context: Context, completion: @escaping (ClassIslandScheduleEntry) -> Void) {
        completion(context.isPreview ? previewEntry : ClassIslandScheduleEntry(
            date: .now, snapshot: ClassIslandSystemSnapshotStore.load()))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<ClassIslandScheduleEntry>) -> Void) {
        let now = Date.now
        let snapshot = ClassIslandSystemSnapshotStore.load()
        let dates = snapshot?.timelineDates(after: now) ?? [now]
        let entries = dates.map { ClassIslandScheduleEntry(date: $0, snapshot: snapshot) }
        completion(Timeline(entries: entries, policy: .after(now.addingTimeInterval(21_600))))
    }

    private var previewEntry: ClassIslandScheduleEntry {
        let now = Date.now
        return ClassIslandScheduleEntry(date: now, snapshot: ClassIslandSystemSnapshot(
            generatedAt: now.timeIntervalSince1970, validUntil: now.addingTimeInterval(86_400).timeIntervalSince1970,
            notificationsEnabled: true,
            lessons: [ClassIslandSystemLesson(
                id: "preview", subject: "数学", location: "教学楼 302",
                startAt: now.addingTimeInterval(600).timeIntervalSince1970,
                endAt: now.addingTimeInterval(3_000).timeIntervalSince1970,
                url: "classisland://app/system/schedule")], classPlans: []))
    }
}

struct ClassIslandScheduleWidget: Widget {
    var body: some WidgetConfiguration {
        StaticConfiguration(kind: "ClassIslandSchedule", provider: ClassIslandScheduleProvider()) { entry in
            ClassIslandScheduleWidgetView(entry: entry)
                .widgetURL(entry.lesson?.deepLink ?? ClassIslandSystemSnapshotStore.scheduleURL)
        }
        .configurationDisplayName("课程与课表")
        .description("查看当前课程、下一节课和今天的课表。")
        .supportedFamilies([.systemSmall, .systemMedium, .systemLarge, .accessoryRectangular, .accessoryInline])
    }
}

private struct ClassIslandScheduleWidgetView: View {
    @Environment(\.widgetFamily) private var family
    let entry: ClassIslandScheduleEntry

    var body: some View {
        content
            .classIslandWidgetBackground()
    }

    @ViewBuilder
    private var content: some View {
        if family == .accessoryInline {
            if let lesson = entry.lesson {
                Label(lesson.subject, systemImage: "calendar")
            } else {
                Text(statusTitle)
            }
        } else if family == .accessoryRectangular {
            VStack(alignment: .leading, spacing: 2) {
                Text(entry.lesson.map { $0.startDate <= entry.date ? "正在上课" : "下一节课" } ?? "ClassIsland")
                    .font(.caption)
                if let lesson = entry.lesson {
                    Text(lesson.subject).font(.headline).lineLimit(1)
                    Text(lesson.startDate, style: .time).font(.caption).monospacedDigit()
                } else {
                    Text(statusTitle).font(.headline)
                    Text(statusDetail).font(.caption).lineLimit(1)
                }
            }
        } else {
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    Label("ClassIsland", systemImage: "calendar")
                        .font(.caption.weight(.semibold)).foregroundStyle(.secondary)
                    Spacer(minLength: 0)
                    if family != .systemSmall {
                        Text(entry.date, style: .date).font(.caption).foregroundStyle(.secondary)
                    }
                }
                if let lesson = entry.lesson {
                    if family == .systemMedium {
                        HStack(alignment: .top, spacing: 14) {
                            lessonSummary(lesson)
                                .frame(maxWidth: .infinity, alignment: .leading)
                            VStack(alignment: .leading, spacing: 8) {
                                Text("今日课表").font(.caption).foregroundStyle(.secondary)
                                todayLessons
                            }
                            .frame(maxWidth: .infinity, alignment: .leading)
                        }
                    } else {
                        lessonSummary(lesson)
                    }
                    if family == .systemLarge {
                        Divider()
                        todayLessons
                    }
                } else {
                    Spacer(minLength: 0)
                    Text(statusTitle).font(.headline)
                    Text(statusDetail).font(.caption).foregroundStyle(.secondary)
                }
                Spacer(minLength: 0)
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
        }
    }

    private func lessonSummary(_ lesson: ClassIslandSystemLesson) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(lesson.startDate <= entry.date ? "正在上课" : "下一节课")
                .font(.caption).foregroundStyle(.secondary)
            Text(lesson.subject).font(.title2.weight(.bold)).lineLimit(1).minimumScaleFactor(0.8)
            HStack(spacing: 3) {
                Text(lesson.startDate, style: .time)
                Text("–")
                Text(lesson.endDate, style: .time)
            }
            .font(.caption).monospacedDigit().foregroundStyle(.secondary)
            if !lesson.location.isEmpty {
                Label(lesson.location, systemImage: "mappin.and.ellipse")
                    .font(.caption).lineLimit(1)
            }
            if lesson.startDate <= entry.date {
                Text(timerInterval: entry.date...lesson.endDate, countsDown: true)
                    .font(.caption.monospacedDigit()).foregroundStyle(.tint)
                    .frame(maxWidth: 90, alignment: .leading)
                    .accessibilityLabel("距离下课")
            }
        }
    }

    @ViewBuilder
    private var todayLessons: some View {
        let lessons = entry.snapshot?.lessons(on: entry.date) ?? []
        let remaining = lessons.filter { $0.endDate > entry.date }
        let visible = Array(remaining.prefix(family == .systemLarge ? 7 : 3))
        if visible.isEmpty {
            Text("今天的课程已结束").font(.caption).foregroundStyle(.secondary)
        } else {
            ForEach(visible) { lesson in
                Link(destination: lesson.deepLink ?? ClassIslandSystemSnapshotStore.scheduleURL) {
                    HStack(spacing: 8) {
                        Text(lesson.startDate, style: .time).monospacedDigit().foregroundStyle(.secondary)
                        Text(lesson.subject).lineLimit(1)
                        Spacer(minLength: 0)
                        if family == .systemLarge && !lesson.location.isEmpty {
                            Text(lesson.location).foregroundStyle(.secondary).lineLimit(1)
                        }
                    }
                    .font(.caption)
                }
                .buttonStyle(.plain)
            }
        }
    }

    private var statusTitle: String {
        if entry.snapshot == nil { return "打开 App 同步课表" }
        if entry.isStale { return "课表需要更新" }
        return "暂无课程"
    }

    private var statusDetail: String {
        if entry.snapshot == nil || entry.isStale { return "轻点打开 ClassIsland 更新" }
        return "已同步的课表中没有接下来的课程"
    }
}

private extension View {
    @ViewBuilder
    func classIslandWidgetBackground() -> some View {
        if #available(iOSApplicationExtension 17.0, *) {
            containerBackground(.background, for: .widget)
        } else {
            padding().background(Color(uiColor: .systemBackground))
        }
    }
}

struct ClassIslandScheduleWidgetPreviews: PreviewProvider {
    static var previews: some View {
        Group {
            ClassIslandScheduleWidgetView(entry: sampleEntry)
                .previewContext(WidgetPreviewContext(family: .systemSmall))
            ClassIslandScheduleWidgetView(entry: sampleEntry)
                .previewContext(WidgetPreviewContext(family: .systemMedium))
            ClassIslandScheduleWidgetView(entry: ClassIslandScheduleEntry(date: .now, snapshot: nil))
                .previewContext(WidgetPreviewContext(family: .accessoryRectangular))
        }
    }

    private static var sampleEntry: ClassIslandScheduleEntry {
        let now = Date.now
        let lessons = (0..<4).map { index in
            ClassIslandSystemLesson(
                id: "preview-\(index)", subject: ["数学", "语文", "英语", "物理"][index], location: "教学楼 302",
                startAt: now.addingTimeInterval(Double(index * 3_600 - 300)).timeIntervalSince1970,
                endAt: now.addingTimeInterval(Double(index * 3_600 + 2_100)).timeIntervalSince1970,
                url: "classisland://app/system/schedule")
        }
        return ClassIslandScheduleEntry(date: now, snapshot: ClassIslandSystemSnapshot(
            generatedAt: now.timeIntervalSince1970, validUntil: now.addingTimeInterval(86_400).timeIntervalSince1970,
            notificationsEnabled: true, lessons: lessons, classPlans: []))
    }
}
