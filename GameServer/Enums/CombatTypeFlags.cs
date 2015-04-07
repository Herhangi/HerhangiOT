using System;

namespace HerhangiOT.GameServer.Enums
{
    [Flags]
    public enum CombatTypeFlags
    {
        First = 0,
        None = First,
        PhysicalDamage = 1 << 0,
        EnergyDamage = 1 << 1,
        EarthDamage = 1 << 2,
        FireDamage = 1 << 3,
        UndefinedDamage = 1 << 4,
        LifeDrain = 1 << 5,
        ManaDrain = 1 << 6,
        Healing = 1 << 7,
        DrownDamage = 1 << 8,
        IceDamage = 1 << 9,
        HolyDamage = 1 << 10,
        DeathDamage = 1 << 11,
        Last = DeathDamage,
        Count = 12
    }
}
