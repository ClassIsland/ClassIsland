using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;

namespace ClassIsland.Core.Controls.CommonDialog;

public partial class DialogAction : ObservableObject
{
    /// <summary>
    /// 操作名称
    /// </summary>
    [ObservableProperty] private string _name = "";
    
    /// <summary>
    /// 图标类型
    /// </summary>
    [ObservableProperty] private MaterialIconKind _materialIconKind = MaterialIconKind.Abacus;
    
    /// <summary>
    /// 是否是主要操作
    /// </summary>
    [ObservableProperty] private bool _isPrimary = false;
}