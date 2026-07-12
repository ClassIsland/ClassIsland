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

    public LiveActivityAvailability Availability
    {
        get
        {
            if (!OperatingSystem.IsIOSVersionAtLeast(16, 1))
            {
                return LiveActivityAvailability.Unsupported;
            }

            try
            {
                var value = NativeGetAvailability();
                return Enum.IsDefined(typeof(LiveActivityAvailability), value)
                    ? (LiveActivityAvailability)value
                    : LiveActivityAvailability.Unsupported;
            }
            catch (Exception exception) when (IsNativeBridgeUnavailable(exception))
            {
                return LiveActivityAvailability.Unsupported;
            }
        }
    }

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

        var availability = Availability;
        if (availability != LiveActivityAvailability.Available)
        {
            return Task.FromResult(new LiveActivityResult(
                availability == LiveActivityAvailability.Disabled
                    ? LiveActivityResultCode.Disabled
                    : LiveActivityResultCode.Unsupported));
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

    private static bool IsNativeBridgeUnavailable(Exception exception) =>
        exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;

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

    [LibraryImport("ClassIslandLiveActivityBridge", EntryPoint = "ci_live_activity_get_availability")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeGetAvailability();

    [LibraryImport(
        "ClassIslandLiveActivityBridge",
        EntryPoint = "ci_live_activity_publish_json",
        StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial void NativePublishJson(
        string json,
        delegate* unmanaged[Cdecl]<nint, int, nint, nint, void> completion,
        nint context);

    [LibraryImport("ClassIslandLiveActivityBridge", EntryPoint = "ci_live_activity_end")]
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
