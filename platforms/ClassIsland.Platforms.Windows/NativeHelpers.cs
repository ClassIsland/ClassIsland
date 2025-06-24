using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClassIsland.Platform.Windows;

public static class NativeHelpers
{
    public static unsafe PWSTR BuildPWSTR(int bufferSize, out nint ptr)
    {
        ptr = Marshal.AllocHGlobal(bufferSize * sizeof(char));
        return new PWSTR((char*)ptr.ToPointer());
    }

    public static void ThrowIfUnSuccess(int result)
    {
        if (result != 0)
        {
            throw new Win32Exception(result);
        }
    }
}