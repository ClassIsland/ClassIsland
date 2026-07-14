import ActivityKit
import Foundation
import SwiftUI
import WidgetKit

struct ClassIslandLiveActivityWidget: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: ClassIslandActivityAttributes.self) { context in
            ClassIslandLockScreenView(
                state: context.state,
                isStale: context.classIslandIsStale
            )
                .widgetURL(context.state.deepLinkURL)
                .activityBackgroundTint(Color.black.opacity(0.88))
                .activitySystemActionForegroundColor(.white)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    ClassIslandPhaseBadge(phase: context.state.phase)
                }
                DynamicIslandExpandedRegion(.trailing) {
                    ClassIslandIslandTimer(state: context.state)
                }
                DynamicIslandExpandedRegion(.center) {
                    VStack(alignment: .leading, spacing: 2) {
                        Text(context.state.title)
                            .font(.headline)
                            .lineLimit(1)
                        if !context.state.subtitle.isEmpty {
                            Text(context.state.subtitle)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                                .lineLimit(1)
                        }
                    }
                }
                DynamicIslandExpandedRegion(.bottom) {
                    VStack(alignment: .leading, spacing: 6) {
                        if !context.state.detail.isEmpty {
                            Text(context.state.detail)
                                .font(.subheadline)
                                .lineLimit(2)
                        }
                        if context.classIslandIsStale {
                            ClassIslandStaleNotice()
                        } else {
                            ClassIslandProgressView(state: context.state)
                        }
                    }
                }
            } compactLeading: {
                HStack(spacing: 3) {
                    Image(systemName: context.state.phase.symbolName)
                        .foregroundStyle(context.state.phase.tint)
                    if context.state.progressRange != nil,
                       !context.state.compactText.isEmpty {
                        Text(context.state.compactText)
                            .font(.caption2)
                            .lineLimit(1)
                            .minimumScaleFactor(0.7)
                    }
                }
            } compactTrailing: {
                if context.classIslandIsStale {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .foregroundStyle(.orange)
                } else {
                    ClassIslandIslandTimer(state: context.state)
                }
            } minimal: {
                Image(systemName: context.classIslandIsStale
                      ? "exclamationmark.triangle.fill"
                      : context.state.phase.symbolName)
                    .foregroundStyle(context.classIslandIsStale ? .orange : context.state.phase.tint)
            }
            .widgetURL(context.state.deepLinkURL)
            .keylineTint(context.state.phase.tint)
        }
    }
}

private struct ClassIslandLockScreenView: View {
    let state: ClassIslandActivityAttributes.ContentState
    let isStale: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack(alignment: .firstTextBaseline, spacing: 8) {
                ClassIslandPhaseBadge(phase: state.phase)
                VStack(alignment: .leading, spacing: 1) {
                    Text("ClassIsland")
                        .font(.caption2.weight(.semibold))
                        .foregroundStyle(.secondary)
                    Text(state.title)
                        .font(.headline)
                        .lineLimit(1)
                }
                Spacer(minLength: 0)
                if state.progressRange != nil {
                    ClassIslandIslandTimer(state: state)
                }
            }

            if !state.subtitle.isEmpty {
                Text(state.subtitle)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .lineLimit(2)
            }

            if !state.detail.isEmpty {
                Text(state.detail)
                    .font(.subheadline)
                    .lineLimit(2)
            }

            if isStale {
                ClassIslandStaleNotice()
            } else {
                ClassIslandProgressView(state: state)
            }
        }
        .padding(.horizontal, 4)
        .padding(.vertical, 2)
        .foregroundStyle(.white)
    }
}

private struct ClassIslandStaleNotice: View {
    var body: some View {
        Label(
            "课程状态可能已变化，请打开 ClassIsland 更新",
            systemImage: "exclamationmark.triangle.fill"
        )
            .font(.caption)
            .foregroundStyle(.orange)
            .lineLimit(2)
    }
}

private struct ClassIslandPhaseBadge: View {
    let phase: ClassIslandActivityPhase

    var body: some View {
        Image(systemName: phase.symbolName)
            .font(.subheadline.weight(.semibold))
            .foregroundStyle(phase.tint)
            .accessibilityLabel(phase.displayName)
    }
}

private struct ClassIslandProgressView: View {
    let state: ClassIslandActivityAttributes.ContentState

    var body: some View {
        if let progressRange = state.progressRange {
            VStack(alignment: .leading, spacing: 3) {
                // ActivityKit 依据 Date 区间自行刷新进度，不需要每秒从 C# 推送。
                ProgressView(timerInterval: progressRange, countsDown: false)
                    .tint(state.phase.tint)
                HStack {
                    Text(progressRange.lowerBound, style: .time)
                    Spacer(minLength: 4)
                    Text(progressRange.upperBound, style: .time)
                }
                .font(.caption2.monospacedDigit())
                .foregroundStyle(.secondary)
            }
        } else if !state.compactText.isEmpty {
            Text(state.compactText)
                .font(.caption)
                .foregroundStyle(.secondary)
                .lineLimit(1)
        }
    }
}

private struct ClassIslandIslandTimer: View {
    let state: ClassIslandActivityAttributes.ContentState

    var body: some View {
        if let startTime = state.startTime,
           let endTime = state.endTime,
           endTime > startTime {
            Text(
                timerInterval: startTime...endTime,
                pauseTime: endTime,
                countsDown: true,
                showsHours: true
            )
                .font(.caption.monospacedDigit())
                .lineLimit(1)
                .minimumScaleFactor(0.75)
        } else {
            Text(state.compactText)
                .font(.caption2)
                .lineLimit(1)
                .minimumScaleFactor(0.75)
        }
    }
}

private extension ClassIslandActivityPhase {
    var displayName: String {
        switch self {
        case .none:
            return "当前无课程"
        case .onClass:
            return "上课"
        case .breaking:
            return "课间"
        case .afterSchool:
            return "放学"
        }
    }

    var symbolName: String {
        switch self {
        case .none:
            return "clock"
        case .onClass:
            return "book.closed.fill"
        case .breaking:
            return "cup.and.saucer.fill"
        case .afterSchool:
            return "house.fill"
        }
    }

    var tint: Color {
        switch self {
        case .none:
            return .gray
        case .onClass:
            return .blue
        case .breaking:
            return .green
        case .afterSchool:
            return .orange
        }
    }
}

private extension ClassIslandActivityAttributes.ContentState {
    var progressRange: ClosedRange<Date>? {
        guard let startTime, let endTime, endTime > startTime else {
            return nil
        }
        return startTime...endTime
    }

    var deepLinkURL: URL? {
        URL(string: deepLink)
    }
}

private extension ActivityViewContext where Attributes == ClassIslandActivityAttributes {
    var classIslandIsStale: Bool {
        let hasPassedEndTime = state.endTime.map { Date() >= $0 } ?? false
        if #available(iOS 16.2, *) {
            return isStale || hasPassedEndTime
        }
        return hasPassedEndTime
    }
}
