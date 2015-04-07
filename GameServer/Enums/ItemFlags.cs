using System;

namespace HerhangiOT.GameServer.Enums
{
    [Flags]
    public enum ItemFlags : uint
    {
        BlocksSolid = 1 << 0,
        BlocksProjectile = 1 << 1,
        BlocksPathFinding = 1 << 2,
        HasHeight = 1 << 3,
        Useable = 1 << 4,
        Pickupable = 1 << 5,
        Moveable = 1 << 6,
        Stackable = 1 << 7,
        FloorChangeDown = 1 << 8,
        FloorChangeNorth = 1 << 9,
        FloorChangeEast = 1 << 10,
        FloorChangeSouth = 1 << 11,
        FloorChangeWest = 1 << 12,
        AlwaysOnTop = 1 << 13,
        Readable = 1 << 14,
        Rotatable = 1 << 15,
        Hangable = 1 << 16,
        Vertical = 1 << 17,
        Horizontal = 1 << 18,
        CannotDecay = 1 << 19,
        AllowDistanceRead = 1 << 20,
        Unused = 1 << 21,
        ClientCharges = 1 << 22, //deprecated
        LookThrough = 1 << 23,
        Animation = 1 << 24,
        FullTile = 1 << 25,
        ForceUse = 1 << 26,
    }
}
