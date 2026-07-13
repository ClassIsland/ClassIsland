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
[SupportedOSPlatform("ios13.0")]
internal sealed partial class IosLiveActivityService : ILiveActivityService
{
    private const int MaximumPayloadBytes = 4 * 1024;
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
        var handle = GCHandle.Alloc(operation);
        try
        {
            NativePublishJson(
                json,
                &OnNativeCompleted,
                GCHandle.ToIntPtr(handle));
        }
        catch (Exception exception)
        {
            if (operation.TryComplete(new LiveActivityResult(
                    IsNativeBridgeUnavailable(exception)
                        ? LiveActivityResultCode.Unsupported
                        : LiveActivityResultCode.NativeFailure,
                    ErrorMessage: exception.Message)))
            {
                handle.Free();
            }
        }

        return operation.Task;
    }

    private static unsafe Task<LiveActivityResult> InvokeEndAsync(
        LiveActivityDismissalPolicy dismissalPolicy,
        CancellationToken cancellationToken)
    {
        var operation = new PendingOperation(cancellationToken);
        var handle = GCHandle.Alloc(operation);
        try
        {
            NativeEnd(
                (int)dismissalPolicy,
                &OnNativeCompleted,
                GCHandle.ToIntPtr(handle));
        }
        catch (Exception exception)
        {
            if (operation.TryComplete(new LiveActivityResult(
                    IsNativeBridgeUnavailable(exception)
                        ? LiveActivityResultCode.Unsupported
                        : LiveActivityResultCode.NativeFailure,
                    ErrorMessage: exception.Message)))
            {
                handle.Free();
            }
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
        var handle = GCHandle.FromIntPtr(context);
        if (handle.Target is not PendingOperation operation)
        {
            handle.Free();
            return;
        }

        var code = Enum.IsDefined(typeof(LiveActivityResultCode), resultCode)
            ? (LiveActivityResultCode)resultCode
            : LiveActivityResultCode.NativeFailure;
        var result = new LiveActivityResult(
            code,
            Marshal.PtrToStringUTF8(activityId),
            Marshal.PtrToStringUTF8(errorMessage));

        operation.TryComplete(result);
        handle.Free();
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

    private sealed class PendingOperation
    {
        private readonly TaskCompletionSource<LiveActivityResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _cancellationRegistration;
        private int _nativeCompletionSignaled;

        public PendingOperation(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _cancellationRegistration = cancellationToken.Register(
                static state => ((PendingOperation)state!).Cancel(),
                this);
        }

        public Task<LiveActivityResult> Task => _completion.Task;

        public bool TryComplete(LiveActivityResult result)
        {
            if (Interlocked.Exchange(ref _nativeCompletionSignaled, 1) != 0)
            {
                return false;
            }

            _cancellationRegistration.Dispose();
            if (_cancellationToken.IsCancellationRequested)
            {
                _completion.TrySetCanceled(_cancellationToken);
            }
            else
            {
                _completion.TrySetResult(result);
            }

            return true;
        }

        private void Cancel() => _completion.TrySetCanceled(_cancellationToken);
    }
}
