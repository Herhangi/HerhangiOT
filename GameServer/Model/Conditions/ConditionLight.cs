using System;
using System.IO;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionLight : Condition
    {
        public ConditionLight(ConditionIds id, ConditionFlags type, int ticks, bool buff, uint subId, byte lightLevel,
            byte lightColor)
            : base(id, type, ticks, buff, subId)
        {
            LightInfo = new LightInfo
            {
                Level = lightLevel,
                Color = lightColor
            };
            InternalLightTicks = 0;
            LightChangeInterval = 0;
        }

        private LightInfo LightInfo { get; set; }
        private uint InternalLightTicks { get; set; }
        private uint LightChangeInterval { get; set; }

        public override sealed bool StartCondition(Creature creature)
        {
            if (!base.StartCondition(creature))
                return false;

            InternalLightTicks = 0;
            LightChangeInterval = (uint) (Ticks/LightInfo.Level);
            creature.InternalLight = LightInfo;
            Game.ChangeLight(creature);
            return true;
        }

        public override sealed bool ExecuteCondition(Creature creature, int interval)
        {
            InternalLightTicks = (uint) (InternalLightTicks + interval);

            if (InternalLightTicks >= LightChangeInterval)
            {
                InternalLightTicks = 0;
                LightInfo creatureLight = creature.GetCreatureLight();

                if (creatureLight.Level > 0)
                {
                    --creatureLight.Level;
                    //creature.InternalLight = creatureLight; //TODO: Check this out
                    Game.ChangeLight(creature);
                }
            }

            return base.ExecuteCondition(creature, interval);
        }

        public override sealed void EndCondition(Creature creature)
        {
            creature.SetNormalCreatureLight();
            Game.ChangeLight(creature);
        }

        public override sealed void AddCondition(Creature creature, Condition addCondition)
        {
            if (UpdateCondition(addCondition))
            {
                SetTicks(addCondition.Ticks);

                var conditionLight = (ConditionLight) addCondition;
                LightInfo.Level = conditionLight.LightInfo.Level;
                LightInfo.Color = conditionLight.LightInfo.Color;
                LightChangeInterval = (uint) (Ticks/LightInfo.Level);
                InternalLightTicks = 0;
                creature.InternalLight = LightInfo;
                Game.ChangeLight(creature);
            }
        }

        public override sealed Condition Clone()
        {
            return (ConditionLight) MemberwiseClone();
        }

        public override sealed bool SetParam(ConditionParameters param, int value)
        {
            bool ret = base.SetParam(param, value);
            if (ret)
                return false;

            switch (param)
            {
                case ConditionParameters.LightLevel:
                    LightInfo.Level = (byte) value;
                    return true;

                case ConditionParameters.LightColor:
                    LightInfo.Color = (byte) value;
                    return true;

                default:
                    return false;
            }
        }

        public override sealed void Serialize(MemoryStream propWriteStream)
        {
            base.Serialize(propWriteStream);

            // TODO: color and level could be serialized as 8-bit if we can retain backwards
            // compatibility, but perhaps we should keep it like this in case they increase
            // in the future...
            propWriteStream.WriteByte((byte) ConditionAttributions.LightColor);
            propWriteStream.WriteUInt32(LightInfo.Color);

            propWriteStream.WriteByte((byte) ConditionAttributions.LightLevel);
            propWriteStream.WriteUInt32(LightInfo.Level);

            propWriteStream.WriteByte((byte) ConditionAttributions.LightTicks);
            propWriteStream.WriteUInt32(InternalLightTicks);

            propWriteStream.WriteByte((byte) ConditionAttributions.LightInterval);
            propWriteStream.WriteUInt32(LightChangeInterval);
        }

        public override sealed bool UnserializeProp(ConditionAttributions attr, MemoryStream propStream)
        {
            try
            {
                if (attr == ConditionAttributions.LightColor)
                {
                    LightInfo.Color = (byte) propStream.ReadUInt32();
                    return true;
                }
                if (attr == ConditionAttributions.LightLevel)
                {
                    LightInfo.Level = (byte) propStream.ReadUInt32();
                    return true;
                }
                if (attr == ConditionAttributions.LightTicks)
                {
                    InternalLightTicks = propStream.ReadUInt32();
                    return true;
                }
                if (attr == ConditionAttributions.LightInterval)
                {
                    LightChangeInterval = propStream.ReadUInt32();
                    return true;
                }
                return base.UnserializeProp(attr, propStream);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}