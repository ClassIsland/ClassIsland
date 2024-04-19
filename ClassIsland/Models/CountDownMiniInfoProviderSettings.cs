using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class CountDownMiniInfoProviderSettings : ObservableRecipient
{
    private string _countDownName = "";
    private DateTime _overTime = DateTime.Now;

    public string countDownName
    {
        get => _countDownName;
        set
        {
            if (value == null) return;
            if (value.Equals(_countDownName)) return;
            _countDownName = value;
            OnPropertyChanged();
        }
    }

    public DateTime overTime
    {
        get => _overTime;
        set
        {
            if (value == null) return;
            if (value.Equals(_overTime)) return;
            _overTime = value;
            OnPropertyChanged();
        }
    }
}
