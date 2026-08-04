using System;

[Flags]
public enum GameLogCategory
{
    None = 0,
    Core = 1 << 0,
    Map = 1 << 1,
    Province = 1 << 2,
    Nation = 1 << 3,
    Economy = 1 << 4,
    AI = 1 << 5,
    AIWar = 1 << 6,
    Army = 1 << 7,
    Battle = 1 << 8,
    Raid = 1 << 9,
    Siege = 1 << 10,
    Turn = 1 << 11,
    UI = 1 << 12,
    Quest = 1 << 13,
    Events = 1 << 14,
    Fog = 1 << 15,
    Supply = 1 << 16,
    All = ~0
}
