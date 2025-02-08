namespace ClassIsland.Models.EventArgs;

public class UriTriggerHandledEventArgs(string name)
{
    public string Name { get; } = name;
}