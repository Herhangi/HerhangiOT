using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionRegeneration : ConditionGeneric
    {
        public ConditionRegeneration(ConditionIds id, ConditionFlags type, int ticks, bool buff = false, uint subId = 0)
            : base(id, type, ticks, buff, subId)
        {
            InternalHealthTicks = 0;
            InternalManaTicks = 0;

            HealthTicks = 1000;
            ManaTicks = 1000;

            HealthGain = 0;
            ManaGain = 0;
        }

        protected uint HealthGain { get; set; }
        protected uint HealthTicks { get; set; }
        protected uint InternalHealthTicks { get; set; }
        protected uint InternalManaTicks { get; set; }

        protected uint ManaGain { get; set; }
        protected uint ManaTicks { get; set; }

        public override sealed void AddCondition(Creature creature, Condition addCondition)
        {
            if (UpdateCondition(addCondition))
            {
                SetTicks(addCondition.Ticks);

                var conditionRegen = (ConditionRegeneration) addCondition;

                HealthTicks = conditionRegen.HealthTicks;
                ManaTicks = conditionRegen.ManaTicks;

                HealthGain = conditionRegen.HealthGain;
                ManaGain = conditionRegen.ManaGain;
            }
        }

        public override sealed bool ExecuteCondition(Creature creature, int interval)
        {
            InternalHealthTicks = (uint) (InternalHealthTicks + interval);
            InternalManaTicks = (uint) (InternalManaTicks + interval);

            if (creature.GetZone() != ZoneTypes.Protection)
            {
                if (InternalHealthTicks >= HealthTicks)
                {
                    InternalHealthTicks = 0;

                    int realHealthGain = creature.Health;
                    creature.ChangeHealth((int) HealthGain);
                    realHealthGain = creature.Health - realHealthGain;

                    if (IsBuff && realHealthGain > 0)
                    {
                        var player = creature as Player;

                        if (player != null)
                        {
                            string healString = string.Format("{0} hitpoint(s).", realHealthGain);

                            var message = new TextMessage
                            {
                                Type = MessageTypes.Healed,
                                Text = "You were healed for " + healString,
                                Position = player.GetPosition(),
                                PrimaryValue = (uint) realHealthGain,
                                PrimaryColor = TextColors.MayaBlue
                            };
                            player.SendTextMessage(message);

                            var list = new HashSet<Creature>();
                            Map.GetSpectators(ref list, player.GetPosition(), false, true);
                            list.Remove(player);
                            if (list.Count != 0)
                            {
                                message.Type = MessageTypes.HealedOthers;
                                message.Text = player.CharacterName + " was healed for " + healString;
                                foreach (Player spectator in list.OfType<Player>())
                                {
                                    spectator.SendTextMessage(message);
                                }
                            }
                        }
                    }
                }

                if (InternalManaTicks >= ManaTicks)
                {
                    InternalManaTicks = 0;
                    creature.ChangeMana((int) ManaGain);
                }
            }

            return base.ExecuteCondition(creature, interval);
        }

        public override sealed bool SetParam(ConditionParameters param, int value)
        {
            bool ret = base.SetParam(param, value);

            switch (param)
            {
                case ConditionParameters.HealthGain:
                    HealthGain = (uint) value;
                    return true;

                case ConditionParameters.HealthTicks:
                    HealthTicks = (uint) value;
                    return true;

                case ConditionParameters.ManaGain:
                    ManaGain = (uint) value;
                    return true;

                case ConditionParameters.ManaTicks:
                    ManaTicks = (uint) value;
                    return true;

                default:
                    return ret;
            }
        }

        public override sealed Condition Clone()
        {
            return (ConditionRegeneration) MemberwiseClone();
        }

        public override sealed void Serialize(MemoryStream propWriteStream)
        {
            base.Serialize(propWriteStream);

            propWriteStream.WriteByte((byte) ConditionAttributions.HealthTicks);
            propWriteStream.WriteUInt32(HealthTicks);

            propWriteStream.WriteByte((byte) ConditionAttributions.HealthGain);
            propWriteStream.WriteUInt32(HealthGain);

            propWriteStream.WriteByte((byte) ConditionAttributions.ManaTicks);
            propWriteStream.WriteUInt32(ManaTicks);

            propWriteStream.WriteByte((byte) ConditionAttributions.ManaGain);
            propWriteStream.WriteUInt32(ManaGain);
        }

        public override sealed bool UnserializeProp(ConditionAttributions attr, MemoryStream propStream)
        {
            try
            {
                switch (attr)
                {
                    case ConditionAttributions.HealthTicks:
                        HealthTicks = propStream.ReadUInt32();
                        return true;

                    case ConditionAttributions.HealthGain:
                        HealthGain = propStream.ReadUInt32();
                        return true;

                    case ConditionAttributions.ManaTicks:
                        ManaTicks = propStream.ReadUInt32();
                        return true;

                    case ConditionAttributions.ManaGain:
                        ManaGain = propStream.ReadUInt32();
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