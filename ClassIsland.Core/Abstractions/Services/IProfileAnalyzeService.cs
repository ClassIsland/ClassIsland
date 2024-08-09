using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;

namespace ClassIsland.Core.Abstractions.Services;

using AttachableObjectNodeDictionary = ObservableDictionary<AttachableObjectAddress, AttachableObjectNode>;

/// <summary>
/// 档案分析服务，用于分析档案中附加信息关系。
/// </summary>
public interface IProfileAnalyzeService : INotifyPropertyChanged
{
    /// <summary>
    /// 当前档案依赖关系节点。
    /// </summary>
    public AttachableObjectNodeDictionary Nodes { get; }

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
    /// <param name="address">当前附加设置位置</param>
    /// <param name="id">附加设置 ID</param>
    /// <param name="requiresEnabled"></param>
    /// <returns></returns>
    List<AttachableObjectNode> FindNextObjects(AttachableObjectAddress address, string id, bool requiresEnabled = true);

    /// <summary>
    /// 查找此位置的附加设置可能覆盖的附加设置。
    /// </summary>
    /// <param name="address">当前附加设置位置</param>
    /// <param name="id">附加设置 ID</param>
    /// <param name="requiresEnabled"></param>
    /// <returns></returns>
    List<AttachableObjectNode> FindPreviousObjects(AttachableObjectAddress address, string id, bool requiresEnabled = true);

}