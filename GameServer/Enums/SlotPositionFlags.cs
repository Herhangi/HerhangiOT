using System;

namespace HerhangiOT.GameServer.Enums
{
    [Flags]
    public enum SlotPositionFlags : uint
    {
        Nowhere = 0,
        Head = 1 << 0,
        Necklace = 1 << 1,
        Backpack = 1 << 2,
        Armor = 1 << 3,
        RightHand = 1 << 4,
        LeftHand = 1 << 5,
        Legs = 1 << 6,
        Feet = 1 << 7,
        Ring = 1 << 8,
        Ammo = 1 << 9,
        Depot = 1 << 10,
        TwoHand = 1 << 11,
        Hand = (RightHand | LeftHand),
        Wherever = ~Nowhere
    }
}
