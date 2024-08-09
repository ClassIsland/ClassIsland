using System.Collections.ObjectModel;
using ClassIsland.Core.Enums;
using ClassIsland.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.ProfileAnalyzing;

using AttachableObjectNodeDictionary = ObservableDictionary<AttachableObjectAddress, AttachableObjectNode>;


/// <summary>
/// 代表一个<see cref="AttachableSettingsObject"/>在档案依赖关系图中的节点。
/// </summary>
public class AttachableObjectNode : ObservableRecipient
{
    public AttachableSettingsObject Object { get; set; } = new();

    public AttachedSettingsTargets Target { get; set; } = AttachedSettingsTargets.None;

    public AttachableObjectNodeDictionary PreviousNodes { get;set; } = new(); 

    public AttachableObjectNodeDictionary NextNodes { get; set; } = new();

    public AttachableObjectNodeDictionary RelatedLessons { get; set; } = new();

    public AttachableObjectAddress Address { get; set; } = new AttachableObjectAddress();

}