using System;
using System.IO;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionSpeed : Condition
    {
        public ConditionSpeed(ConditionIds id, ConditionFlags type, int ticks, bool buff, uint subId, int changeSpeed)
            : base(id, type, ticks, buff, subId)
        {
            SpeedDelta = changeSpeed;
            MinA = 0.0f;
            MinB = 0.0f;
            MaxA = 0.0f;
            MaxB = 0.0f;
        }

        protected int SpeedDelta { get; set; }

        //formula variables
        protected float MinA { get; set; }
        protected float MinB { get; set; }
        protected float MaxA { get; set; }
        protected float MaxB { get; set; }

        protected void GetFormulaValues(int var, out int min, out int max)
        {
            min = (int) ((var*MinA) + MinB);
            max = (int) ((var*MaxA) + MaxB);
        }

        public override sealed bool StartCondition(Creature creature)
        {
            if (!base.StartCondition(creature))
                return false;

            if (SpeedDelta == 0)
            {
                int min, max;
                GetFormulaValues(creature.BaseSpeed, out min, out max);
                SpeedDelta = Game.RNG.Next(min, max);
            }

            Game.ChangeSpeed(creature, SpeedDelta);
            return true;
        }

        public override sealed void EndCondition(Creature creature)
        {
            Game.ChangeSpeed(creature, -SpeedDelta);
        }

        public override sealed void AddCondition(Creature creature, Condition addCondition)
        {
            if (ConditionType != addCondition.ConditionType)
                return;

            if (Ticks == -1 && addCondition.Ticks > 0)
                return;

            SetTicks(addCondition.Ticks);

            var conditionSpeed = (ConditionSpeed) addCondition;
            int oldSpeedDelta = SpeedDelta;
            SpeedDelta = conditionSpeed.SpeedDelta;
            MinA = conditionSpeed.MinA;
            MaxA = conditionSpeed.MaxA;
            MinB = conditionSpeed.MinB;
            MaxB = conditionSpeed.MaxB;

            if (SpeedDelta == 0)
            {
                int min, max;
                GetFormulaValues(creature.BaseSpeed, out min, out max);
                SpeedDelta = Game.RNG.Next(min, max);
            }

            int newSpeedChange = (SpeedDelta - oldSpeedDelta);
            if (newSpeedChange != 0)
            {
                Game.ChangeSpeed(creature, newSpeedChange);
            }
        }

        public override sealed IconFlags GetIcons()
        {
            IconFlags icons = base.GetIcons();

            switch (ConditionType)
            {
                case ConditionFlags.Haste:
                    icons |= IconFlags.Haste;
                    break;

                case ConditionFlags.Paralyze:
                    icons |= IconFlags.Paralyze;
                    break;
            }
            return icons;
        }

        public override sealed Condition Clone()
        {
            return (ConditionSpeed) MemberwiseClone();
        }

        public override sealed bool SetParam(ConditionParameters param, int value)
        {
            base.SetParam(param, value);
            if (param != ConditionParameters.Speed)
                return false;

            SpeedDelta = value;
            ConditionType = value > 0 ? ConditionFlags.Haste : ConditionFlags.Paralyze;
            return true;
        }

        public void SetFormulaVars(float minA, float minB, float maxA, float maxB)
        {
            MinA = minA;
            MinB = minB;
            MaxA = maxA;
            MaxB = maxB;
        }

        public override void Serialize(MemoryStream propWriteStream)
        {
            base.Serialize(propWriteStream);

            propWriteStream.WriteByte((byte) ConditionAttributions.SpeedDelta);
            propWriteStream.WriteInt32(SpeedDelta);

            propWriteStream.WriteByte((byte) ConditionAttributions.FormulaMinA);
            propWriteStream.WriteFloat(MinA);

            propWriteStream.WriteByte((byte) ConditionAttributions.FormulaMinB);
            propWriteStream.WriteFloat(MinB);

            propWriteStream.WriteByte((byte) ConditionAttributions.FormulaMaxA);
            propWriteStream.WriteFloat(MaxA);

            propWriteStream.WriteByte((byte) ConditionAttributions.FormulaMaxB);
            propWriteStream.WriteFloat(MaxB);
        }

        public override bool UnserializeProp(ConditionAttributions attr, MemoryStream propStream)
        {
            try
            {
                if (attr == ConditionAttributions.SpeedDelta)
                {
                    SpeedDelta = propStream.ReadInt32();
                    return true;
                }
                if (attr == ConditionAttributions.FormulaMinA)
                {
                    MinA = propStream.ReadFloat();
                    return true;
                }
                if (attr == ConditionAttributions.FormulaMinB)
                {
                    MinB = propStream.ReadFloat();
                    return true;
                }
                if (attr == ConditionAttributions.FormulaMaxA)
                {
                    MaxA = propStream.ReadFloat();
                    return true;
                }
                if (attr == ConditionAttributions.FormulaMaxB)
                {
                    MaxB = propStream.ReadFloat();
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