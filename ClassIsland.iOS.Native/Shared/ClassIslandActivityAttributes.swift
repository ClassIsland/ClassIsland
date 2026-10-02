import ActivityKit
import Foundation

/// C# 提供的展示数据，扩展不解析课表和课程图标表达式。
@available(iOS 16.1, *)
public struct ClassIslandLessonDetails: Codable, Hashable {
    public let lessonName: String
    public let location: String
    public let timeText: String
    public let intervalTimeText: String
    public let iconPngBase64: String?
}

/// 与托管层 `LessonLiveActivityPhase` 数值保持一致。
@available(iOS 16.1, *)
public enum ClassIslandActivityPhase: Int, Codable, Hashable {
    case none = 0
    case onClass = 1
    case breaking = 2
    case afterSchool = 3
}

/// 主应用与 Widget Extension 唯一共享的 ActivityKit schema。
@available(iOS 16.1, *)
public struct ClassIslandActivityAttributes: ActivityAttributes {
    public struct ContentState: Codable, Hashable {
        public let phase: ClassIslandActivityPhase
        public let title: String
        public let subtitle: String
        public let detail: String
        public let compactText: String
        public let startTime: Date?
        public let endTime: Date?
        public let deepLink: String
        public let details: ClassIslandLessonDetails?

        public init(
            phase: ClassIslandActivityPhase,
            title: String,
            subtitle: String,
            detail: String,
            compactText: String,
            startTime: Date?,
            endTime: Date?,
            deepLink: String,
            details: ClassIslandLessonDetails? = nil
        ) {
            self.phase = phase
            self.title = title
            self.subtitle = subtitle
            self.detail = detail
            self.compactText = compactText
            self.startTime = startTime
            self.endTime = endTime
            self.deepLink = deepLink
            self.details = details
        }
    }

    /// 创建活动时的课程区间标识；后续课程与阶段变化通过 ContentState 平滑更新。
    public let intervalId: String

    public init(intervalId: String) {
        self.intervalId = intervalId
    }
}
