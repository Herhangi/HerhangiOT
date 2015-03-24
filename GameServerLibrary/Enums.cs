using System;

namespace HerhangiOT.GameServerLibrary
{
    public enum LocationType
    {
        Container,
        Slot,
        Ground
    }

    public enum ItemGroup
    {
        None = 0,
        Ground,
        Container,
        Weapon, //deprecated
        Ammunition, //deprecated
        Armor, //deprecated
        Charges,
        Teleport, //deprecated
        MagicField, //deprecated
        Writeable, //deprecated
        Key, //deprecated
        Splash,
        Fluid,
        Door, //deprecated
        Deprecated,
        Last
    }

    public enum ItemTypes
    {
        None = 0,
        Depot,
        Mailbox,
        ThrashHolder,
        Container,
        Door,
        MagicField,
        Teleport,
        Bed,
        Key,
        Rune,
        Last
    };

    [Flags]
    public enum SlotPositions : uint
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

    [Flags]
    public enum CombatTypes : ushort
    {
        First = 0,
        None = First,
        PhysicalDamage = 1,
        EnergyDamage = 2,
        EarthDamage = 4,
        FireDamage = 8,
        UndefinedDamage = 16,
        LifeDrain = 32,
        ManaDrain = 64,
        Healing = 128,
        DrownDamage = 256,
        IceDamage = 512,
        HolyDamage = 1024,
        DeathDamage = 2048,
        Last = DeathDamage
    };
    public enum FluidColors : byte {
	    Empty = 0x00,
	    Blue = 0x01,
	    Red = 0x02,
	    Brown = 0x03,
	    Green = 0x04,
	    Yellow = 0x05,
	    White = 0x06,
	    Purple = 0x07
    };

    public enum WeaponType : byte
    {
        None,
        Sword,
        Club,
        Axe,
        Shield,
        Distance,
        Wand,
        Ammunition
    }
    public enum AmmoType : byte
    {
        None = 0,
        Bolt,
        Arrow,
        Spear,
        ThrowingStar,
        ThrowingKnife,
        Stone,
        Snowball
    }
    public enum ProjectileType : byte
    {
        None = 0,
        Spear = 1,
        Bolt = 2,
        Arrow = 3,
        Fire = 4,
        Energy = 5,
        PoisonArrow = 6,
        BurstArrow = 7,
        ThrowingStar = 8,
        ThrowingKnife = 9,
        SmallStone = 10,
        Death = 11,
        LargeRock = 12,
        Snowball = 13,
        PowerBolt = 14,
        PoisonField = 15,
        InfernalBolt = 16,
        HuntingSpear = 17,
        EnchantedSpear = 18,
        RedStar = 19,
        GreenStar = 20,
        RoyalSpear = 21,
        SniperArrow = 22,
        OnyxArrow = 23,
        PiercingBolt = 24,
        WhirlwindSword = 25,
        WhirlwindAxe = 26,
        WhirlwindClub = 27,
        EtherealSpear = 28,
        Ice = 29,
        Earth = 30,
        Holy = 31,
        SuddenDeath = 32,
        FlashArrow = 33,
        FlamingArrow = 34,
        ShiverArrow = 35,
        EnergyBall = 36,
        SmallIce = 37,
        SmallHoly = 38,
        SmallEarth = 39,
        EarthArrow = 40,
        Explosion = 41,
        Cake = 42,

        TarsalArrow = 44,
        VortexBolt = 45,

        PrismaticBolt = 48,
        CrystallineArrow = 49,
        DrillBolt = 50,
        EnvenomedArrow = 51,

        //for internal use, dont send to client
        WeaponType = 0xFE
    }
    public enum CorpseType : byte
    {
        None = 0,
        Venom,
        Blood,
        Undead,
        Fire,
        Energy
    }
    public enum FluidTypes : byte {
	    None = FluidColors.Empty,
        Water = FluidColors.Blue,
        Blood = FluidColors.Red,
        Beer = FluidColors.Brown,
        Slime = FluidColors.Green,
        Lemonade = FluidColors.Yellow,
        Milk = FluidColors.White,
        Purple = FluidColors.Purple,

        Life = FluidColors.Red + 8,
        Oil = FluidColors.Brown + 8,
        Urine = FluidColors.Yellow + 8,
        CoconutMilk = FluidColors.White + 8,
        Wine = FluidColors.Purple + 8,

        Mud = FluidColors.Brown + 16,
        FruitJuice = FluidColors.Yellow + 16,

        Lava = FluidColors.Red + 24,
        Rum = FluidColors.Brown + 24,
        Swamp = FluidColors.Green + 24,

        Tea = FluidColors.Brown + 32,
        Mead = FluidColors.Brown + 40
    }

    public enum FloorChangeDirection
    {
        None,
        Up,
        Down,
        North,
        South,
        West,
        East
    }

    [FlagsAttribute]
    public enum ItemFlags : uint
    {
        BlocksSolid = 1,
        BlocksProjectile = 2,
        BlocksPathFinding = 4,
        HasHeight = 8,
        Useable = 16,
        Pickupable = 32,
        Moveable = 64,
        Stackable = 128,
        FloorChangeDown = 256,
        FloorChangeNorth = 512,
        FloorChangeEast = 1024,
        FloorChangeSouth = 2048,
        FloorChangeWest = 4096,
        AlwaysOnTop = 8192,
        Readable = 16384,
        Rotatable = 32768,
        Hangable = 65536,
        Vertical = 131072,
        Horizontal = 262144,
        CannotDecay = 524288,
        AllowDistanceRead = 1048576,
        Unused = 2097152,
        ClientCharges = 4194304,
        LookThrough = 8388608
    }
}
