namespace ClassIsland.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ProviderInfo(string guid, string name="", string description="") : Attribute
{
    public Guid Guid { get; } = new Guid(guid);

    public string Name { get; } = name;

    public string Description { get; } = description;
}