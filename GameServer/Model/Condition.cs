using System;
using System.IO;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model.Conditions;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model
{
    public abstract class Condition
    {
        protected Condition()
        {
        }

        protected Condition(ConditionIds id, ConditionFlags type, int ticks, bool buff = false, uint subId = 0)
        {
            SubId = subId;
            Ticks = ticks;
            ConditionType = type;
            Id = id;
            IsBuff = buff;

            EndTime = Ticks == -1 ? long.MaxValue : 0;
        }

        public long EndTime { get; protected set; }
        public uint SubId { get; protected set; }
        public int Ticks { get; protected set; }
        public ConditionFlags ConditionType { get; protected set; }
        public ConditionIds Id { get; protected set; }
        public bool IsBuff { get; protected set; }

        protected virtual bool UpdateCondition(Condition addCondition)
        {
            if (ConditionType != addCondition.ConditionType)
                return false;

            if (Ticks == -1 && addCondition.Ticks > 0)
                return false;

            if (addCondition.Ticks >= 0 && EndTime > (Tools.GetSystemMilliseconds() + addCondition.Ticks))
                return false;

            return true;
        }

        public virtual bool StartCondition(Creature creature)
        {
            if (Ticks > 0)
            {
                EndTime = Ticks + Tools.GetSystemMilliseconds();
            }
            return true;
        }

        public virtual bool ExecuteCondition(Creature creature, int interval)
        {
            if (Ticks == -1)
                return true;

            //Not using set ticks here since it would reset endTime
            Ticks = Math.Max(0, Ticks - interval);
            return EndTime >= Tools.GetSystemMilliseconds();
        }

        public abstract void EndCondition(Creature creature);
        public abstract void AddCondition(Creature creature, Condition addCondition);

        public virtual IconFlags GetIcons()
        {
            return IsBuff ? IconFlags.PartyBuff : IconFlags.None;
        }

        public abstract Condition Clone();

        public void SetTicks(int newTicks)
        {
            Ticks = newTicks;
            EndTime = Ticks + Tools.GetSystemMilliseconds();
        }

        public static Condition CreateCondition(ConditionIds id, ConditionFlags type, int ticks, int param = 0,
            bool buff = false, uint subId = 0)
        {
            switch (type)
            {
                case ConditionFlags.Poison:
                case ConditionFlags.Fire:
                case ConditionFlags.Energy:
                case ConditionFlags.Drown:
                case ConditionFlags.Freezing:
                case ConditionFlags.Dazzled:
                case ConditionFlags.Cursed:
                case ConditionFlags.Bleeding:
                    return new ConditionDamage(id, type, buff, subId);

                case ConditionFlags.Haste:
                case ConditionFlags.Paralyze:
                    return new ConditionSpeed(id, type, ticks, buff, subId, param);

                case ConditionFlags.Invisible:
                    return new ConditionInvisible(id, type, ticks, buff, subId);

                case ConditionFlags.Outfit:
                    return new ConditionOutfit(id, type, ticks, buff, subId);

                case ConditionFlags.Light:
                    return new ConditionLight(id, type, ticks, buff, subId, (byte) (param & 0xFF),
                        (byte) ((param & 0xFF00) >> 8));

                case ConditionFlags.Regeneration:
                    return new ConditionRegeneration(id, type, ticks, buff, subId);

                case ConditionFlags.Soul:
                    return new ConditionSoul(id, type, ticks, buff, subId);

                case ConditionFlags.Attributes:
                    return new ConditionAttributes(id, type, ticks, buff, subId);

                case ConditionFlags.SpellCooldown:
                    return new ConditionSpellCooldown(id, type, ticks, buff, subId);

                case ConditionFlags.SpellGroupCooldown:
                    return new ConditionSpellGroupCooldown(id, type, ticks, buff, subId);

                case ConditionFlags.InFight:
                case ConditionFlags.Drunk:
                case ConditionFlags.ExhaustWeapon:
                case ConditionFlags.ExhaustCombat:
                case ConditionFlags.ExhaustHeal:
                case ConditionFlags.Muted:
                case ConditionFlags.ChannelMutedTicks:
                case ConditionFlags.YellTicks:
                case ConditionFlags.Pacified:
                case ConditionFlags.ManaShield:
                    return new ConditionGeneric(id, type, ticks, buff, subId);

                default:
                    return null;
            }
        }

        public static Condition CreateCondition(MemoryStream propStream)
        {
            int attr;
            if ((attr = propStream.ReadByte()) == -1 || attr != (int) ConditionAttributions.Type)
                return null;
            uint type = propStream.ReadUInt32();

            if ((attr = propStream.ReadByte()) == -1 || attr != (int) ConditionAttributions.ID)
                return null;
            uint id = propStream.ReadUInt32();

            if ((attr = propStream.ReadByte()) == -1 || attr != (int) ConditionAttributions.Ticks)
                return null;
            uint ticks = propStream.ReadUInt32();

            if ((attr = propStream.ReadByte()) == -1 || attr != (int) ConditionAttributions.IsBuff)
                return null;
            bool isBuff = propStream.ReadBoolean();

            if ((attr = propStream.ReadByte()) == -1 || attr != (int) ConditionAttributions.Ticks)
                return null;
            uint subId = propStream.ReadUInt32();

            return CreateCondition((ConditionIds) id, (ConditionFlags) type, (int) ticks, 0, isBuff, subId);
        }

        public virtual bool SetParam(ConditionParameters param, int value)
        {
            switch (param)
            {
                case ConditionParameters.Ticks:
                    Ticks = value;
                    return true;

                case ConditionParameters.BuffSpell:
                    IsBuff = (value != 0);
                    return true;

                case ConditionParameters.SubID:
                    SubId = (uint) value;
                    return true;

                default:
                    return false;
            }
        }

        public bool Unserialize(MemoryStream propStream)
        {
            int attrType;
            while ((attrType = propStream.ReadByte()) != -1 && attrType != (int) ConditionAttributions.End)
            {
                if (!UnserializeProp((ConditionAttributions) (attrType), propStream))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual void Serialize(MemoryStream propWriteStream)
        {
            propWriteStream.WriteByte((byte) ConditionAttributions.Type);
            propWriteStream.WriteUInt32((uint) ConditionType);

            propWriteStream.WriteByte((byte) ConditionAttributions.ID);
            propWriteStream.WriteUInt32((uint) Id);

            propWriteStream.WriteByte((byte) ConditionAttributions.Ticks);
            propWriteStream.WriteUInt32((uint) Ticks);

            propWriteStream.WriteByte((byte) ConditionAttributions.IsBuff);
            propWriteStream.WriteBoolean(IsBuff);

            propWriteStream.WriteByte((byte) ConditionAttributions.SubID);
            propWriteStream.WriteUInt32(SubId);
        }

        public virtual bool UnserializeProp(ConditionAttributions attr, MemoryStream propStream)
        {
            try
            {
                switch (attr)
                {
                    case ConditionAttributions.Type:
                        uint uValue = propStream.ReadUInt32();
                        ConditionType = (ConditionFlags) uValue;
                        return true;

                    case ConditionAttributions.ID:
                        uValue = propStream.ReadUInt32();
                        Id = (ConditionIds) uValue;
                        return true;

                    case ConditionAttributions.Ticks:
                        Ticks = (int) propStream.ReadUInt32();
                        return true;

                    case ConditionAttributions.IsBuff:
                        IsBuff = propStream.ReadBoolean();
                        return true;

                    case ConditionAttributions.SubID:
                        SubId = propStream.ReadUInt32();
                        return true;

                    case ConditionAttributions.End:
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsPersistent()
        {
            if (Ticks == -1)
                return false;

            if (!(Id == ConditionIds.Default || Id == ConditionIds.Combat))
                return false;

            return true;
        }
    }
}