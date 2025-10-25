using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Platforms.Windows.FilePickerHost.Models;

namespace ClassIsland.Platform.Windows.Services;

public class PlatformFilePickerService : IPlatformFilePickerService
{
    private const string FilePickerHostExe = "ClassIsland.Platforms.Windows.FilePickerHost.exe";

    private async Task<string?> StartFilePickerHost(FilePickerHostArguments args)
    {
        var jsonB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(args)));
        await using var server = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        var psi = new ProcessStartInfo(FilePickerHostExe)
        {
            UseShellExecute = false,
            ArgumentList =
            {
                server.GetClientHandleAsString(),
                jsonB64
            },
            RedirectStandardOutput = false,
            CreateNoWindow = true
        };

        using var child = Process.Start(psi);
        if (child == null)
        {
            return null;
        }
        server.DisposeLocalCopyOfClientHandle();
        await child.WaitForExitAsync();
        var returns = await (new StreamReader(server).ReadToEndAsync());
        return returns;
    }
    
    public async Task<List<string>> OpenFilesPickerAsync(FilePickerOpenOptions options, TopLevel root)
    {
        var serializableOptions = new FilePickerOpenOptions
        {
            Title = options.Title,
            AllowMultiple = options.AllowMultiple,
            FileTypeFilter = options.FileTypeFilter
        };
        var args = new FilePickerHostArguments
        {
            Mode = FilePickerHostArguments.FilePickerMode.OpenFile,
            Options = serializableOptions,
            ParentHWnd = (int)(root.TryGetPlatformHandle()?.Handle ?? nint.Zero)
        };
        var result = await StartFilePickerHost(args);
        if (string.IsNullOrEmpty(result))
        {
            return new List<string>();
        }
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(result) ?? new List<string>();
            return list;
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<string?> SaveFilePickerAsync(FilePickerSaveOptions options, TopLevel root)
    {
        var serializableOptions = new FilePickerSaveOptions
        {
            Title = options.Title,
            SuggestedFileName = options.SuggestedFileName,
            FileTypeChoices = options.FileTypeChoices
        };
        var args = new FilePickerHostArguments
        {
            Mode = FilePickerHostArguments.FilePickerMode.SaveFile,
            Options = serializableOptions,
            ParentHWnd = (int)(root.TryGetPlatformHandle()?.Handle ?? nint.Zero)
        };
        var result = await StartFilePickerHost(args);
        if (string.IsNullOrEmpty(result))
        {
            return null;
        }
        try
        {
            var path = JsonSerializer.Deserialize<string?>(result);
            return path;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<string>> OpenFoldersPickerAsync(FolderPickerOpenOptions options, TopLevel root)
    {
        var serializableOptions = new FolderPickerOpenOptions
        {
            Title = options.Title,
            AllowMultiple = options.AllowMultiple
        };
        var args = new FilePickerHostArguments
        {
            Mode = FilePickerHostArguments.FilePickerMode.OpenFolder,
            Options = serializableOptions,
            ParentHWnd = (int)(root.TryGetPlatformHandle()?.Handle ?? nint.Zero)
        };
        var result = await StartFilePickerHost(args);
        if (string.IsNullOrEmpty(result))
        {
            return new List<string>();
        }
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(result) ?? new List<string>();
            return list;
        }
        catch
        {
            return new List<string>();
        }
    }
}