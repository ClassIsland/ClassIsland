import ActivityKit
import Foundation
import SwiftUI
import UIKit
import WidgetKit

private let activityAccent = Color(red: 0.54, green: 0.83, blue: 1)
private let activitySecondary = Color(red: 0.64, green: 0.69, blue: 0.74)

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
                }
                DynamicIslandExpandedRegion(.trailing) {
                    ClassIslandIslandTimer(state: context.state, isStale: context.classIslandIsStale)
                        .font(.system(size: 18, weight: .semibold, design: .rounded))
                        .foregroundStyle(activityAccent)
                        .frame(width: 72, alignment: .trailing)
                }
                DynamicIslandExpandedRegion(.center) {
                    HStack(spacing: 7) {
                        ClassIslandLessonIcon(encodedImage: context.state.details?.iconPngBase64, size: 22)
                        VStack(alignment: .leading, spacing: 2) {
                            Text(context.state.lessonName).font(.headline).lineLimit(1).minimumScaleFactor(0.75)
                            Text(context.state.phase.displayName).font(.caption).foregroundStyle(activityAccent)
                        }
                    }
                    .foregroundStyle(.white)
                }
                DynamicIslandExpandedRegion(.bottom) {
                    VStack(alignment: .leading, spacing: 8) {
                        HStack {
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
                        }
                    }
                    .foregroundStyle(.white)
                    .padding(.bottom, 8)
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

struct ClassIslandLockScreenView: View {
    let state: ClassIslandActivityAttributes.ContentState
    let isStale: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: state.phase == .onClass ? 12 : 8) {
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
                    VStack(spacing: 8) {
                        ClassIslandProgressView(state: state)
                        HStack(spacing: 8) {
                            Image(systemName: "hourglass").foregroundStyle(activityAccent)
                            Text("剩余时间").foregroundStyle(activitySecondary)
                            Spacer(minLength: 8)
                            ClassIslandIslandTimer(state: state, isStale: false)
                        }
                        .font(.system(size: 12, weight: .medium))
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
                    .padding(.horizontal, 14).padding(.vertical, 5)
                    .background(Color.white.opacity(0.045), in: RoundedRectangle(cornerRadius: 18))
                }
            }
        }
        .padding(.horizontal, 16).padding(.vertical, state.phase == .onClass ? 10 : 7)
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
        .frame(minHeight: 38)
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
            ProgressView(timerInterval: range, countsDown: false).labelsHidden().tint(activityAccent)
                .scaleEffect(x: 1, y: 1.5).frame(height: 6)
        }
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
        }
        .frame(width: 380).background(Color.black).previewLayout(.sizeThatFits)
    }
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
