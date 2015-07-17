using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class ItemTemplate
    {
        public ushort Id;
        public ushort ClientId;

        public string Name;
        public string Article;
        public string PluralName;
        public string Description;
        public string RuneSpellName;
        public string VocationString;

        public ItemGroups Group;
        public ItemTypes Type;

        public bool IsStackable;
        public bool IsAnimation;

        public Abilities Abilities { get; private set; }
        //ConditionDamage

        public uint Weight = 0;
        public uint LevelDoor;
        public uint DecayTime;
        public uint WieldInfo;
        public uint MinReqLevel;
        public uint MinReqMagicLevel;
        public uint Charges;

        public int MaxHitChance;
        public ushort DecayTo = 0;
        public ushort Attack = 0;
        public ushort Defense = 0;
        public short ExtraAttack = 0;
        public short ExtraDefense = 0;
        public ushort Armor = 0;
        public ushort RotateTo = 0;
        public int RuneLevel = 0;

        public CombatTypeFlags CombatType;
        public ushort[] TransformToOnUse; //[2]
        public ushort TransformToFree;
        public ushort DestroyTo;
        public ushort MaxTextLength;
        public ushort WriteOnceItemId;
        public ushort TransformEquipTo;
        public ushort TransformDequipTo;
        public ushort MaxItems;
        public SlotPositionFlags SlotPosition;
        public ushort Speed = 0;
        public ushort WareId = 0;

        //MagicEffect
        //Bed
        public Directions BedPartnerDirection;
        public WeaponTypes WeaponType = WeaponTypes.None;
        public AmmoTypes AmmoType = AmmoTypes.None;
        public ProjectileTypes ShootType = ProjectileTypes.None;
        public CorpseTypes CorpseType = CorpseTypes.None;
        public FluidTypes FluidSource = FluidTypes.None;

        public byte AlwaysOnTopOrder;
        public byte LightLevel;
        public byte LightColor;
        public byte ShootRange;
        public sbyte HitChance;
        
        public FloorChangeDirections FloorChange = FloorChangeDirections.None;
        public bool HasHeight;
        public bool WalkStack;
        public bool ForceUse;

        public bool DoesBlockSolid;
        public bool DoesBlockPickupable;
        public bool DoesBlockProjectile;
        public bool DoesBlockPathFinding;
        public bool DoesAllowPickupable;
        public bool ShowCount;
        public bool ShowDuration;
        public bool ShowCharges;
        public bool ShowAttributes;

        public bool IsReplaceable;
        public bool IsPickupable;
        public bool IsRotatable;
        public bool IsUseable;
        public bool IsMoveable;
        public bool IsAlwaysOnTop = false;
        public bool CanReadText = false;
        public bool CanWriteText = false;
        public bool CanLookThrough = false;
        public bool IsVertical = false;
        public bool IsHorizontal = false;
        public bool IsHangable = false;
        public bool AllowDistanceRead = false;
        public bool StopTime;
        
        public string GetPluralName()
        {
            //Returning cached value if calculated before
            if (!string.IsNullOrEmpty(PluralName))
                return PluralName;

            //Creating a cache not to calculate again next time
            PluralName = Name;
            if (ShowCount)
                PluralName += "s";

            return PluralName;
        }

        public Abilities GetAbilitiesWithInitializer()
        {
            return Abilities ?? (Abilities = new Abilities());
        }

        public bool IsSplash { get { return Group == ItemGroups.Splash; } }
        public bool IsFluidContainer { get { return Group == ItemGroups.Fluid; } }
        public bool HasSubType { get { return (IsFluidContainer || IsSplash || IsStackable || Charges != 0); } }
    }
}
