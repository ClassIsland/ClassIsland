using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 组件服务。
/// </summary>
public interface IComponentsService : INotifyPropertyChanged
{
    public ObservableCollection<ComponentSettings> CurrentComponents { get; set; }
}