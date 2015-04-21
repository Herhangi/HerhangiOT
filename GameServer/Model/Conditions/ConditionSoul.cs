using System;
using System.IO;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionSoul : ConditionGeneric
    {
        public ConditionSoul(ConditionIds id, ConditionFlags type, int ticks, bool buff = false, uint subId = 0)
            : base(id, type, ticks, buff, subId)
        {
            InternalSoulTicks = 0;
            SoulTicks = 0;
            SoulGain = 0;
        }

        protected uint InternalSoulTicks { get; set; }
        protected uint SoulTicks { get; set; }
        protected uint SoulGain { get; set; }

        public override sealed void AddCondition(Creature creature, Condition addCondition)
        {
            if (UpdateCondition(addCondition))
            {
                SetTicks(addCondition.Ticks);

                var conditionSoul = (ConditionSoul) addCondition;

                SoulTicks = conditionSoul.SoulTicks;
                SoulGain = conditionSoul.SoulGain;
            }
        }

        public override sealed bool ExecuteCondition(Creature creature, int interval)
        {
            InternalSoulTicks = (uint) (InternalSoulTicks + interval);

            var player = creature as Player;
            if (player != null)
            {
                if (player.GetZone() != ZoneTypes.Protection)
                {
                    if (InternalSoulTicks >= SoulTicks)
                    {
                        InternalSoulTicks = 0;
                        player.ChangeSoul((int) SoulGain);
                    }
                }
            }

            return base.ExecuteCondition(creature, interval);
        }

        public override sealed bool SetParam(ConditionParameters param, int value)
        {
            bool ret = base.SetParam(param, value);

            switch (param)
            {
                case ConditionParameters.SoulGain:
                    SoulGain = (uint) value;
                    return true;

                case ConditionParameters.SoulTicks:
                    SoulTicks = (uint) value;
                    return true;

                default:
                    return ret;
            }
        }

        public override sealed Condition Clone()
        {
            return (ConditionSoul) MemberwiseClone();
        }

        public override sealed void Serialize(MemoryStream propWriteStream)
        {
            base.Serialize(propWriteStream);

            propWriteStream.WriteByte((byte) ConditionAttributions.SoulGain);
            propWriteStream.WriteUInt32(SoulGain);

            propWriteStream.WriteByte((byte) ConditionAttributions.SoulTicks);
            propWriteStream.WriteUInt32(SoulTicks);
        }

        public override sealed bool UnserializeProp(ConditionAttributions attr, MemoryStream propStream)
        {
            try
            {
                switch (attr)
                {
                    case ConditionAttributions.SoulGain:
                        SoulGain = propStream.ReadUInt32();
                        return true;

                    case ConditionAttributions.SoulTicks:
                        SoulTicks = propStream.ReadUInt32();
                        return true;

                    default:
                        return base.UnserializeProp(attr, propStream);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}