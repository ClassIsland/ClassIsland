using System.ComponentModel;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Shared.Interfaces;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 档案分析服务，用于分析档案中附加信息关系。
/// </summary>
public interface IProfileAnalyzeService : INotifyPropertyChanged
{
    /// <summary>
    /// 立即重新分析。
    /// </summary>
    void Analyze();

    /// <summary>
    /// 转储关系图。
    /// </summary>
    string DumpMermaidGraph();

    /// <summary>
    /// 查找可能会覆盖此位置的附加设置的附加设置。
    /// </summary>
    /// <typeparam name="T">附加设置类型</typeparam>
    /// <param name="address">当前附加设置位置</param>
    /// <param name="id">附加设置 ID</param>
    /// <param name="requiresEnabled"></param>
    /// <returns></returns>
    List<T?> FindNextObjects<T>(AttachableObjectAddress address, string id, bool requiresEnabled = true) where T : class, IAttachedSettings;

}