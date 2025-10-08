using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using Windows.Win32;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Windows.FilePickerHost.Models;

namespace ClassIsland.Platforms.Windows.FilePickerHost;

class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        PInvoke.SetCurrentProcessExplicitAppUserModelID("cn.classisland.app");
        if (args.Length < 2)
        {
            return 1;
        }
        var handle = args[0];
        var argsJson = args[1];
        var pickerArgs =
            JsonSerializer.Deserialize<FilePickerHostArguments>(
                Encoding.UTF8.GetString(Convert.FromBase64String(argsJson)));
        if (pickerArgs == null || pickerArgs.Mode == FilePickerHostArguments.FilePickerMode.None)
        {
            return 1;
        }

        var optionsObj = pickerArgs.Options is JsonElement ? (JsonElement)pickerArgs.Options : default;
        using var client = new AnonymousPipeClientStream(PipeDirection.Out, handle);
        using var writer = new StreamWriter(client, Encoding.UTF8);
        var resultString = "";

        switch (pickerArgs.Mode)
        {
            case FilePickerHostArguments.FilePickerMode.OpenFile:
                var options1 = optionsObj.Deserialize<FilePickerOpenOptions>();
                if (options1 == null)
                {
                    return 1;
                }

                List<string> filters = [];
                foreach (var filter in options1.FileTypeFilter ?? [])
                {
                    filters.Add(filter.Name);
                    filters.Add(string.Join(";", filter.Patterns ?? []));
                }

                var results = FileDialogService.ShowOpenFilesDialog(
                    title: options1.Title ?? "选择要打开的文件",
                    initialDirectory: options1.SuggestedStartLocation?.ToString() ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    filters: filters.ToArray(),
                    multiple: options1.AllowMultiple,
                    folder: false
                );
                resultString = JsonSerializer.Serialize(results ?? []);
                break;
            case FilePickerHostArguments.FilePickerMode.OpenFolder:
                var options = optionsObj.Deserialize<FilePickerOpenOptions>();
                if (options == null)
                {
                    return 1;
                }
                
                var folderOptions = optionsObj.Deserialize<FolderPickerOpenOptions>();
                if (folderOptions == null)
                {
                    return 1;
                }
                var folders = FileDialogService.ShowOpenFilesDialog(
                    title: folderOptions.Title ?? "选择文件夹",
                    initialDirectory: folderOptions.SuggestedStartLocation?.ToString() ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    multiple: options.AllowMultiple,
                    folder: true
                );
                resultString = JsonSerializer.Serialize(folders ?? []);
                break;
            case FilePickerHostArguments.FilePickerMode.SaveFile:
                var saveOptions = optionsObj.Deserialize<FilePickerSaveOptions>();
                if (saveOptions == null)
                {
                    return 1;
                }
                var saveResult = FileDialogService.ShowSaveFileDialog(
                    title: saveOptions.Title ?? "保存文件",
                    initialDirectory: saveOptions.SuggestedStartLocation?.ToString() ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    defaultFileName: saveOptions.SuggestedFileName,
                    filters: (saveOptions.FileTypeChoices ?? new List<FilePickerFileType>()).SelectMany(f => new[] { f.Name, string.Join(";", f.Patterns ?? Array.Empty<string>()) }).ToArray()
                );
                resultString = JsonSerializer.Serialize(saveResult ?? "");
                break;
            case FilePickerHostArguments.FilePickerMode.None:
            default:
                break;
        }
        
        writer.Write(resultString);
        writer.Flush();

        return 0;
    }
    
}