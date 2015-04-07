using System;

namespace HerhangiOT.GameServer.Enums
{
    [Flags]
    public enum ItemAttributeFlags : uint
    {
        None = 0,
        ActionId = 1 << 0,
        UniqueId = 1 << 1,
        Description = 1 << 2,
        Text = 1 << 3,
        Date = 1 << 4,
        Writer = 1 << 5,
        Name = 1 << 6,
        Article = 1 << 7,
        PluralName = 1 << 8,
        Weight = 1 << 9,
        Attack = 1 << 10,
        Defense = 1 << 11,
        ExtraDefense = 1 << 12,
        Armor = 1 << 13,
        HitChance = 1 << 14,
        ShootRange = 1 << 15,
        Owner = 1 << 16,
        Duration = 1 << 17,
        DecayState = 1 << 18,
        CorpseOwner = 1 << 19,
        Charges = 1 << 20,
        FluidType = 1 << 21,
        DoorId = 1 << 22,
    }
}
