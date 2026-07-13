import ActivityKit
import Foundation

public typealias ClassIslandLiveActivityCompletion = @convention(c) (
    UnsafeMutableRawPointer?,
    Int32,
    UnsafePointer<CChar>?,
    UnsafePointer<CChar>?
) -> Void

private enum ClassIslandLiveActivityAvailability: Int32 {
    case unsupported = 0
    case disabled = 1
    case available = 2
}

private enum ClassIslandLiveActivityResultCode: Int32 {
    case succeeded = 0
    case unsupported = 1
    case disabled = 2
    case invalidContent = 3
    case nativeFailure = 4
}

private struct ClassIslandLiveActivityResult {
    let code: ClassIslandLiveActivityResultCode
    let activityId: String?
    let errorMessage: String?

    static func success(activityId: String? = nil) -> Self {
        Self(code: .succeeded, activityId: activityId, errorMessage: nil)
    }

    static func failure(
        _ code: ClassIslandLiveActivityResultCode,
        message: String
    ) -> Self {
        Self(code: code, activityId: nil, errorMessage: message)
    }
}

private enum ClassIslandLiveActivityInputError: LocalizedError {
    case emptyIntervalId
    case emptyTitle
    case invalidDate(String)
    case incompleteProgressRange
    case invalidProgressRange
    case invalidDeepLink
    case payloadTooLarge(Int)
    case invalidDismissalPolicy(Int32)

    var errorDescription: String? {
        switch self {
        case .emptyIntervalId:
            return "intervalId must not be empty."
        case .emptyTitle:
            return "title must not be empty."
        case let .invalidDate(field):
            return "\(field) must be a valid ISO-8601 date."
        case .incompleteProgressRange:
            return "startTime and endTime must either both be set or both be null."
        case .invalidProgressRange:
            return "endTime must be later than startTime."
        case .invalidDeepLink:
            return "deepLink must be an absolute URL."
        case let .payloadTooLarge(size):
            return "The ActivityKit payload is \(size) bytes; the maximum is 4096 bytes."
        case let .invalidDismissalPolicy(value):
            return "Unsupported dismissal policy: \(value)."
        }
    }
}

@available(iOS 16.1, *)
private struct ClassIslandLiveActivityRequest: Decodable {
    let intervalId: String
    let phase: ClassIslandActivityPhase
    let title: String
    let subtitle: String
    let detail: String
    let compactText: String
    let startTime: String?
    let endTime: String?
    let deepLink: String

    func makePayload() throws -> ClassIslandLiveActivityPayload {
        guard !intervalId.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else {
            throw ClassIslandLiveActivityInputError.emptyIntervalId
        }
        guard !title.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else {
            throw ClassIslandLiveActivityInputError.emptyTitle
        }

        let parsedStartTime = try Self.parseDate(startTime, field: "startTime")
        let parsedEndTime = try Self.parseDate(endTime, field: "endTime")
        switch (parsedStartTime, parsedEndTime) {
        case (nil, nil):
            break
        case let (start?, end?) where end > start:
            break
        case (_?, _?):
            throw ClassIslandLiveActivityInputError.invalidProgressRange
        default:
            throw ClassIslandLiveActivityInputError.incompleteProgressRange
        }

        guard let deepLinkURL = URL(string: deepLink), deepLinkURL.scheme != nil else {
            throw ClassIslandLiveActivityInputError.invalidDeepLink
        }

        let payload = ClassIslandLiveActivityPayload(
            attributes: ClassIslandActivityAttributes(intervalId: intervalId),
            state: ClassIslandActivityAttributes.ContentState(
                phase: phase,
                title: title,
                subtitle: subtitle,
                detail: detail,
                compactText: compactText,
                startTime: parsedStartTime,
                endTime: parsedEndTime,
                deepLink: deepLink
            )
        )
        try payload.validateSize()
        return payload
    }

    private static func parseDate(_ value: String?, field: String) throws -> Date? {
        guard let value else {
            return nil
        }

        if let date = ISO8601DateFormatter.classIslandWithFractionalSeconds.date(from: value)
            ?? ISO8601DateFormatter.classIslandInternetDateTime.date(from: value)
            ?? DateFormatter.classIslandDotNetRoundTrip.date(from: value) {
            return date
        }
        throw ClassIslandLiveActivityInputError.invalidDate(field)
    }
}

@available(iOS 16.1, *)
private struct ClassIslandLiveActivityPayload: Encodable {
    let attributes: ClassIslandActivityAttributes
    let state: ClassIslandActivityAttributes.ContentState

    func validateSize() throws {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        let byteCount = try encoder.encode(self).count
        guard byteCount <= 4_096 else {
            throw ClassIslandLiveActivityInputError.payloadTooLarge(byteCount)
        }
    }
}

private extension ISO8601DateFormatter {
    static let classIslandWithFractionalSeconds: ISO8601DateFormatter = {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return formatter
    }()

    static let classIslandInternetDateTime: ISO8601DateFormatter = {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime]
        return formatter
    }()
}

private extension DateFormatter {
    /// `DateTimeOffset.ToString("O")` 默认输出 7 位小数；该 fallback 保证完整兼容。
    static let classIslandDotNetRoundTrip: DateFormatter = {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.dateFormat = "yyyy-MM-dd'T'HH:mm:ss.SSSSSSSXXXXX"
        return formatter
    }()
}

@available(iOS 16.1, *)
private actor ClassIslandLiveActivityCoordinator {
    static let shared = ClassIslandLiveActivityCoordinator()

    private typealias Attributes = ClassIslandActivityAttributes
    private typealias LiveActivity = Activity<Attributes>

    private var activeActivity: LiveActivity?

    func publish(jsonData: Data) async -> ClassIslandLiveActivityResult {
        guard ActivityAuthorizationInfo().areActivitiesEnabled else {
            return .failure(.disabled, message: "Live Activities are disabled in system settings.")
        }

        do {
            let request = try JSONDecoder().decode(
                ClassIslandLiveActivityRequest.self,
                from: jsonData
            )
            let payload = try request.makePayload()
            let activities = LiveActivity.activities
            let matchingActivity = preferredActivity(in: activities)

            if let matchingActivity {
                // Activity attributes 不可变；课程区间和阶段属于 ContentState，
                // 因此跨阶段继续更新现有活动，避免先删除再创建造成锁屏空窗。
                await update(matchingActivity, state: payload.state)
                activeActivity = matchingActivity
                await endAll(
                    activities.filter { $0.id != matchingActivity.id },
                    dismissalPolicy: .immediate
                )
                return .success(activityId: matchingActivity.id)
            }

            let activity = try requestActivity(payload)
            activeActivity = activity
            await endAll(activities, dismissalPolicy: .immediate)
            return .success(activityId: activity.id)
        } catch let error as ClassIslandLiveActivityInputError {
            return .failure(
                .invalidContent,
                message: error.errorDescription ?? "Invalid live activity content."
            )
        } catch is DecodingError {
            return .failure(
                .invalidContent,
                message: "The live activity JSON does not match the expected schema."
            )
        } catch {
            return .failure(
                .nativeFailure,
                message: "ActivityKit could not publish the live activity: \(error.localizedDescription)"
            )
        }
    }

    func end(dismissalPolicy value: Int32) async -> ClassIslandLiveActivityResult {
        do {
            let dismissalPolicy: ActivityUIDismissalPolicy
            switch value {
            case 0:
                dismissalPolicy = .default
            case 1:
                dismissalPolicy = .immediate
            default:
                throw ClassIslandLiveActivityInputError.invalidDismissalPolicy(value)
            }

            let activities = LiveActivity.activities
            let activityId = activeActivity?.id ?? activities.first?.id
            await endAll(activities, dismissalPolicy: dismissalPolicy)
            activeActivity = nil
            return .success(activityId: activityId)
        } catch let error as ClassIslandLiveActivityInputError {
            return .failure(
                .invalidContent,
                message: error.errorDescription ?? "Invalid dismissal policy."
            )
        } catch {
            return .failure(
                .nativeFailure,
                message: "ActivityKit could not end the live activity: \(error.localizedDescription)"
            )
        }
    }

    private func preferredActivity(in activities: [LiveActivity]) -> LiveActivity? {
        if let activeActivity,
           activities.contains(where: { $0.id == activeActivity.id }),
           canUpdate(activeActivity) {
            return activeActivity
        }
        return activities.first { canUpdate($0) }
    }

    private func canUpdate(_ activity: LiveActivity) -> Bool {
        if #available(iOS 16.2, *) {
            switch activity.activityState {
            case .active, .stale:
                return true
            case .dismissed, .ended:
                return false
            @unknown default:
                return false
            }
        } else {
            switch activity.activityState {
            case .active:
                return true
            case .dismissed, .ended:
                return false
            @unknown default:
                return false
            }
        }
    }

    private func requestActivity(_ payload: ClassIslandLiveActivityPayload) throws -> LiveActivity {
        if #available(iOS 16.2, *) {
            return try LiveActivity.request(
                attributes: payload.attributes,
                content: ActivityContent(
                    state: payload.state,
                    staleDate: payload.state.endTime
                ),
                pushType: nil
            )
        }

        return try LiveActivity.request(
            attributes: payload.attributes,
            contentState: payload.state,
            pushType: nil
        )
    }

    private func update(_ activity: LiveActivity, state: Attributes.ContentState) async {
        if #available(iOS 16.2, *) {
            await activity.update(
                ActivityContent(state: state, staleDate: state.endTime)
            )
        } else {
            await activity.update(using: state)
        }
    }

    private func endAll(
        _ activities: [LiveActivity],
        dismissalPolicy: ActivityUIDismissalPolicy
    ) async {
        for activity in activities {
            if #available(iOS 16.2, *) {
                await activity.end(nil, dismissalPolicy: dismissalPolicy)
            } else {
                await activity.end(using: nil, dismissalPolicy: dismissalPolicy)
            }
        }
    }
}

@_cdecl("ci_live_activity_get_availability")
public func ci_live_activity_get_availability() -> Int32 {
    guard #available(iOS 16.1, *) else {
        return ClassIslandLiveActivityAvailability.unsupported.rawValue
    }
    return ActivityAuthorizationInfo().areActivitiesEnabled
        ? ClassIslandLiveActivityAvailability.available.rawValue
        : ClassIslandLiveActivityAvailability.disabled.rawValue
}

@_cdecl("ci_live_activity_publish_json")
public func ci_live_activity_publish_json(
    _ jsonUTF8: UnsafePointer<CChar>?,
    _ completion: ClassIslandLiveActivityCompletion?,
    _ context: UnsafeMutableRawPointer?
) {
    guard #available(iOS 16.1, *) else {
        Task {
            complete(
                .failure(.unsupported, message: "Live Activities require iOS 16.1 or later."),
                using: completion,
                context: context
            )
        }
        return
    }

    guard let jsonUTF8, let json = String(validatingUTF8: jsonUTF8) else {
        Task {
            complete(
                .failure(.invalidContent, message: "json_utf8 must contain valid UTF-8 JSON."),
                using: completion,
                context: context
            )
        }
        return
    }

    // 调用返回后托管层可以立即释放输入指针，因此必须先复制为 Swift Data。
    let jsonData = Data(json.utf8)
    Task {
        let result = await ClassIslandLiveActivityCoordinator.shared.publish(jsonData: jsonData)
        complete(result, using: completion, context: context)
    }
}

@_cdecl("ci_live_activity_end")
public func ci_live_activity_end(
    _ dismissalPolicy: Int32,
    _ completion: ClassIslandLiveActivityCompletion?,
    _ context: UnsafeMutableRawPointer?
) {
    guard #available(iOS 16.1, *) else {
        Task {
            complete(
                .failure(.unsupported, message: "Live Activities require iOS 16.1 or later."),
                using: completion,
                context: context
            )
        }
        return
    }

    Task {
        let result = await ClassIslandLiveActivityCoordinator.shared.end(
            dismissalPolicy: dismissalPolicy
        )
        complete(result, using: completion, context: context)
    }
}

private func complete(
    _ result: ClassIslandLiveActivityResult,
    using completion: ClassIslandLiveActivityCompletion?,
    context: UnsafeMutableRawPointer?
) {
    guard let completion else {
        return
    }

    withOptionalCString(result.activityId) { activityId in
        withOptionalCString(result.errorMessage) { errorMessage in
            completion(context, result.code.rawValue, activityId, errorMessage)
        }
    }
}

private func withOptionalCString<Result>(
    _ value: String?,
    _ body: (UnsafePointer<CChar>?) throws -> Result
) rethrows -> Result {
    guard let value else {
        return try body(nil)
    }
    return try value.withCString(body)
}
