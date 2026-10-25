import ActivityKit
import Foundation
import SwiftUI
import UIKit
import WidgetKit

private let activityAccent = Color(red: 0.54, green: 0.83, blue: 1)
private let activitySecondary = Color(red: 0.64, green: 0.69, blue: 0.74)

private enum ActivityLayout {
    static let contentHorizontalPadding: CGFloat = 14
    static let contentVerticalPadding: CGFloat = 10
    static let sectionSpacing: CGFloat = 10
    static let rowSpacing: CGFloat = 8
    static let timerCardHorizontalPadding: CGFloat = 12
    static let timerCardVerticalPadding: CGFloat = 8
    static let timerCardCornerRadius: CGFloat = 16
    static let islandHorizontalPadding: CGFloat = 8
    static let islandVerticalPadding: CGFloat = 8
}

struct ClassIslandLiveActivityWidget: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: ClassIslandActivityAttributes.self) { context in
            ClassIslandLockScreenView(state: context.state, isStale: context.classIslandIsStale)
                .widgetURL(context.state.deepLinkURL)
                .activityBackgroundTint(Color(red: 0.035, green: 0.055, blue: 0.07).opacity(0.94))
                .activitySystemActionForegroundColor(.white)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    ClassIslandBrandIcon(size: 26)
                        .padding(.leading, ActivityLayout.islandHorizontalPadding)
                        .padding(.top, ActivityLayout.islandVerticalPadding)
                }
                DynamicIslandExpandedRegion(.trailing) {
                    ClassIslandIslandTimer(state: context.state, isStale: context.classIslandIsStale)
                        .font(.system(size: 18, weight: .semibold, design: .rounded))
                        .foregroundStyle(activityAccent)
                        .frame(width: 72, alignment: .trailing)
                        .padding(.trailing, ActivityLayout.islandHorizontalPadding)
                        .padding(.top, ActivityLayout.islandVerticalPadding)
                }
                DynamicIslandExpandedRegion(.center) {
                    HStack(spacing: 7) {
                        ClassIslandLessonIcon(encodedImage: context.state.details?.iconPngBase64, size: 22)
                        VStack(alignment: .leading, spacing: 2) {
                            Text(context.state.phase == .breaking ? context.state.phase.displayName : context.state.lessonName)
                                .font(.headline).lineLimit(1).minimumScaleFactor(0.75)
                            Text(context.state.phase == .breaking ? "下节课：\(context.state.lessonName)" : context.state.phase.displayName)
                                .font(.caption).foregroundStyle(activityAccent).lineLimit(1).minimumScaleFactor(0.75)
                        }
                    }
                    .foregroundStyle(.white)
                    .frame(maxWidth: .infinity, alignment: .center)
                }
                DynamicIslandExpandedRegion(.bottom) {
                    if let components = context.state.components {
                        if !components.isEmpty {
                            ClassIslandExpandedComponents(
                                components: components,
                                state: context.state,
                                isStale: context.classIslandIsStale)
                                .padding(.horizontal, ActivityLayout.islandHorizontalPadding)
                                .padding(.vertical, ActivityLayout.islandVerticalPadding)
                        }
                    } else {
                        VStack(alignment: .leading, spacing: ActivityLayout.rowSpacing) {
                            HStack(alignment: .firstTextBaseline) {
                                if let location = context.state.details?.location, !location.isEmpty {
                                    Label(location, systemImage: "mappin.and.ellipse")
                                }
                                Spacer(minLength: 8)
                                Text(context.state.details?.timeText ?? context.state.detail).monospacedDigit()
                            }
                            .font(.caption).foregroundStyle(activitySecondary).lineLimit(1)
                            if context.classIslandIsStale {
                                ClassIslandStaleNotice()
                            } else {
                                ClassIslandProgressView(state: context.state)
                                ClassIslandRemainingTimeView(state: context.state)
                            }
                        }
                        .foregroundStyle(.white)
                        .padding(.horizontal, ActivityLayout.islandHorizontalPadding)
                        .padding(.vertical, ActivityLayout.islandVerticalPadding)
                    }
                }
            } compactLeading: {
                HStack(spacing: 4) {
                    ClassIslandLessonIcon(encodedImage: context.state.details?.iconPngBase64, size: 17)
                    Text(context.state.compactText).font(.caption2).lineLimit(1).foregroundStyle(.white)
                        .frame(maxWidth: 36, alignment: .leading)
                }
            } compactTrailing: {
                ClassIslandIslandTimer(state: context.state, isStale: context.classIslandIsStale)
                    .font(.caption.monospacedDigit()).foregroundStyle(activityAccent)
                    // 系统倒计时会占满提议宽度，需要在灵动岛区域内限制宽度。
                    .frame(width: 52, alignment: .trailing)
            } minimal: {
                if context.classIslandIsStale {
                    Image(systemName: "exclamationmark.triangle.fill").foregroundStyle(.orange)
                } else {
                    ClassIslandLessonIcon(encodedImage: context.state.details?.iconPngBase64, size: 20)
                }
            }
            .widgetURL(context.state.deepLinkURL)
            .keylineTint(activityAccent)
        }
    }
}

private struct ClassIslandExpandedComponents: View {
    let components: [ClassIslandActivityComponent]
    let state: ClassIslandActivityAttributes.ContentState
    let isStale: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            ForEach(Array(visibleRows.enumerated()), id: \.offset) { _, row in
                HStack(alignment: .center, spacing: 12) {
                    ForEach(Array(row.enumerated()), id: \.offset) { _, component in
                        componentView(component)
                            .frame(maxWidth: .infinity, alignment: .leading)
                    }
                }
            }
            if isStale && !components.isEmpty {
                Label("课程状态可能已变化，请打开 App 更新", systemImage: "exclamationmark.triangle.fill")
                    .font(.system(size: 10)).foregroundStyle(.orange).lineLimit(1)
            }
        }
        .foregroundStyle(.white)
    }

    private var visibleRows: [[ClassIslandActivityComponent]] {
        var rows: [[ClassIslandActivityComponent]] = []
        let supportedKinds = ["schedule", "date", "clock", "weather", "countdown", "text", "separator"]
        for component in components.prefix(6) where supportedKinds.contains(component.kind) {
            if !isFullWidth(component), let last = rows.last, last.count == 1,
               let previous = last.first, !isFullWidth(previous) {
                rows[rows.count - 1].append(component)
            } else {
                rows.append([component])
            }
        }

        // 展开态高度由系统限制；保留前面的组件，避免高密度配置裁切。
        let heightBudget: CGFloat = isStale ? 72 : 86
        var usedHeight: CGFloat = 0
        return rows.prefix { row in
            let rowHeight = row.map(estimatedHeight).max() ?? 0
            let nextHeight = usedHeight + (usedHeight > 0 ? 4 : 0) + rowHeight
            guard nextHeight <= heightBudget else { return false }
            usedHeight = nextHeight
            return true
        }.map { $0 }
    }

    private func isFullWidth(_ component: ClassIslandActivityComponent) -> Bool {
        ["schedule", "text"].contains(component.kind)
    }

    private func estimatedHeight(_ component: ClassIslandActivityComponent) -> CGFloat {
        switch component.kind {
        case "separator": return 14
        case "schedule":
            return 18 + (component.showProgress && !isStale && state.progressRange != nil ? 7 : 0)
        default: return 16
        }
    }

    @ViewBuilder
    private func componentView(_ component: ClassIslandActivityComponent) -> some View {
        switch component.kind {
        case "separator":
            Rectangle().fill(Color.white.opacity(0.16)).frame(width: 1, height: 14)
        case "schedule":
            HStack(spacing: 6) {
                componentIcon(component)
                Text(component.text).fontWeight(.semibold)
                if !component.secondaryText.isEmpty {
                    Text(component.secondaryText)
                        .foregroundStyle(activitySecondary)
                }
                if !component.extraText.isEmpty {
                    Spacer(minLength: 4)
                    Text(component.extraText)
                        .foregroundStyle(activitySecondary)
                }
            }
            .font(.system(size: 10)).lineLimit(1).minimumScaleFactor(0.65)
            if component.showProgress && !isStale {
                ClassIslandProgressView(state: state).padding(.top, 1)
            }
        case "countdown":
            HStack(spacing: 6) {
                componentIcon(component)
                Text(component.text.isEmpty ? "剩余" : component.text)
                    .font(.system(size: 10)).foregroundStyle(activitySecondary).lineLimit(1)
                countdownValue(component)
                    .font(.system(size: 12, weight: .semibold, design: .rounded))
                    .foregroundStyle(activityAccent).layoutPriority(1)
            }
        case "clock":
            HStack(spacing: 6) {
                componentIcon(component)
                if let targetTime = component.targetTime {
                    let start = Date(timeIntervalSince1970: targetTime)
                    let end = Calendar.current.date(byAdding: .day, value: 1, to: start) ?? start.addingTimeInterval(86400)
                    Text(timerInterval: start...end, countsDown: false, showsHours: true)
                        .font(.system(size: 11)).monospacedDigit().lineLimit(1).minimumScaleFactor(0.75)
                        .multilineTextAlignment(.leading)
                } else {
                    Text(component.text).font(.system(size: 11)).monospacedDigit().lineLimit(1)
                }
            }
        case "date", "weather", "text":
            HStack(spacing: 6) {
                componentIcon(component)
                Text(component.text).font(.system(size: 11)).lineLimit(1).minimumScaleFactor(0.75)
                if !component.secondaryText.isEmpty {
                    Text(component.secondaryText)
                        .font(.system(size: 10)).foregroundStyle(activitySecondary).lineLimit(1)
                }
            }
        default:
            EmptyView()
        }
    }

    @ViewBuilder
    private func countdownValue(_ component: ClassIslandActivityComponent) -> some View {
        if let targetTime = component.targetTime {
            let targetDate = Date(timeIntervalSince1970: targetTime)
            if targetDate > Date() {
                Text(timerInterval: Date()...targetDate,
                     countsDown: true, showsHours: true)
                    .monospacedDigit().lineLimit(1).minimumScaleFactor(0.65)
                    .multilineTextAlignment(.leading)
            } else {
                Text("已到日期")
            }
        } else if isStale {
            Text("待更新")
        } else if let range = state.progressRange {
            Text(timerInterval: range, countsDown: true, showsHours: false)
                .monospacedDigit().lineLimit(1).minimumScaleFactor(0.75)
                .multilineTextAlignment(.leading)
        } else {
            Text(component.secondaryText.isEmpty ? "暂无课程" : component.secondaryText)
                .lineLimit(1)
        }
    }

    @ViewBuilder
    private func componentIcon(_ component: ClassIslandActivityComponent) -> some View {
        if !component.symbolName.isEmpty {
            Image(systemName: component.symbolName)
                .font(.system(size: 12))
                .foregroundStyle(activityAccent)
                .frame(width: 16)
        }
    }
}

struct ClassIslandLockScreenView: View {
    let state: ClassIslandActivityAttributes.ContentState
    let isStale: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: ActivityLayout.sectionSpacing) {
            HStack(spacing: 8) {
                ClassIslandBrandIcon(size: 24)
                Text("ClassIsland").font(.system(size: 15, weight: .semibold))
                Spacer(minLength: 4)
                HStack(spacing: 5) {
                    if state.phase == .onClass {
                        Circle().fill(activityAccent).frame(width: 6, height: 6)
                    } else {
                        Image(systemName: state.phase.symbolName)
                    }
                    Text(state.phase.displayName)
                }
                .font(.system(size: 12, weight: .medium)).foregroundStyle(activityAccent)
            }
            if state.phase == .onClass {
                currentLesson
                if isStale {
                    ClassIslandStaleNotice()
                } else {
                    VStack(spacing: ActivityLayout.rowSpacing) {
                        ClassIslandProgressView(state: state)
                        ClassIslandRemainingTimeView(state: state)
                    }
                }
            } else {
                upcomingLesson
                if isStale {
                    ClassIslandStaleNotice()
                } else if state.progressRange != nil {
                    VStack(spacing: 0) {
                        Label(state.details == nil ? "当前时段剩余" : "距离上课还有", systemImage: "clock")
                            .font(.system(size: 11)).foregroundStyle(activitySecondary)
                        ClassIslandIslandTimer(state: state, isStale: false, textAlignment: .center)
                            .font(.system(size: 26, weight: .semibold, design: .rounded))
                            .foregroundStyle(activityAccent)
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.horizontal, ActivityLayout.timerCardHorizontalPadding)
                    .padding(.vertical, ActivityLayout.timerCardVerticalPadding)
                    .background(
                        Color.white.opacity(0.045),
                        in: RoundedRectangle(cornerRadius: ActivityLayout.timerCardCornerRadius))
                }
            }
        }
        .padding(.horizontal, ActivityLayout.contentHorizontalPadding)
        .padding(.vertical, ActivityLayout.contentVerticalPadding)
        .foregroundStyle(.white)
    }

    private var currentLesson: some View {
        HStack(alignment: .center, spacing: 10) {
            ClassIslandLessonIcon(encodedImage: state.details?.iconPngBase64, size: 26)
            VStack(alignment: .leading, spacing: 3) {
                Text("当前课程").font(.system(size: 11)).foregroundStyle(activitySecondary)
                Text(state.lessonName).font(.system(size: 16, weight: .semibold))
                    .lineLimit(1).minimumScaleFactor(0.75)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            Rectangle().fill(Color.white.opacity(0.08)).frame(width: 1, height: 32)
            if let location = state.details?.location, !location.isEmpty {
                ClassIslandInfoColumn(label: "教室", value: location, symbol: "mappin.and.ellipse")
                    .frame(maxWidth: 70, alignment: .leading)
            }
            ClassIslandInfoColumn(label: "时间", value: state.details?.timeText ?? state.detail, symbol: "clock")
        }
        .frame(minHeight: 40)
    }

    private var upcomingLesson: some View {
        HStack(alignment: .top, spacing: 16) {
            VStack(alignment: .leading, spacing: 4) {
                Label("当前", systemImage: "clock").font(.system(size: 12)).foregroundStyle(activitySecondary)
                Text(state.intervalLabel)
                    .font(.system(size: 14, weight: .medium)).monospacedDigit()
                    .lineLimit(1).minimumScaleFactor(0.75)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            Rectangle().fill(Color.white.opacity(0.08)).frame(width: 1, height: 43)
            HStack(alignment: .top, spacing: 8) {
                ClassIslandLessonIcon(encodedImage: state.details?.iconPngBase64, size: 24)
                VStack(alignment: .leading, spacing: 3) {
                    Text(state.details == nil ? "课程" : "下一节").font(.system(size: 11)).foregroundStyle(activitySecondary)
                    Text(state.lessonName).font(.system(size: 15, weight: .semibold)).lineLimit(1).minimumScaleFactor(0.75)
                    Text(state.details?.timeText ?? state.detail)
                        .font(.system(size: 11)).monospacedDigit().foregroundStyle(activitySecondary).lineLimit(1)
                }
            }
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }
}

private struct ClassIslandInfoColumn: View {
    let label: String
    let value: String
    let symbol: String
    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Label(label, systemImage: symbol).font(.system(size: 11)).foregroundStyle(activitySecondary)
            Text(value).font(.system(size: 12, weight: .medium)).monospacedDigit().lineLimit(1).minimumScaleFactor(0.7)
        }
    }
}

private struct ClassIslandBrandIcon: View {
    let size: CGFloat
    private static let image = Bundle.main.url(forResource: "ClassIslandBrand", withExtension: "png")
        .flatMap { UIImage(contentsOfFile: $0.path) }
    var body: some View {
        if let image = Self.image {
            Image(uiImage: image).resizable().scaledToFit().frame(width: size, height: size).accessibilityHidden(true)
        } else {
            Image(systemName: "book.closed.fill")
                .resizable().scaledToFit().foregroundStyle(activityAccent)
                .frame(width: size, height: size).accessibilityHidden(true)
        }
    }
}

private struct ClassIslandLessonIcon: View {
    let encodedImage: String?
    let size: CGFloat
    var body: some View {
        Group {
            if let encodedImage, let data = Data(base64Encoded: encodedImage), let image = UIImage(data: data) {
                Image(uiImage: image).resizable().scaledToFit()
            } else {
                Image(systemName: "book.closed.fill").resizable().scaledToFit().foregroundStyle(activityAccent)
            }
        }
        .frame(width: size, height: size).accessibilityHidden(true)
    }
}

private struct ClassIslandStaleNotice: View {
    var body: some View {
        Label("课程状态可能已变化，请打开 ClassIsland 更新", systemImage: "exclamationmark.triangle.fill")
            .font(.caption).foregroundStyle(.orange).lineLimit(2)
    }
}

private struct ClassIslandProgressView: View {
    let state: ClassIslandActivityAttributes.ContentState
    var body: some View {
        if let range = state.progressRange {
            // 系统刷新 C# 提供的时间区间，扩展不自行推进课程状态。
            // 显式移除默认的已过时长，避免文字溢出进度条的固定高度。
            ProgressView(timerInterval: range, countsDown: false) {
                EmptyView()
            } currentValueLabel: {
                EmptyView()
            }
            .tint(activityAccent)
            .scaleEffect(x: 1, y: 1.5).frame(height: 6)
        }
    }
}

private struct ClassIslandRemainingTimeView: View {
    let state: ClassIslandActivityAttributes.ContentState
    var body: some View {
        HStack(spacing: 8) {
            Image(systemName: "hourglass").foregroundStyle(activityAccent)
            Text("剩余时间").foregroundStyle(activitySecondary)
            Spacer(minLength: 8)
            ClassIslandIslandTimer(state: state, isStale: false)
        }
        .font(.system(size: 12, weight: .medium))
    }
}

private struct ClassIslandIslandTimer: View {
    let state: ClassIslandActivityAttributes.ContentState
    let isStale: Bool
    var textAlignment: TextAlignment = .trailing
    var body: some View {
        if isStale {
            Image(systemName: "exclamationmark.triangle.fill").foregroundStyle(.orange)
        } else if let range = state.progressRange {
            Text(timerInterval: range, countsDown: true, showsHours: false)
                .monospacedDigit().multilineTextAlignment(textAlignment).lineLimit(1).minimumScaleFactor(0.75)
        } else {
            Text(state.compactText).font(.caption2).lineLimit(1)
        }
    }
}

private extension ClassIslandActivityPhase {
    var displayName: String {
        switch self {
        case .none: return "即将上课"
        case .onClass: return "正在上课"
        case .breaking: return "课间休息"
        case .afterSchool: return "已放学"
        }
    }
    var symbolName: String {
        switch self {
        case .none: return "clock"
        case .onClass: return "book.closed.fill"
        case .breaking: return "cup.and.saucer.fill"
        case .afterSchool: return "house.fill"
        }
    }
}

private extension ClassIslandActivityAttributes.ContentState {
    var lessonName: String { details?.lessonName ?? (compactText.isEmpty ? title : compactText) }
    var intervalLabel: String {
        if let details, !details.intervalTimeText.isEmpty { return details.intervalTimeText }
        return phase.displayName
    }
    var progressRange: ClosedRange<Date>? {
        guard let startTime, let endTime, endTime > startTime else { return nil }
        return startTime...endTime
    }
    var deepLinkURL: URL? { URL(string: deepLink) }
}

private extension ActivityViewContext where Attributes == ClassIslandActivityAttributes {
    var classIslandIsStale: Bool {
        let hasPassedEndTime = state.endTime.map { Date() >= $0 } ?? false
        if #available(iOS 16.2, *) { return isStale || hasPassedEndTime }
        return hasPassedEndTime
    }
}

#if DEBUG
struct ClassIslandLiveActivityPreviews: PreviewProvider {
    static var previews: some View {
        VStack(spacing: 20) {
            ClassIslandLockScreenView(state: fixture(.onClass), isStale: false)
            ClassIslandLockScreenView(state: fixture(.breaking), isStale: false)
            ClassIslandLockScreenView(state: fixture(.onClass), isStale: true)
            ClassIslandExpandedComponents(components: components, state: fixture(.onClass), isStale: false)
                .padding(8)
            ClassIslandExpandedComponents(components: components, state: fixture(.breaking), isStale: true)
                .padding(8)
        }
        .frame(width: 380).background(Color.black).previewLayout(.sizeThatFits)
    }
    static let components: [ClassIslandActivityComponent] = [
        .init(kind: "schedule", text: "高等数学", secondaryText: "A301 · 14:10–14:55",
              symbolName: "book.closed.fill", extraText: "下一节：物理 · 15:10–15:55"),
        .init(kind: "date", text: "10月11日 星期日", symbolName: "calendar"),
        .init(kind: "weather", text: "24 ℃", secondaryText: "多云", symbolName: "cloud.sun.fill"),
        .init(kind: "countdown", text: "课程剩余", symbolName: "hourglass"),
        .init(kind: "countdown", text: "期末考试", symbolName: "calendar.badge.clock",
              targetTime: Date().addingTimeInterval(86400 * 20).timeIntervalSince1970),
        .init(kind: "text", text: "认真学习，按时休息", symbolName: "text.alignleft")
    ]
    static func fixture(_ phase: ClassIslandActivityPhase) -> ClassIslandActivityAttributes.ContentState {
        .init(phase: phase, title: "高等数学", subtitle: "", detail: "14:10–14:55", compactText: "数学",
              startTime: Date().addingTimeInterval(-1800), endTime: Date().addingTimeInterval(900),
              deepLink: "classisland://app/live-activity",
              details: .init(lessonName: phase == .onClass ? "高等数学" : "物理", location: "A301",
                             timeText: phase == .onClass ? "14:10–14:55" : "15:10–15:55",
                             intervalTimeText: "14:55–15:10", iconPngBase64: nil))
    }
}
#endif
