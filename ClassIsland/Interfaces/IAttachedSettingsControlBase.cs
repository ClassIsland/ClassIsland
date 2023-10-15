using System;
using System.ComponentModel;

namespace ClassIsland.Interfaces;

public interface IAttachedSettingsControlBase
{
    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; }
}