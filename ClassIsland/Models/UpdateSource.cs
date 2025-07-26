﻿using System.Collections.Generic;
using ClassIsland.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class UpdateSource: ObservableRecipient
{
    private string _name = "";
    private List<string> _speedTestSources = new();
    private SpeedTestResult _speedTestResult = new();

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public List<string> SpeedTestSources
    {
        get => _speedTestSources;
        set
        {
            if (Equals(value, _speedTestSources)) return;
            _speedTestSources = value;
            OnPropertyChanged();
        }
    }

    public SpeedTestResult SpeedTestResult
    {
        get => _speedTestResult;
        set
        {
            if (Equals(value, _speedTestResult)) return;
            _speedTestResult = value;
            OnPropertyChanged();
        }
    }
}