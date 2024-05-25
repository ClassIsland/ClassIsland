using System;

using ClassIsland.Core.Models.Profile;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class ClassChangingViewModel : ObservableRecipient
{
    private bool _writeToSourceClassPlan = false;
    private int _slideIndex = 0;
    private bool _isSwapMode = true;
    private int _sourceIndex = -1;
    private int _swapModeTargetIndex = -1;
    private Subject? _targetSubject;
    private bool _isAutoNextStep = false;
    private string? _targetSubjectIndex;

    public bool WriteToSourceClassPlan
    {
        get => _writeToSourceClassPlan;
        set
        {
            if (value == _writeToSourceClassPlan) return;
            _writeToSourceClassPlan = value;
            OnPropertyChanged();
        }
    }

    public int SlideIndex
    {
        get => _slideIndex;
        set
        {
            if (value == _slideIndex) return;
            _slideIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsSwapMode
    {
        get => _isSwapMode;
        set
        {
            if (value == _isSwapMode) return;
            _isSwapMode = value;
            OnPropertyChanged();
        }
    }

    public int SwapModeTargetIndex
    {
        get => _swapModeTargetIndex;
        set
        {
            if (value == _swapModeTargetIndex) return;
            _swapModeTargetIndex = value;
            OnPropertyChanged();
        }
    }

    public Subject? TargetSubject
    {
        get => _targetSubject;
        set
        {
            if (Equals(value, _targetSubject)) return;
            _targetSubject = value;
            OnPropertyChanged();
        }
    }

    public string? TargetSubjectIndex
    {
        get => _targetSubjectIndex;
        set
        {
            if (value == _targetSubjectIndex) return;
            _targetSubjectIndex = value;
            OnPropertyChanged();
        }
    }

    public int SourceIndex
    {
        get => _sourceIndex;
        set
        {
            if (value == _sourceIndex) return;
            _sourceIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsAutoNextStep
    {
        get => _isAutoNextStep;
        set
        {
            if (value == _isAutoNextStep) return;
            _isAutoNextStep = value;
            OnPropertyChanged();
        }
    }

    public Guid DialogIdentifier { get; } = Guid.NewGuid();
}