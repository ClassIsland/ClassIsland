using System;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Interfaces;

public interface INotificationProvider
{
    public string Name { get; set; }
    public string Description { get; set; }

    public Guid ProviderGuid { get; set; }

    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; }

}