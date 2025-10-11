using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace ClassIsland.Platforms.Windows.FilePickerHost;

[SupportedOSPlatform("windows")]
public class FileDialogService
{
    private const FILEOPENDIALOGOPTIONS DefaultDialogOptions =
        FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
        FILEOPENDIALOGOPTIONS.FOS_NOVALIDATE | FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE |
        FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT;
    
    public static unsafe string[]? ShowOpenFilesDialog(
        string? title = null,
        string? initialDirectory = null,
        string[]? filters = null,
        bool multiple = false,
        bool folder = false,
        nint root = 0)
    {
        var hr = PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED | COINIT.COINIT_DISABLE_OLE1DDE);
        if (hr.Failed)
            return null;

        try
        {
            var fileOpenDialog = new FileOpenDialog();
            var dialog = (IFileOpenDialog)fileOpenDialog;

            // 允许多选并确保路径/文件存在
            var options = DefaultDialogOptions;
            if (multiple)
            {
                options |= FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT;
            }

            if (folder)
            {
                options |= FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS;
            }
            dialog.SetOptions(options);

            if (!string.IsNullOrEmpty(title))
            {
                dialog.SetTitle(title);
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                var hr2 = PInvoke.SHCreateItemFromParsingName(initialDirectory, null, typeof(IShellItem).GUID, out var shellItem);
                if (hr2.Succeeded && shellItem != null)
                {
                    dialog.SetFolder((IShellItem)shellItem);
                }
            }

            if (filters != null && filters.Length > 0)
            {
                var filterSpecs = new COMDLG_FILTERSPEC[filters.Length / 2];
                for (int i = 0; i < filters.Length / 2; i++)
                {
                    fixed (char* namePtr = filters[i * 2])
                    fixed (char* specPtr = filters[i * 2 + 1])
                    {
                        filterSpecs[i] = new COMDLG_FILTERSPEC
                        {
                            pszName = namePtr,
                            pszSpec = specPtr
                        };
                    }
                }
                dialog.SetFileTypes(filterSpecs);
            }

            dialog.Show(HWND.Null);
            dialog.GetResults(out var resultsArray);
            if (resultsArray != null)
            {
                resultsArray.GetCount(out uint count);
                var paths = new List<string>((int)count);
                for (uint i = 0; i < count; i++)
                {
                    resultsArray.GetItemAt(i, out var item);
                    if (item != null)
                    {
                        item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                        paths.Add(path.ToString());
                    }
                }
                return paths.ToArray();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowOpenFilesDialog: {ex.Message}");
        }
        finally
        {
            PInvoke.CoUninitialize();
        }

        return null;
    }

    public static unsafe string? ShowSaveFileDialog(string? title = null,
        string? initialDirectory = null,
        string? defaultFileName = null,
        string[]? filters = null,
        nint root = 0)
    {
        var hr = PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED | COINIT.COINIT_DISABLE_OLE1DDE);
        if (hr.Failed)
            return null;

        try
        {
            var fileSaveDialog = new FileSaveDialog();
            var dialog = (IFileSaveDialog)fileSaveDialog;
            dialog.SetOptions(DefaultDialogOptions);

            if (!string.IsNullOrEmpty(title))
            {
                dialog.SetTitle(title);
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                var hr2 = PInvoke.SHCreateItemFromParsingName(initialDirectory, null, typeof(IShellItem).GUID, out var shellItem);
                if (hr2.Succeeded && shellItem != null)
                {
                    dialog.SetFolder((IShellItem)shellItem);
                }
            }

            if (!string.IsNullOrEmpty(defaultFileName))
            {
                dialog.SetFileName(defaultFileName);
            }

            if (filters != null && filters.Length > 0)
            {
                var filterSpecs = new COMDLG_FILTERSPEC[filters.Length / 2];
                for (int i = 0; i < filters.Length / 2; i++)
                {
                    fixed (char* namePtr = filters[i * 2])
                    fixed (char* specPtr = filters[i * 2 + 1])
                    {
                        filterSpecs[i] = new COMDLG_FILTERSPEC
                        {
                            pszName = namePtr,
                            pszSpec = specPtr
                        };
                    }
                }
                dialog.SetFileTypes(filterSpecs);
            }

            dialog.Show((HWND)root);
            dialog.GetResult(out var resultItem);
            if (resultItem != null)
            {
                resultItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                return path.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowSaveFileDialog: {ex.Message}");
        }
        finally
        {
            PInvoke.CoUninitialize();
        }

        return null;
    }

    public static unsafe string? ShowFolderDialog(string? title = null, string? initialDirectory = null)
    {
        var hr = PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED | COINIT.COINIT_DISABLE_OLE1DDE);
        if (hr.Failed)
            return null;

        try
        {
            var fileOpenDialog = new FileOpenDialog();
            var dialog = (IFileOpenDialog)fileOpenDialog;

            // 设置为文件夹选择模式
            dialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS | FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST);

            if (!string.IsNullOrEmpty(title))
            {
                dialog.SetTitle(title);
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                var hr2 = PInvoke.SHCreateItemFromParsingName(initialDirectory, null, typeof(IShellItem).GUID, out var shellItem);
                if (hr2.Succeeded && shellItem != null)
                {
                    dialog.SetFolder((IShellItem)shellItem);
                }
            }

            dialog.Show(HWND.Null);
            dialog.GetResult(out var resultItem);
            if (resultItem != null)
            {
                resultItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                return path.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowFolderDialog: {ex.Message}");
        }
        finally
        {
            PInvoke.CoUninitialize();
        }

        return null;
    }
}

[ComImport]
[Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
internal class FileOpenDialog
{
}

[ComImport]
[Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B")]
internal class FileSaveDialog
{
}