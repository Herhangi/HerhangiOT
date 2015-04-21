using System;
using System.IO;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionOutfit : Condition
    {
        public ConditionOutfit(ConditionIds id, ConditionFlags type, int ticks, bool buff, uint subId = 0)
            : base(id, type, ticks, buff, subId)
        {
            Outfit = new Outfit();
        }

        protected Outfit Outfit { get; set; }

        public override sealed bool StartCondition(Creature creature)
        {
            if (!base.StartCondition(creature))
                return false;

            Game.InternalCreatureChangeOutfit(creature, Outfit);
            return true;
        }

        public override sealed void EndCondition(Creature creature)
        {
            Game.InternalCreatureChangeOutfit(creature, creature.DefaultOutfit);
        }

        public override sealed void AddCondition(Creature creature, Condition addCondition)
        {
            if (UpdateCondition(addCondition))
            {
                SetTicks(addCondition.Ticks);

                var conditionOutfit = (ConditionOutfit) addCondition;
                Outfit = conditionOutfit.Outfit;

                Game.InternalCreatureChangeOutfit(creature, Outfit);
            }
        }

        public override sealed Condition Clone()
        {
            return (ConditionOutfit) MemberwiseClone();
        }

        public override sealed void Serialize(MemoryStream propWriteStream)
        {
            base.Serialize(propWriteStream);

            propWriteStream.WriteByte((byte) ConditionAttributions.Outfit);
            propWriteStream.WriteUInt16(Outfit.LookType);
            propWriteStream.WriteUInt16(Outfit.LookTypeEx);
            propWriteStream.WriteUInt16(Outfit.LookMount);
            propWriteStream.WriteByte(Outfit.LookHead);
            propWriteStream.WriteByte(Outfit.LookBody);
            propWriteStream.WriteByte(Outfit.LookLegs);
            propWriteStream.WriteByte(Outfit.LookFeet);
            propWriteStream.WriteByte(Outfit.LookAddons);
        }

        public override sealed bool UnserializeProp(ConditionAttributions attr, MemoryStream propStream)
        {
            try
            {
                if (attr == ConditionAttributions.Outfit)
                {
                    Outfit = new Outfit
                    {
                        LookType = propStream.ReadUInt16(),
                        LookTypeEx = propStream.ReadUInt16(),
                        LookMount = propStream.ReadUInt16(),
                        LookHead = (byte) propStream.ReadByte(),
                        LookBody = (byte) propStream.ReadByte(),
                        LookLegs = (byte) propStream.ReadByte(),
                        LookFeet = (byte) propStream.ReadByte(),
                        LookAddons = (byte) propStream.ReadByte()
                    };
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