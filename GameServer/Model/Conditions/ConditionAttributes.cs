using System;
using System.IO;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionAttributes : ConditionGeneric
    {
        public ConditionAttributes(ConditionIds id, ConditionFlags type, int ticks, bool buff = false, uint subId = 0)
            : base(id, type, ticks, buff, subId)
        {
            CurrentSkill = 0;
            CurrentStat = 0;

            const byte arrayLength = (byte) Enums.Stats.Last + 1;

            Skills = new int[arrayLength];
            SkillsPercent = new int[arrayLength];
            Stats = new int[arrayLength];
            StatsPercent = new int[arrayLength];
        }

        protected int[] Skills { get; set; }
        protected int[] SkillsPercent { get; set; }
        protected int[] Stats { get; set; }
        protected int[] StatsPercent { get; set; }
        protected int CurrentSkill { get; set; }
        protected int CurrentStat { get; set; }

        protected void UpdatePercentStats(Player player)
        {
            for (var i = (byte) Enums.Stats.First; i <= (byte) Enums.Stats.Last; ++i)
            {
                if (StatsPercent[i] == 0)
                    continue;

                switch ((Stats) i)
                {
                    case Enums.Stats.MaxHitPoints:
                        Stats[i] = (int) (player.HealthMax*((StatsPercent[i] - 100)/100f));
                        break;

                    case Enums.Stats.MaxManaPoints:
                        Stats[i] = (int) (player.ManaMax*((StatsPercent[i] - 100)/100f));
                        break;

                    case Enums.Stats.MagicPoints:
                        Stats[i] = (int) (player.CharacterData.MagicLevel*((StatsPercent[i] - 100)/100f));
                        break;
                }
            }
        }

        protected void UpdateStats(Player player)
        {
            bool needUpdateStats = false;

            for (var i = (byte) Enums.Stats.First; i <= (byte) Enums.Stats.Last; ++i)
            {
                if (Stats[i] != 0)
                {
                    needUpdateStats = true;
                    player.SetVarStats((Stats) i, Stats[i]);
                }
            }

            if (needUpdateStats)
                player.SendStats();
        }

        protected void UpdatePercentSkills(Player player)
        {
            for (var i = (byte) Enums.Skills.First; i <= (byte) Enums.Skills.Last; ++i)
            {
                if (SkillsPercent[i] == 0)
                    continue;

                int currSkill = player.GetSkillLevel((Skills) i);
                Skills[i] = (int) (currSkill*((SkillsPercent[i] - 100)/100f));
            }
        }

        protected void UpdateSkills(Player player)
        {
            bool needUpdateSkills = false;

            for (var i = (byte) Enums.Skills.First; i <= (byte) Enums.Skills.Last; ++i)
            {
                if (Skills[i] != 0)
                {
                    needUpdateSkills = true;
                    player.VarSkillLevels[i] = (ushort) (player.VarSkillLevels[i] + Skills[i]);
                }
            }

            if (needUpdateSkills)
                player.SendSkills();
        }

        public override bool StartCondition(Creature creature)
        {
            if (!base.StartCondition(creature))
                return false;

            var player = creature as Player;
            if (player != null)
            {
                UpdatePercentSkills(player);
                UpdateSkills(player);
                UpdatePercentStats(player);
                UpdateStats(player);
            }

            return true;
        }

        public override void EndCondition(Creature creature)
        {
            var player = creature as Player;

            if (player != null)
            {
                bool needUpdateSkills = false;

                for (var i = (byte) Enums.Skills.First; i <= (byte) Enums.Skills.Last; ++i)
                {
                    if (Skills[i] != 0 || SkillsPercent[i] != 0)
                    {
                        needUpdateSkills = true;
                        player.VarSkillLevels[i] = (ushort) (player.VarSkillLevels[i] - Skills[i]);
                    }
                }

                if (needUpdateSkills)
                {
                    player.SendSkills();
                }

                bool needUpdateStats = false;

                for (var i = (byte) Enums.Stats.First; i <= (byte) Enums.Stats.Last; ++i)
                {
                    if (Stats[i] != 0)
                    {
                        needUpdateStats = true;
                        player.SetVarStats((Stats) i, -Stats[i]);
                    }
                }

                if (needUpdateStats)
                {
                    player.SendStats();
                }
            }
        }

        public override void AddCondition(Creature creature, Condition addCondition)
        {
            if (UpdateCondition(addCondition))
            {
                SetTicks(addCondition.Ticks);

                var conditionAttrs = (ConditionAttributes) addCondition;
                //Remove the old condition
                EndCondition(creature);

                //Apply the new one
                Skills.MemCpy(0, conditionAttrs.Skills, Skills.Length);
                SkillsPercent.MemCpy(0, conditionAttrs.SkillsPercent, SkillsPercent.Length);
                Stats.MemCpy(0, conditionAttrs.Stats, Stats.Length);
                StatsPercent.MemCpy(0, conditionAttrs.StatsPercent, StatsPercent.Length);

                var player = creature as Player;
                if (player != null)
                {
                    UpdatePercentSkills(player);
                    UpdateSkills(player);
                    UpdatePercentStats(player);
                    UpdateStats(player);
                }
            }
        }

        public override bool SetParam(ConditionParameters param, int value)
        {
            bool ret = base.SetParam(param, value);

            switch (param)
            {
                case ConditionParameters.SkillMelee:
                    Skills[(byte) Enums.Skills.Club] = value;
                    Skills[(byte) Enums.Skills.Axe] = value;
                    Skills[(byte) Enums.Skills.Sword] = value;
                    return true;

                case ConditionParameters.SkillMeleePercent:
                    SkillsPercent[(byte) Enums.Skills.Club] = value;
                    SkillsPercent[(byte) Enums.Skills.Axe] = value;
                    SkillsPercent[(byte) Enums.Skills.Sword] = value;
                    return true;

                case ConditionParameters.SkillFist:
                    Skills[(byte) Enums.Skills.Fist] = value;
                    return true;

                case ConditionParameters.SkillFistPercent:
                    SkillsPercent[(byte) Enums.Skills.Fist] = value;
                    return true;

                case ConditionParameters.SkillClub:
                    Skills[(byte) Enums.Skills.Club] = value;
                    return true;

                case ConditionParameters.SkillClubPercent:
                    SkillsPercent[(byte) Enums.Skills.Club] = value;
                    return true;

                case ConditionParameters.SkillSword:
                    Skills[(byte) Enums.Skills.Sword] = value;
                    return true;

                case ConditionParameters.SkillSwordPercent:
                    SkillsPercent[(byte) Enums.Skills.Sword] = value;
                    return true;

                case ConditionParameters.SkillAxe:
                    Skills[(byte) Enums.Skills.Axe] = value;
                    return true;

                case ConditionParameters.SkillAxePercent:
                    SkillsPercent[(byte) Enums.Skills.Axe] = value;
                    return true;

                case ConditionParameters.SkillDistance:
                    Skills[(byte) Enums.Skills.Distance] = value;
                    return true;

                case ConditionParameters.SkillDistancePercent:
                    SkillsPercent[(byte) Enums.Skills.Distance] = value;
                    return true;

                case ConditionParameters.SkillShield:
                    Skills[(byte) Enums.Skills.Shield] = value;
                    return true;

                case ConditionParameters.SkillShieldPercent:
                    SkillsPercent[(byte) Enums.Skills.Shield] = value;
                    return true;

                case ConditionParameters.SkillFishing:
                    Skills[(byte) Enums.Skills.Fishing] = value;
                    return true;

                case ConditionParameters.SkillFishingPercent:
                    SkillsPercent[(byte) Enums.Skills.Fishing] = value;
                    return true;

                case ConditionParameters.StatMaxHitPoints:
                    Stats[(byte) Enums.Stats.MaxHitPoints] = value;
                    return true;

                case ConditionParameters.StatMaxManaPoints:
                    Stats[(byte) Enums.Stats.MaxManaPoints] = value;
                    return true;

                case ConditionParameters.StatMagicPoints:
                    Stats[(byte) Enums.Stats.MagicPoints] = value;
                    return true;

                case ConditionParameters.StatMaxHitPointsPercent:
                    StatsPercent[(byte) Enums.Stats.MaxHitPoints] = Math.Max(0, value);
                    return true;

                case ConditionParameters.StatMaxManaPointsPercent:
                    StatsPercent[(byte) Enums.Stats.MaxManaPoints] = Math.Max(0, value);
                    return true;

                case ConditionParameters.StatMagicPointsPercent:
                    StatsPercent[(byte) Enums.Stats.MagicPoints] = Math.Max(0, value);
                    return true;

                default:
                    return ret;
            }
        }

        public override Condition Clone()
        {
            return (ConditionAttributes) MemberwiseClone();
        }

        public override void Serialize(MemoryStream propWriteStream)
        {
            base.Serialize(propWriteStream);

            for (var i = (byte) Enums.Skills.First; i <= (byte) Enums.Skills.Last; ++i)
            {
                propWriteStream.WriteByte((byte) ConditionAttributions.Skills);
                propWriteStream.WriteInt32(Skills[i]);
            }

            for (var i = (byte) Enums.Stats.First; i <= (byte) Enums.Stats.Last; ++i)
            {
                propWriteStream.WriteByte((byte) ConditionAttributions.Stats);
                propWriteStream.WriteInt32(Stats[i]);
            }
        }

        public override bool UnserializeProp(ConditionAttributions attr, MemoryStream propStream)
        {
            try
            {
                if (attr == ConditionAttributions.Skills)
                {
                    Skills[CurrentSkill++] = propStream.ReadInt32();
                    return true;
                }
                if (attr == ConditionAttributions.Stats)
                {
                    Stats[CurrentStat++] = propStream.ReadInt32();
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