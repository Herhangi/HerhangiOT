using System;

namespace HerhangiOT.GameServer.Enums
{
    [Flags]
    public enum TileFlags : uint
    {
        None = 0,
        ProtectionZone = 1 << 0,
        DeprecatedHouse = 1 << 1,
        NoPvpZone = 1 << 2,
        NoLogout = 1 << 3,
        PvpZone = 1 << 4,
        Refresh = 1 << 5, // unused

        //internal usage
        House = 1 << 6,
        FloorChange = 1 << 7,
        FloorChangeDown = 1 << 8,
        FloorchangeNorth = 1 << 9,
        FloorChangeSouth = 1 << 10,
        FloorChangeEast = 1 << 11,
        FloorChangeWest = 1 << 12,
        Teleport = 1 << 13,
        MagicField = 1 << 14,
        MailBox = 1 << 15,
        TrashHolder = 1 << 16,
        Bed = 1 << 17,
        Depot = 1 << 18,
        BlockSolid = 1 << 19,
        BlockPath = 1 << 20,
        ImmovableBlockSolid = 1 << 21,
        ImmovableBlockPath = 1 << 22,
        ImmovableNoFieldBlockPath = 1 << 23,
        NoFieldBlockPath = 1 << 24,
        DynamicTile = 1 << 25,
        FloorChangeSouthAlt = 1 << 26,
        FloorChangeEastAlt = 1 << 27,
        SupportsHangable = 1 << 28,
    }
}
