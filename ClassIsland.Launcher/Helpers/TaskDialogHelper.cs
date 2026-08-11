#if Platforms_Windows
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;

namespace ClassIsland.Launcher.Helpers;

internal static class TaskDialogHelper
{
    private const int FirstButtonId = 1000;

    internal static unsafe (int Index, string Text)? ShowCommands(
        IReadOnlyList<string> commands,
        string title,
        string mainInstruction,
        string? content = null,
        HWND owner = default)
    {
        if (commands.Count == 0)
            throw new ArgumentException("至少需要提供一个命令。", nameof(commands));

        var nativeButtons = new TASKDIALOG_BUTTON[commands.Count];

        // TASKDIALOG_BUTTON 只保存字符串指针，
        // 因此字符串内存必须一直存活到 TaskDialogIndirect 返回。
        var textPointers = new IntPtr[commands.Count];

        try
        {
            for (var i = 0; i < commands.Count; i++)
            {
                textPointers[i] = Marshal.StringToCoTaskMemUni(commands[i]);

                nativeButtons[i] = new TASKDIALOG_BUTTON
                {
                    nButtonID = FirstButtonId + i,
                    pszButtonText = new PCWSTR((char*)textPointers[i])
                };
            }

            fixed (TASKDIALOG_BUTTON* pButtons = nativeButtons)
            fixed (char* pTitle = title)
            fixed (char* pMainInstruction = mainInstruction)
            fixed (char* pContent = content)
            {
                var config = new TASKDIALOGCONFIG
                {
                    cbSize = (uint)sizeof(TASKDIALOGCONFIG),

                    hwndParent = owner,

                    dwFlags =
                        TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS |
                        TASKDIALOG_FLAGS.TDF_ALLOW_DIALOG_CANCELLATION,

                    pszWindowTitle = new PCWSTR(pTitle),
                    pszMainInstruction = new PCWSTR(pMainInstruction),
                    pszContent = new PCWSTR(pContent),

                    cButtons = (uint)nativeButtons.Length,
                    pButtons = pButtons,

                    // 第一个按钮默认获得焦点
                    nDefaultButton = FirstButtonId
                };

                int selectedButton = 0;

                PInvoke.TaskDialogIndirect(
                    config,
                    &selectedButton,
                    null,
                    null
                ).ThrowOnFailure();

                var index = selectedButton - FirstButtonId;

                // 用户可能通过 Esc / Alt+F4 取消
                if (index < 0 || index >= commands.Count)
                    return null;

                return (index, commands[index]);
            }
        }
        finally
        {
            foreach (var ptr in textPointers)
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(ptr);
            }
        }
    }
}
#endif
