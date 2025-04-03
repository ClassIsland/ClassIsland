namespace ClassIsland.Models.EventArgs;

public class MainWindowAnimationEventArgs(string? storyboardName) : System.EventArgs
{
    public string? StoryboardName { get; } = storyboardName;
}