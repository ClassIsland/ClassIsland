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

@available(iOS 16.1, *)
public struct ClassIslandActivityComponent: Codable, Hashable {
    public let kind: String
    public let text: String
    public let secondaryText: String
    public let symbolName: String
    public let extraText: String
    public let targetTime: Double?
    public let showProgress: Bool

    public init(
        kind: String,
        text: String = "",
        secondaryText: String = "",
        symbolName: String = "",
        extraText: String = "",
        targetTime: Double? = nil,
        showProgress: Bool = true
    ) {
        self.kind = kind
        self.text = text
        self.secondaryText = secondaryText
        self.symbolName = symbolName
        self.extraText = extraText
        self.targetTime = targetTime
        self.showProgress = showProgress
    }

    private enum CodingKeys: String, CodingKey {
        case kind, text, secondaryText, symbolName, extraText, targetTime, showProgress
    }

    public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        kind = try container.decode(String.self, forKey: .kind)
        text = try container.decodeIfPresent(String.self, forKey: .text) ?? ""
        secondaryText = try container.decodeIfPresent(String.self, forKey: .secondaryText) ?? ""
        symbolName = try container.decodeIfPresent(String.self, forKey: .symbolName) ?? ""
        extraText = try container.decodeIfPresent(String.self, forKey: .extraText) ?? ""
        targetTime = try container.decodeIfPresent(Double.self, forKey: .targetTime)
        showProgress = try container.decodeIfPresent(Bool.self, forKey: .showProgress) ?? true
    }
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
        public let components: [ClassIslandActivityComponent]?

        public init(
            phase: ClassIslandActivityPhase,
            title: String,
            subtitle: String,
            detail: String,
            compactText: String,
            startTime: Date?,
            endTime: Date?,
            deepLink: String,
            details: ClassIslandLessonDetails? = nil,
            components: [ClassIslandActivityComponent]? = nil
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
            self.components = components
        }
    }

    /// 创建活动时的课程区间标识；后续课程与阶段变化通过 ContentState 平滑更新。
    public let intervalId: String

    public init(intervalId: String) {
        self.intervalId = intervalId
    }
}
