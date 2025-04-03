using System;
using System.Collections.ObjectModel;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Services;

public class AttachedSettingsHostService : IAttachedSettingsHostService
{
    public ObservableCollection<Type> TimePointSettingsAttachedSettingsControls { get; } = new();
    public ObservableCollection<Type> TimeLayoutSettingsAttachedSettingsControls { get; } = new();
    public ObservableCollection<Type> ClassPlanSettingsAttachedSettingsControls { get; } = new();
    public ObservableCollection<Type> SubjectSettingsAttachedSettingsControls { get; } = new();
}