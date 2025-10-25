namespace ClassIsland.Core;

/// <summary>
/// 应用常用路径
/// </summary>
public static class CommonDirectories
{
    /// <summary>
    /// 应用打包根目录
    /// </summary>
    public static string AppPackageRoot { get; internal set; } = "./";

    /// <summary>
    /// 应用数据根目录
    /// </summary>
    public static string AppRootFolderPath { get; internal set; } = "./";

    /// <summary>
    /// 全局数据目录
    /// </summary>
    public static string AppDataFolderPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClassIsland");

    /// <summary>
    /// 应用日志目录
    /// </summary>
    public static string AppLogFolderPath => Path.Combine(AppRootFolderPath, "Logs");

    /// <summary>
    /// 应用配置目录
    /// </summary>
    public static string AppConfigPath => Path.Combine(AppRootFolderPath, "Config");

    /// <summary>
    /// 应用缓存目录
    /// </summary>
    public static string AppCacheFolderPath => string.IsNullOrWhiteSpace(OverrideAppCacheFolderPath) 
        ? Path.Combine(AppRootFolderPath, "Cache") : OverrideAppCacheFolderPath;

    /// <summary>
    /// 应用临时数据目录
    /// </summary>
    public static string AppTempFolderPath => string.IsNullOrWhiteSpace(OverrideAppTempFolderPath)
        ? Path.Combine(AppRootFolderPath, "Temp") : OverrideAppTempFolderPath;


    internal static string? OverrideAppCacheFolderPath { get; set; }
    internal static string? OverrideAppTempFolderPath { get; set; }
}