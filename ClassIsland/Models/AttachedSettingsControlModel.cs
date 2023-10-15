using ClassIsland.Interfaces;

namespace ClassIsland.Models;

public class AttachedSettingsControlModel
{
    public IAttachedSettingsControlBase Control { get; }

    public AttachedSettingsControlModel(IAttachedSettingsControlBase c)
    {
        Control = c;
    }
}