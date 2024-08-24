using System.Runtime.InteropServices;
using ClassIsland.Core.Helpers.Native;

namespace ClassIsland.Core;

/// <summary>
/// 可以自动释放的 <see cref="PWSTR"/>。
/// </summary>
public class DisposablePWSTR : IDisposable
{
    /// <summary>
    /// <see cref="PWSTR"/> 实例。
    /// </summary>
    public PWSTR PWSTR { get; }

    private nint Pointer { get; }

    /// <summary>
    /// 初始化<see cref="DisposablePWSTR"/>实例。
    /// </summary>
    /// <param name="size">缓冲区大小。</param>
    public DisposablePWSTR(int size)
    {
        PWSTR = NativeWindowHelper.BuildPWSTR(size, out var ptr);
        Pointer = ptr;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Marshal.FreeHGlobal(Pointer);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return PWSTR.ToString();
    }
}