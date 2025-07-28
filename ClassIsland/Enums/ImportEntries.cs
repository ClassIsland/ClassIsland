using System;

namespace ClassIsland.Enums;

[Flags]
public enum ImportEntries
{
    None        = 0b_0000,
    Profiles    = 0b_0001,
    Settings    = 0b_0010,
    OtherConfig = 0b_0100
}