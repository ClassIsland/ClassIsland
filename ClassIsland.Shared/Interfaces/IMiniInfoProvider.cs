namespace ClassIsland.Shared.Interfaces;

public interface IMiniInfoProvider
{
    public string Name { get; set; }
    public string Description { get; set; }

    public Guid ProviderGuid { get; set; }

    public object? SettingsElement { get; set; }

    public object InfoElement { get; set; }
}