namespace ClassIsland.Models.EventArgs;

public class SignalTriggerEventArgs(string signalName, bool revert)
{
    public string SignalName { get; } = signalName;
    public bool Revert { get; } = revert;
}