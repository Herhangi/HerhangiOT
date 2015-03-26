using System;

namespace HerhangiOT.GameServerLibrary
{
    #region Flag Enums
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
    [Flags]
    public enum CombatTypes : ushort
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
        ClientCharges = 1 << 22,
        LookThrough = 1 << 23,
        Animation = 1 << 24,
        FullTile = 1 << 25
    }
    [Flags]
    public enum ConditionFlags : uint
    {
        None = 0,
        Poison = 1 << 0,
        Fire = 1 << 1,
        Energy = 1 << 2,
        Bleeding = 1 << 3,
        Haste = 1 << 4,
        Paralyze = 1 << 5,
        Outfit = 1 << 6,
        Invisible = 1 << 7,
        Light = 1 << 8,
        ManaShield = 1 << 9,
        InFight = 1 << 10,
        Drunk = 1 << 11,
        ExhaustWeapon = 1 << 12, // unused
        Regeneration = 1 << 13,
        Soul = 1 << 14,
        Drown = 1 << 15,
        Muted = 1 << 16,
        ChannelMutedTicks = 1 << 17,
        YellTicks = 1 << 18,
        Attributes = 1 << 19,
        Freezing = 1 << 20,
        Dazzled = 1 << 21,
        Cursed = 1 << 22,
        ExhaustCombat = 1 << 23, // unused
        ExhaustHeal = 1 << 24, // unused
        Pacified = 1 << 25,
        SpellCooldown = 1 << 26,
        SpellGroupCooldown = 1 << 27
    }
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
        SupportsHangable = 1 << 28
    }
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
        DoorId = 1 << 22
    }
    #endregion

    public enum Direction : byte
    {
	    North = 0,
	    East = 1,
	    South = 2,
	    West = 3,

	    DiagonalMask = 4,
	    SouthWest = DiagonalMask | 0,
        SouthEast = DiagonalMask | 1,
        NorthWest = DiagonalMask | 2,
        NorthEast = DiagonalMask | 3,

        Last = NorthEast,
	    SouthAlt = 8,
	    EastAlt = 9,
	    None = 10
    }
    public enum FloorChangeDirection
    {
        None,
        Up,
        Down,
        North,
        South,
        West,
        East,
        EastAlt,
        SouthAlt,
        EastEx = EastAlt,
        SouthEx = SouthAlt,
    }
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
        TrashHolder,
        Container,
        Door,
        MagicField,
        Teleport,
        Bed,
        Key,
        Rune,
        Last
    }
    public enum ItemAttribute : byte
    {
        ServerId = 0x10,
        ClientId,
        Name,
        Description,
        Speed,
        Slot,
        MaxItems,
        Weight,
        Weapon,
        Ammunition,
        Armor,
        MagicLevel,
        MagicFieldType,
        Writeable,
        RotateTo,
        Decay,
        SpriteHash,
        MiniMapColor,
        Attr07,
        Attr08,
        Light,

        //1-byte aligned
        Decay2, //deprecated
        Weapon2, //deprecated
        Ammunition2, //deprecated
        Armor2, //deprecated
        Writeable2, //deprecated
        Light2,
        TopOrder,
        Writeable3, //deprecated

        WareId
    }

    public enum FluidColors : byte {
	    Empty = 0x00,
	    Blue = 0x01,
	    Red = 0x02,
	    Brown = 0x03,
	    Green = 0x04,
	    Yellow = 0x05,
	    White = 0x06,
	    Purple = 0x07
    }
    public enum FluidTypes : byte
    {
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
        FlammingArrow = 34,
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

    public enum Stats : byte
    {
        First = 0,
        MaxHitPoints = First,
        MaxManaPoints = 1,
        SoulPoints = 2, //Unused
        MagicPoints = 3,
        Last = MagicPoints
    }
    public enum Skills : byte
    {
        First = 0,
        Fist = First,
        Club = 1,
        Sword = 2,
        Axe = 3,
        Distance = 4,
        Shield = 5,
        Fishing = 6,
        MagicLevel = 7,
        Level = 8,
        Last = Fishing
    }

    public enum Genders : byte
    {
        First = 0,
        Female = First,
        Male = 1,
        Last = Male
    }

    public enum FixedItems : ushort
    {
        BrowseField = 460, // for internal use

        FireFieldPvpFull = 1487,
        FireFieldPvpMedium = 1488,
        FireFieldPvpSmall = 1489,
        FireFieldPersistentFull = 1492,
        FireFieldPersistentMedium = 1493,
        FireFieldPersistentSmall = 1494,
        FireFieldNoPvp = 1500,

        PoisonFieldPvp = 1490,
        PoisonFieldPersistent = 1496,
        PoisonFieldNoPvp = 1503,

        EnergyFieldPvp = 1491,
        EnergyFieldPersistent = 1495,
        EnergyFieldNoPvp = 1504,

        MagicWall = 1497,
        MagicWallPersistent = 1498,
        MagicWallSafe = 11098,

        WildGrowth = 1499,
        WildGrowthPersistent = 2721,
        WildGrowthSafe = 11099,

        Bag = 1987,

        GoldCoin = 2148,
        PlatinumCoin = 2152,
        CrystalCoin = 2160,

        Depot = 2594,
        Locket1 = 2589,
        Inbox = 14404,
        Market = 14405,

        MaleCorpse = 3058,
        FemaleCorpse = 3065,

        FullSplash = 2016,
        SmallSplash = 2019,

        Parcel = 2595,
        Letter = 2597,
        LetterStamped = 2598,
        Label = 2599,

        AmuletOfLoss = 2173,

        DocumentReadOnly = 1968
    }

    public enum AttributeTypes : byte
    {
        Description = 1,
        ExtFile = 2,
        TileFlags = 3,
        ActionId = 4,
        UniqueId = 5,
        Text = 6,
        Desc = 7,
        TeleDest = 8,
        Item = 9,
        DepotId = 10,
        ExtSpawnFile = 11,
        RuneCharges = 12,
        ExtHouseFile = 13,
        HouseDoorId = 14,
        Count = 15,
        Duration = 16,
        DecayingState = 17,
        WrittenDate = 18,
        WrittenBy = 19,
        SleeperGuid = 20,
        SleepStart = 21,
        Charges = 22,
        ContainerItems = 23,
        Name = 24,
        Article = 25,
        PluralName = 26,
        Weight = 27,
        Attack = 28,
        Defense = 29,
        ExtraDefense = 30,
        Armor = 31,
        HitChance = 32,
        ShootRange = 33
    }

}
