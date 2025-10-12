using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls.ProfileTransferProviders;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Helpers.ProfileTransferHelpers;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Controls.ProfileTransferProviders;

public class ClassIsland1ImportProvider : GenericImportProviderBase
{
    private IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>();
    
    public ClassIsland1ImportProvider() : base()
    {
        ImportFileHeader = "ClassIsland 1.x 课表文件路径";
        FileTypes =
        [
            new FilePickerFileType("ClassIsland 1.x 课表文件")
            {
                Patterns = ["*.json"]
            }
        ];
        AllowMergeToCurrentProfile = false;
    }
    public override async Task<bool> InvokeTransfer()
    {
        try
        {
            var profile = ClassIslandV1ProfileTransferHelper.TransferClassIslandV1ProfileToClassIslandProfile(SourceFilePath);
            if (ImportType == 1)
            {
                var path = Path.Combine("./Profiles", NewProfileName + ".json");
                if (File.Exists(path))
                {
                    throw new InvalidOperationException($"无法导入课表：{path} 已存在。");
                }
                ConfigureFileHelper.SaveConfig(path, profile);
            }

            this.ShowSuccessToast("导入成功。");
            return true;
        }
        catch (Exception exception)
        {
            var logger = IAppHost.GetService<ILogger<CsesImportProvider>>();
            logger.LogError(exception, "导入 ClassIsland 1.x 课表失败");
            this.ShowErrorToast($"无法导入 ClassIsland 1.x 课表", exception);
            return false;
        }
    }
}