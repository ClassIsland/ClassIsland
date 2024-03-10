namespace ClassIsland.Core.Interfaces;

public interface IAttachedSettingsHelper
{
    public AttachableSettingsObject? AttachedTarget
    {
        get;
        set;
    }
}