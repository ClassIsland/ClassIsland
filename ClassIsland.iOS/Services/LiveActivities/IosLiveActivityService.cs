using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.iOS.Services.LiveActivities;

/// <summary>
/// 通过稳定 C ABI 调用薄 Swift ActivityKit 桥；上层只接触 C# DTO。
/// </summary>
[SupportedOSPlatform("ios15.0")]
internal sealed partial class IosLiveActivityService : ILiveActivityService
{
    private const int MaximumPayloadBytes = 4 * 1024;
    private static readonly TimeSpan NativeOperationTimeout = TimeSpan.FromSeconds(15);
    private const string NativeLibrary =
        "@rpath/ClassIslandLiveActivityBridge.framework/ClassIslandLiveActivityBridge";

    public LiveActivityAvailability Availability => GetAvailability().Availability;

    public Task<LiveActivityResult> PublishAsync(
        LessonLiveActivityContent content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<LiveActivityResult>(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(content.IntervalId) ||
            string.IsNullOrWhiteSpace(content.Title))
        {
            return Task.FromResult(new LiveActivityResult(
                LiveActivityResultCode.InvalidContent,
                ErrorMessage: "IntervalId 和 Title 不能为空。"));
        }

        var availability = GetAvailability();
        if (availability.Availability != LiveActivityAvailability.Available)
        {
            return Task.FromResult(new LiveActivityResult(
                availability.Availability == LiveActivityAvailability.Disabled
                    ? LiveActivityResultCode.Disabled
                    : LiveActivityResultCode.Unsupported,
                ErrorMessage: availability.ErrorMessage));
        }

        var payload = new NativeLessonLiveActivityPayload(
            content.IntervalId,
            (int)content.Phase,
            content.Title,
            content.Subtitle,
            content.Detail,
            content.CompactText,
            FormatDate(content.HasProgress ? content.StartTime : null),
            FormatDate(content.HasProgress ? content.EndTime : null),
            content.DeepLink);
        var json = JsonSerializer.Serialize(
            payload,
            LiveActivityJsonContext.Default.NativeLessonLiveActivityPayload);

        if (Encoding.UTF8.GetByteCount(json) > MaximumPayloadBytes)
        {
            return Task.FromResult(new LiveActivityResult(
                LiveActivityResultCode.InvalidContent,
                ErrorMessage: "实时活动内容超过 ActivityKit 的 4 KB 限制。"));
        }

        return InvokePublishAsync(json, cancellationToken);
    }

    public Task<LiveActivityResult> EndAsync(
        LiveActivityDismissalPolicy dismissalPolicy = LiveActivityDismissalPolicy.Default,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<LiveActivityResult>(cancellationToken);
        }

        if (!OperatingSystem.IsIOSVersionAtLeast(16, 1))
        {
            return Task.FromResult(new LiveActivityResult(LiveActivityResultCode.Unsupported));
        }

        return InvokeEndAsync(dismissalPolicy, cancellationToken);
    }

    private static string? FormatDate(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static AvailabilityCheck GetAvailability()
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(16, 1))
        {
            return new AvailabilityCheck(
                LiveActivityAvailability.Unsupported,
                "实时活动需要 iOS 16.1 或更高版本。");
        }

        try
        {
            var value = NativeGetAvailability();
            if (!Enum.IsDefined(typeof(LiveActivityAvailability), value))
            {
                return new AvailabilityCheck(
                    LiveActivityAvailability.Unsupported,
                    $"实时活动原生桥返回了未知可用性值：{value}。");
            }

            var availability = (LiveActivityAvailability)value;
            return new AvailabilityCheck(
                availability,
                availability == LiveActivityAvailability.Disabled
                    ? "系统设置已关闭 ClassIsland 的实时活动。"
                    : null);
        }
        catch (Exception exception) when (IsNativeBridgeUnavailable(exception))
        {
            return new AvailabilityCheck(
                LiveActivityAvailability.Unsupported,
                $"无法加载实时活动原生桥 {NativeLibrary}：" +
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static bool IsNativeBridgeUnavailable(Exception exception) =>
        exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;

    private readonly record struct AvailabilityCheck(
        LiveActivityAvailability Availability,
        string? ErrorMessage);

    private static unsafe Task<LiveActivityResult> InvokePublishAsync(
        string json,
        CancellationToken cancellationToken)
    {
        var operation = new PendingOperation(cancellationToken);
        try
        {
            NativePublishJson(
                json,
                &OnNativeCompleted,
                operation.Context);
            operation.Arm();
        }
        catch (Exception exception)
        {
            operation.CompleteStartupFailure(new LiveActivityResult(
                IsNativeBridgeUnavailable(exception)
                    ? LiveActivityResultCode.Unsupported
                    : LiveActivityResultCode.NativeFailure,
                ErrorMessage: exception.Message));
        }

        return operation.Task;
    }

    private static unsafe Task<LiveActivityResult> InvokeEndAsync(
        LiveActivityDismissalPolicy dismissalPolicy,
        CancellationToken cancellationToken)
    {
        var operation = new PendingOperation(cancellationToken);
        try
        {
            NativeEnd(
                (int)dismissalPolicy,
                &OnNativeCompleted,
                operation.Context);
            operation.Arm();
        }
        catch (Exception exception)
        {
            operation.CompleteStartupFailure(new LiveActivityResult(
                IsNativeBridgeUnavailable(exception)
                    ? LiveActivityResultCode.Unsupported
                    : LiveActivityResultCode.NativeFailure,
                ErrorMessage: exception.Message));
        }

        return operation.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnNativeCompleted(
        nint context,
        int resultCode,
        nint activityId,
        nint errorMessage)
    {
        PendingOperation? operation = null;
        try
        {
            var handle = GCHandle.FromIntPtr(context);
            operation = handle.Target as PendingOperation;
            if (operation == null)
            {
                return;
            }

            var code = Enum.IsDefined(typeof(LiveActivityResultCode), resultCode)
                ? (LiveActivityResultCode)resultCode
                : LiveActivityResultCode.NativeFailure;
            var result = new LiveActivityResult(
                code,
                Marshal.PtrToStringUTF8(activityId),
                Marshal.PtrToStringUTF8(errorMessage));

            operation.CompleteFromNative(result);
        }
        catch (Exception exception)
        {
            // 异常不得跨越 unmanaged callback 边界；若上下文仍有效则由操作对象释放。
            operation?.CompleteFromNative(new LiveActivityResult(
                LiveActivityResultCode.NativeFailure,
                ErrorMessage: exception.Message));
        }
    }

    [LibraryImport(NativeLibrary, EntryPoint = "ci_live_activity_get_availability")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeGetAvailability();

    [LibraryImport(
        NativeLibrary,
        EntryPoint = "ci_live_activity_publish_json",
        StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial void NativePublishJson(
        string json,
        delegate* unmanaged[Cdecl]<nint, int, nint, nint, void> completion,
        nint context);

    [LibraryImport(NativeLibrary, EntryPoint = "ci_live_activity_end")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial void NativeEnd(
        int dismissalPolicy,
        delegate* unmanaged[Cdecl]<nint, int, nint, nint, void> completion,
        nint context);

    [LibraryImport(NativeLibrary, EntryPoint = "ci_live_activity_cancel")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeCancel(nint context);

    private static void RequestNativeCancellation(PendingOperation operation)
    {
        try
        {
            // cancel 返回 1 表示 Swift 已同步完成回调，或确认该上下文不再可能回调。
            // 只有得到该确认后，托管层才可以安全释放 GCHandle。
            if (NativeCancel(operation.Context) != 0)
            {
                operation.CompleteAfterNativeCancellationAcknowledged();
                return;
            }

            operation.CompleteCancellationWithoutOwnershipAcknowledgement(
                new InvalidOperationException("实时活动原生桥未确认取消操作的回调所有权。"));
        }
        catch (Exception exception)
        {
            // ABI 损坏时仍结束调用方等待，但保留 GCHandle，等待可能迟到的原生回调。
            // 在没有原生所有权确认时提前 Free 会造成 use-after-free。
            operation.CompleteCancellationWithoutOwnershipAcknowledgement(exception);
        }
    }

    private sealed class PendingOperation
    {
        private const int Pending = 0;
        private const int CallerCancellationRequested = 1;
        private const int TimeoutRequested = 2;
        private const int CompletingOrCompleted = 3;

        private readonly TaskCompletionSource<LiveActivityResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationToken _cancellationToken;
        private readonly object _lifetimeSync = new();
        private GCHandle _selfHandle;
        private CancellationTokenRegistration _cancellationRegistration;
        private Timer? _timeoutTimer;
        private bool _hasCancellationRegistration;
        private int _state;
        private int _ownsNativeContext = 1;

        public PendingOperation(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _selfHandle = GCHandle.Alloc(this);
            Context = GCHandle.ToIntPtr(_selfHandle);
        }

        public nint Context { get; }

        public Task<LiveActivityResult> Task => _completion.Task;

        public void Arm()
        {
            if (Volatile.Read(ref _state) != Pending)
            {
                return;
            }

            if (_cancellationToken.CanBeCanceled)
            {
                var registration = _cancellationToken.Register(
                    static state => ((PendingOperation)state!).RequestCancellation(
                        CallerCancellationRequested),
                    this);
                StoreCancellationRegistration(registration);
            }

            if (Volatile.Read(ref _state) != Pending)
            {
                return;
            }

            var timeoutTimer = new Timer(
                static state => ((PendingOperation)state!).RequestCancellation(TimeoutRequested),
                this,
                NativeOperationTimeout,
                Timeout.InfiniteTimeSpan);
            StoreTimeoutTimer(timeoutTimer);
        }

        public void CompleteStartupFailure(LiveActivityResult result)
        {
            if (Interlocked.CompareExchange(
                    ref _state,
                    CompletingOrCompleted,
                    Pending) == Pending)
            {
                CompleteCore(Pending, result);
            }
        }

        public void CompleteFromNative(LiveActivityResult result)
        {
            while (true)
            {
                var previousState = Volatile.Read(ref _state);
                if (previousState == CompletingOrCompleted)
                {
                    return;
                }

                if (Interlocked.CompareExchange(
                        ref _state,
                        CompletingOrCompleted,
                        previousState) == previousState)
                {
                    CompleteCore(previousState, result);
                    return;
                }
            }
        }

        public void CompleteAfterNativeCancellationAcknowledged()
        {
            var previousState = Volatile.Read(ref _state);
            if (previousState is not (CallerCancellationRequested or TimeoutRequested))
            {
                return;
            }

            if (Interlocked.CompareExchange(
                    ref _state,
                    CompletingOrCompleted,
                    previousState) == previousState)
            {
                CompleteCore(previousState, new LiveActivityResult(
                    LiveActivityResultCode.Cancelled));
            }
        }

        public void CompleteCancellationWithoutOwnershipAcknowledgement(Exception exception)
        {
            var state = Volatile.Read(ref _state);
            if (state is not (CallerCancellationRequested or TimeoutRequested))
            {
                return;
            }

            DisposeLifetime();
            CompleteCancellationTask(state, exception);
        }

        private void RequestCancellation(int requestedState)
        {
            if (Interlocked.CompareExchange(ref _state, requestedState, Pending) != Pending)
            {
                return;
            }

            RequestNativeCancellation(this);
        }

        private void CompleteCore(int previousState, LiveActivityResult nativeResult)
        {
            DisposeLifetime();
            CompleteCancellationTask(previousState, null, nativeResult);
            ReleaseNativeContext();
        }

        private void CompleteCancellationTask(
            int previousState,
            Exception? cancellationException,
            LiveActivityResult? nativeResult = null)
        {
            switch (previousState)
            {
                case CallerCancellationRequested:
                    _completion.TrySetCanceled(_cancellationToken);
                    break;
                case TimeoutRequested:
                    var detail = cancellationException == null
                        ? null
                        : $" {cancellationException.GetType().Name}: {cancellationException.Message}";
                    _completion.TrySetResult(new LiveActivityResult(
                        LiveActivityResultCode.Cancelled,
                        ErrorMessage: $"实时活动原生桥在 {NativeOperationTimeout.TotalSeconds:0} 秒内未响应，操作已取消。{detail}"));
                    break;
                default:
                    _completion.TrySetResult(nativeResult ?? new LiveActivityResult(
                        LiveActivityResultCode.NativeFailure,
                        ErrorMessage: "实时活动原生桥未返回结果。"));
                    break;
            }
        }

        private void StoreCancellationRegistration(CancellationTokenRegistration registration)
        {
            var unregister = false;
            lock (_lifetimeSync)
            {
                if (Volatile.Read(ref _state) == CompletingOrCompleted)
                {
                    unregister = true;
                }
                else
                {
                    _cancellationRegistration = registration;
                    _hasCancellationRegistration = true;
                }
            }

            if (unregister)
            {
                registration.Unregister();
            }
        }

        private void StoreTimeoutTimer(Timer timeoutTimer)
        {
            var dispose = false;
            lock (_lifetimeSync)
            {
                if (Volatile.Read(ref _state) == CompletingOrCompleted)
                {
                    dispose = true;
                }
                else
                {
                    _timeoutTimer = timeoutTimer;
                }
            }

            if (dispose)
            {
                timeoutTimer.Dispose();
            }
        }

        private void DisposeLifetime()
        {
            CancellationTokenRegistration registration = default;
            Timer? timeoutTimer;
            var unregister = false;
            lock (_lifetimeSync)
            {
                if (_hasCancellationRegistration)
                {
                    registration = _cancellationRegistration;
                    _hasCancellationRegistration = false;
                    unregister = true;
                }

                timeoutTimer = _timeoutTimer;
                _timeoutTimer = null;
            }

            // Unregister 不等待正在执行的取消回调，因此可安全地从回调栈内调用。
            if (unregister)
            {
                registration.Unregister();
            }
            timeoutTimer?.Dispose();
        }

        private void ReleaseNativeContext()
        {
            if (Interlocked.Exchange(ref _ownsNativeContext, 0) != 0)
            {
                _selfHandle.Free();
            }
        }
    }
}
