using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
namespace ClassIsland.Core.Models.Action;

/// <summary>
/// 代表一个行动列表。
/// </summary>
public class ActionList : ObservableRecipient
{
    private string _guid = System.Guid.NewGuid().ToString();
    /// <summary>
    /// 行动列表唯一Guid。
    /// </summary>
    public string Guid
    {
        get => _guid;
        set
        {
            if (value == _guid) return;
            _guid = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<Action> _actions = [];
    public ObservableCollection<Action> Actions
    {
        get => _actions;
        set
        {
            if (value == _actions) return;
            _actions = value;
            OnPropertyChanged();
        }
    }
}