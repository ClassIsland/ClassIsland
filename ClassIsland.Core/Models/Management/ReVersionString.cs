namespace ClassIsland.Core.Models.Management;

public struct ReVersionString
{
    public ReVersionString()
    {
        
    }

    public string? Value { get; set; }

    public int Version { get; set; }

    public bool IsNewerAndNotNull(int version) => !(string.IsNullOrWhiteSpace(Value)) && Version > version;
}