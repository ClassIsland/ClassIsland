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
    case cancelled = 5
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

@available(iOS 16.1, *)
private final class ClassIslandLiveActivityCommandQueue: @unchecked Sendable {
    static let shared = ClassIslandLiveActivityCommandQueue()
    private static let maximumBufferedCommands = 8

    private struct Callback: @unchecked Sendable {
        let completion: ClassIslandLiveActivityCompletion?
        let context: UnsafeMutableRawPointer?

        func call(with result: ClassIslandLiveActivityResult) {
            complete(result, using: completion, context: context)
        }
    }

    /// 一个 callback 上下文的原生所有权状态。`cancel` 返回前会等待正在执行的
    /// callback 返回；因此返回后托管层可以安全释放其 GCHandle。
    private final class Operation: @unchecked Sendable {
        private enum State: Equatable {
            case pending
            case completing
            case completed
        }

        let key: UInt
        private let callback: Callback
        private let condition = NSCondition()
        private var state = State.pending

        init(
            completion: ClassIslandLiveActivityCompletion?,
            context: UnsafeMutableRawPointer?
        ) {
            key = context.map { UInt(bitPattern: $0) } ?? 0
            callback = Callback(completion: completion, context: context)
        }

        var shouldExecute: Bool {
            condition.lock()
            defer { condition.unlock() }
            return state == .pending
        }

        func complete(with result: ClassIslandLiveActivityResult) {
            condition.lock()
            guard state == .pending else {
                condition.unlock()
                return
            }
            state = .completing
            condition.unlock()

            callback.call(with: result)
            markCompleted()
        }

        func cancel() {
            condition.lock()
            while state == .completing {
                condition.wait()
            }
            guard state == .pending else {
                condition.unlock()
                return
            }
            state = .completing
            condition.unlock()

            callback.call(with: .failure(
                .cancelled,
                message: "The live activity operation was cancelled."
            ))
            markCompleted()
        }

        /// GCHandle 地址可在旧 callback 返回前的极短窗口内被运行时复用。
        /// 仅等待已经开始完成的旧操作；真正仍 pending 的重复 context 属于 ABI 误用。
        func waitForCompletionIfStarted() -> Bool {
            condition.lock()
            guard state != .pending else {
                condition.unlock()
                return false
            }
            while state == .completing {
                condition.wait()
            }
            condition.unlock()
            return true
        }

        private func markCompleted() {
            condition.lock()
            state = .completed
            condition.broadcast()
            condition.unlock()
        }
    }

    private enum Command: @unchecked Sendable {
        case publish(Data, Operation)
        case end(Int32, Operation)
        case complete(ClassIslandLiveActivityResult, Operation)

        var operation: Operation {
            switch self {
            case let .publish(_, operation),
                 let .end(_, operation),
                 let .complete(_, operation):
                return operation
            }
        }
    }

    private let registryLock = NSLock()
    private var operations: [UInt: Operation] = [:]
    private let continuation: AsyncStream<Command>.Continuation

    private init() {
        var streamContinuation: AsyncStream<Command>.Continuation?
        let stream = AsyncStream<Command>(
            bufferingPolicy: .bufferingNewest(Self.maximumBufferedCommands)
        ) { continuation in
            streamContinuation = continuation
        }
        continuation = streamContinuation!

        Task { [weak self] in
            for await command in stream {
                guard let self else {
                    command.operation.complete(with: .failure(
                        .nativeFailure,
                        message: "The live activity command queue is unavailable."
                    ))
                    continue
                }
                await self.process(command)
            }
        }
    }

    func publish(
        jsonData: Data,
        completion: ClassIslandLiveActivityCompletion?,
        context: UnsafeMutableRawPointer?
    ) {
        let operation = Operation(completion: completion, context: context)
        enqueue(.publish(
            jsonData,
            operation
        ))
    }

    func end(
        dismissalPolicy: Int32,
        completion: ClassIslandLiveActivityCompletion?,
        context: UnsafeMutableRawPointer?
    ) {
        let operation = Operation(completion: completion, context: context)
        enqueue(.end(
            dismissalPolicy,
            operation
        ))
    }

    func report(
        result: ClassIslandLiveActivityResult,
        completion: ClassIslandLiveActivityCompletion?,
        context: UnsafeMutableRawPointer?
    ) {
        let operation = Operation(completion: completion, context: context)
        enqueue(.complete(
            result,
            operation
        ))
    }

    /// 同步取消并交还 callback 上下文所有权。返回时，该上下文的 callback
    /// 已经返回，或此前已经返回，之后绝不会再次调用。
    func cancel(context: UnsafeMutableRawPointer?) -> Bool {
        let key = context.map { UInt(bitPattern: $0) } ?? 0
        registryLock.lock()
        let operation = operations[key]
        registryLock.unlock()

        guard let operation else {
            // 注册先于 C ABI publish/end 返回；查不到只可能表示 callback 已完成。
            return true
        }

        operation.cancel()
        remove(operation)
        return true
    }

    private func enqueue(_ command: Command) {
        guard register(command.operation) else {
            command.operation.complete(with: .failure(
                .nativeFailure,
                message: "The callback context is already registered."
            ))
            return
        }

        switch continuation.yield(command) {
        case .enqueued(_):
            break
        case let .dropped(droppedCommand):
            // bufferingNewest 会合并过载的旧命令；被替换的 callback 仍须恰好完成一次。
            finish(
                droppedCommand.operation,
                with: .failure(
                    .nativeFailure,
                    message: "The operation was superseded while the native queue was full."
                )
            )
        case .terminated:
            finish(
                command.operation,
                with: .failure(
                    .nativeFailure,
                    message: "The live activity command queue has stopped."
                )
            )
        @unknown default:
            // 即使未来 Swift 新增 yield 状态，也不能让 callback 永久悬挂。
            finish(
                command.operation,
                with: .failure(
                    .nativeFailure,
                    message: "The live activity command could not be queued."
                )
            )
        }
    }

    private func process(_ command: Command) async {
        // cancel 无法中断已经进入 ActivityKit 的 await，但必须阻止仍在 buffer
        // 中的过期命令随后产生副作用。
        guard command.operation.shouldExecute else {
            remove(command.operation)
            return
        }

        let result: ClassIslandLiveActivityResult
        switch command {
        case let .publish(jsonData, _):
            result = await ClassIslandLiveActivityCoordinator.shared.publish(
                jsonData: jsonData
            )
        case let .end(dismissalPolicy, _):
            result = await ClassIslandLiveActivityCoordinator.shared.end(
                dismissalPolicy: dismissalPolicy
            )
        case let .complete(callbackResult, _):
            result = callbackResult
        }

        finish(command.operation, with: result)
    }

    private func register(_ operation: Operation) -> Bool {
        while true {
            registryLock.lock()
            guard let existingOperation = operations[operation.key] else {
                operations[operation.key] = operation
                registryLock.unlock()
                return true
            }
            registryLock.unlock()

            guard existingOperation.waitForCompletionIfStarted() else {
                return false
            }
            remove(existingOperation)
        }
    }

    private func finish(
        _ operation: Operation,
        with result: ClassIslandLiveActivityResult
    ) {
        operation.complete(with: result)
        remove(operation)
    }

    private func remove(_ operation: Operation) {
        registryLock.lock()
        if operations[operation.key] === operation {
            operations.removeValue(forKey: operation.key)
        }
        registryLock.unlock()
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
        complete(
            .failure(.unsupported, message: "Live Activities require iOS 16.1 or later."),
            using: completion,
            context: context
        )
        return
    }

    guard let jsonUTF8, let json = String(validatingUTF8: jsonUTF8) else {
        ClassIslandLiveActivityCommandQueue.shared.report(
            result: .failure(
                .invalidContent,
                message: "json_utf8 must contain valid UTF-8 JSON."
            ),
            completion: completion,
            context: context
        )
        return
    }

    // 调用返回后托管层可以立即释放输入指针，因此必须先复制为 Swift Data。
    let jsonData = Data(json.utf8)
    ClassIslandLiveActivityCommandQueue.shared.publish(
        jsonData: jsonData,
        completion: completion,
        context: context
    )
}

@_cdecl("ci_live_activity_end")
public func ci_live_activity_end(
    _ dismissalPolicy: Int32,
    _ completion: ClassIslandLiveActivityCompletion?,
    _ context: UnsafeMutableRawPointer?
) {
    guard #available(iOS 16.1, *) else {
        complete(
            .failure(.unsupported, message: "Live Activities require iOS 16.1 or later."),
            using: completion,
            context: context
        )
        return
    }

    ClassIslandLiveActivityCommandQueue.shared.end(
        dismissalPolicy: dismissalPolicy,
        completion: completion,
        context: context
    )
}

/// 同步取消指定 callback 上下文。返回 1 时，callback 已返回或以后绝不会再调用，
/// 调用方因而可以安全释放 context 指向的资源。
@_cdecl("ci_live_activity_cancel")
public func ci_live_activity_cancel(
    _ context: UnsafeMutableRawPointer?
) -> Int32 {
    guard #available(iOS 16.1, *) else {
        return 1
    }

    return ClassIslandLiveActivityCommandQueue.shared.cancel(context: context) ? 1 : 0
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
