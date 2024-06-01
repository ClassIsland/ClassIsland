using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Profile;

using Microsoft.Extensions.Hosting;

namespace ClassIsland.Services;

public class AttachedSettingsHostService : IHostedService, IAttachedSettingsHostService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    public ObservableCollection<Type> TimePointSettingsAttachedSettingsControls { get; } = new();
    public ObservableCollection<Type> TimeLayoutSettingsAttachedSettingsControls { get; } = new();
    public ObservableCollection<Type> ClassPlanSettingsAttachedSettingsControls { get; } = new();
    public ObservableCollection<Type> SubjectSettingsAttachedSettingsControls { get; } = new();
}