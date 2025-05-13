using ClassIsland.Core.Models.Plugin;

namespace ClassIsland.Models.Plugins;

public record class DependencyNode(PluginInfo Plugin) 
{
    public PluginInfo Plugin { get; set; } = Plugin;

    public int DependencyTreeDepth { get; set; } = 0;

    public bool IsDiscovered { get; set; } = false;
}