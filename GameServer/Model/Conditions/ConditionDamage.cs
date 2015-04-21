using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionDamage : Condition
    {
        public ConditionDamage()
        {
        }

        public ConditionDamage(ConditionIds id, ConditionFlags type, bool buff = false, uint subId = 0)
            : base(id, type, 0, buff, subId)
        {
            Delayed = false;
            ForceUpdate = false;
            Field = false;
            Owner = 0;
            MinDamage = 0;
            MaxDamage = 0;
            StartDamage = 0;
            PeriodDamage = 0;
            PeriodDamageTick = 0;
            TickInterval = 2000;
        }

        protected int MaxDamage { get; set; }
        protected int MinDamage { get; set; }
        protected int StartDamage { get; set; }
        protected int PeriodDamage { get; set; }
        protected int PeriodDamageTick { get; set; }
        protected int TickInterval { get; set; }

        protected bool ForceUpdate { get; set; }
        protected bool Delayed { get; set; }
        protected bool Field { get; set; }
        protected uint Owner { get; set; }

        protected List<IntervalInfo> DamageList { get; set; }

        protected bool Init()
        {
            if (PeriodDamage != 0)
                return true;

            if (DamageList.Count == 0)
            {
                SetTicks(0);

                int amount = Game.RNG.Next(MinDamage, MaxDamage);
                if (amount != 0)
                {
                    if (StartDamage > MaxDamage)
                        StartDamage = MaxDamage;
                    else if (StartDamage == 0)
                        StartDamage = Math.Max(1, (int) Math.Ceiling(amount/20f));

                    var list = new List<int>();
                    GenerateDamageList(amount, StartDamage, ref list);
                    foreach (int value in list)
                    {
                        AddDamage(1, TickInterval, -value);
                    }
                }
            }
            return DamageList.Count != 0;
        }

        public static void GenerateDamageList(int amount, int start, ref List<int> list)
        {
            amount = Math.Abs(amount);
            int sum = 0;

            for (int i = start; i > 0; --i)
            {
                int n = start + 1 - i;
                int med = (n*amount)/start;

                double x1, x2;
                do
                {
                    sum += i;
                    list.Add(i);

                    x1 = Math.Abs(1.0 - (((float) (sum)) + i)/med);
                    x2 = Math.Abs(1.0 - ((float) (sum)/med));
                } while (x1 < x2);
            }
        }

        public override sealed bool StartCondition(Creature creature)
        {
            if (!base.StartCondition(creature))
                return false;

            if (!Init())
                return false;

            if (!Delayed)
            {
                int damage;
                if (GetNextDamage(out damage))
                {
                    return DoDamage(creature, damage);
                }
            }
            return true;
        }

        public override sealed bool ExecuteCondition(Creature creature, int interval)
        {
            if (PeriodDamage != 0)
            {
                PeriodDamageTick += interval;

                if (PeriodDamageTick >= TickInterval)
                {
                    PeriodDamageTick = 0;
                    DoDamage(creature, PeriodDamage);
                }
            }
            else if (DamageList.Count != 0)
            {
                IntervalInfo damageInfo = DamageList[0];

                bool bRemove = (Ticks != -1);
                creature.OnTickCondition(ConditionType, ref bRemove);
                damageInfo.TimeLeft -= interval;

                if (damageInfo.TimeLeft <= 0)
                {
                    int damage = damageInfo.Value;

                    if (bRemove)
                    {
                        DamageList.RemoveAt(0);
                    }
                    else
                    {
                        damageInfo.TimeLeft = damageInfo.Interval;
                    }

                    DoDamage(creature, damage);
                }

                if (!bRemove)
                {
                    if (Ticks > 0)
                    {
                        EndTime += interval;
                    }

                    interval = 0;
                }
            }

            return base.ExecuteCondition(creature, interval);
        }

        public override sealed void EndCondition(Creature creature)
        {
        }

        public override sealed void AddCondition(Creature creature, Condition addCondition)
        {
            if (addCondition.ConditionType != ConditionType)
                return;

            if (!UpdateCondition(addCondition))
                return;

            var conditionDamage = (ConditionDamage) addCondition;

            SetTicks(addCondition.Ticks);
            Owner = conditionDamage.Owner;
            MaxDamage = conditionDamage.MaxDamage;
            MinDamage = conditionDamage.MinDamage;
            StartDamage = conditionDamage.StartDamage;
            TickInterval = conditionDamage.TickInterval;
            PeriodDamage = conditionDamage.PeriodDamage;
            int nextTimeLeft = TickInterval;

            if (DamageList.Count != 0)
            {
                //save previous timeLeft
                IntervalInfo damageInfo = DamageList[0];
                nextTimeLeft = damageInfo.TimeLeft;
                DamageList.Clear();
            }

            DamageList = conditionDamage.DamageList;

            if (Init())
            {
                if (DamageList.Count != 0)
                {
                    //restore last timeLeft
                    DamageList[0].TimeLeft = nextTimeLeft;
                }

                if (!Delayed)
                {
                    int damage;
                    if (GetNextDamage(out damage))
                        DoDamage(creature, damage);
                }
            }
        }

        public override sealed Condition Clone()
        {
            return (ConditionDamage) MemberwiseClone();
        }

        public override sealed IconFlags GetIcons()
        {
            IconFlags icons = base.GetIcons();

            switch (ConditionType)
            {
                case ConditionFlags.Fire:
                    icons |= IconFlags.Burn;
                    break;

                case ConditionFlags.Energy:
                    icons |= IconFlags.Energy;
                    break;

                case ConditionFlags.Drown:
                    icons |= IconFlags.Drowning;
                    break;

                case ConditionFlags.Poison:
                    icons |= IconFlags.Poison;
                    break;

                case ConditionFlags.Freezing:
                    icons |= IconFlags.Freezing;
                    break;

                case ConditionFlags.Dazzled:
                    icons |= IconFlags.Dazzled;
                    break;

                case ConditionFlags.Cursed:
                    icons |= IconFlags.Cursed;
                    break;

                case ConditionFlags.Bleeding:
                    icons |= IconFlags.Bleeding;
                    break;
            }
            return icons;
        }

        public override sealed bool SetParam(ConditionParameters param, int value)
        {
            bool ret = base.SetParam(param, value);

            switch (param)
            {
                case ConditionParameters.Owner:
                    Owner = (uint) value;
                    return true;

                case ConditionParameters.ForceUpdate:
                    ForceUpdate = (value != 0);
                    return true;

                case ConditionParameters.Delayed:
                    Delayed = (value != 0);
                    return true;

                case ConditionParameters.MaxValue:
                    MaxDamage = Math.Abs(value);
                    break;

                case ConditionParameters.MinValue:
                    MinDamage = Math.Abs(value);
                    break;

                case ConditionParameters.StartValue:
                    StartDamage = Math.Abs(value);
                    break;

                case ConditionParameters.TickInterval:
                    TickInterval = Math.Abs(value);
                    break;

                case ConditionParameters.PeriodicDamage:
                    PeriodDamage = value;
                    break;

                case ConditionParameters.Field:
                    Field = (value != 0);
                    break;

                default:
                    return false;
            }

            return ret;
        }

        public bool AddDamage(int rounds, int time, int value)
        {
            time = Math.Max(time, Constants.JobCheckCreatureCompletionInterval);

            if (rounds == -1)
            {
                //periodic damage
                PeriodDamage = value;
                SetParam(ConditionParameters.TickInterval, time);
                SetParam(ConditionParameters.Ticks, -1);
                return true;
            }

            if (PeriodDamage > 0)
                return false;

            //rounds, time, damage
            for (int i = 0; i < rounds; ++i)
            {
                var damageInfo = new IntervalInfo {Interval = time, TimeLeft = time, Value = value};
                DamageList.Add(damageInfo);

                if (Ticks != -1)
                    SetTicks(Ticks + damageInfo.Interval);
            }

            return true;
        }

        public int GetTotalDamage()
        {
            int result;
            if (DamageList.Count != 0)
                result = DamageList.Sum(intervalInfo => intervalInfo.Value);
            else
                result = MinDamage + (MaxDamage - MinDamage)/2;

            return Math.Abs(result);
        }

        public override sealed void Serialize(MemoryStream propWriteStream)
        {
            base.Serialize(propWriteStream);

            propWriteStream.WriteByte((byte) ConditionAttributions.Delayed);
            propWriteStream.WriteBoolean(Delayed);

            propWriteStream.WriteByte((byte) ConditionAttributions.PeriodDamage);
            propWriteStream.WriteInt32(PeriodDamage);

            foreach (IntervalInfo intervalInfo in DamageList)
            {
                propWriteStream.WriteByte((byte) ConditionAttributions.IntervalData);
                propWriteStream.WriteInt32(intervalInfo.TimeLeft);
                propWriteStream.WriteInt32(intervalInfo.Value);
                propWriteStream.WriteInt32(intervalInfo.Interval);
            }
        }

        public override sealed bool UnserializeProp(ConditionAttributions attr, MemoryStream propStream)
        {
            try
            {
                if (attr == ConditionAttributions.Delayed)
                {
                    Delayed = propStream.ReadBoolean();
                    return true;
                }
                if (attr == ConditionAttributions.PeriodDamage)
                {
                    PeriodDamage = propStream.ReadInt32();
                    return true;
                }
                if (attr == ConditionAttributions.Owner)
                {
                    propStream.ReadBytes(4);
                    return true;
                }
                if (attr == ConditionAttributions.IntervalData)
                {
                    var damageInfo = new IntervalInfo
                    {
                        TimeLeft = propStream.ReadInt32(),
                        Value = propStream.ReadInt32(),
                        Interval = propStream.ReadInt32(),
                    };
                    DamageList.Add(damageInfo);

                    if (Ticks != -1)
                    {
                        SetTicks(Ticks + damageInfo.Interval);
                    }
                    return true;
                }
                return base.UnserializeProp(attr, propStream);
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected bool GetNextDamage(out int damage)
        {
            damage = 0;
            if (PeriodDamage != 0)
            {
                damage = PeriodDamage;
                return true;
            }
            if (DamageList.Count != 0)
            {
                IntervalInfo damageInfo = DamageList[0];
                damage = damageInfo.Value;
                if (Ticks != -1)
                {
                    DamageList.RemoveAt(0);
                }
                return true;
            }
            return false;
        }

        protected bool DoDamage(Creature creature, int healthChange)
        {
            if (creature.IsSuppressed(ConditionType))
                return true;

            var damage = new CombatDamage
            {
                Origin = CombatOrigins.Condition,
                PrimaryValue = healthChange,
                PrimaryType = Combat.ConditionToDamageType(ConditionType)
            };

            Creature attacker = Game.GetCreatureById(Owner);
            if (!creature.IsAttackable() || Combat.CanDoCombat(attacker, creature) != ReturnTypes.NoError)
            {
                if (!creature.IsInGhostMode())
                {
                    Game.AddMagicEffect(creature.GetPosition(), MagicEffects.Poff);
                }
                return false;
            }

            if (Game.CombatBlockHit(damage, attacker, creature, false, false, Field))
                return false;
            return Game.CombatChangeHealth(attacker, creature, damage);
        }

        protected override sealed bool UpdateCondition(Condition addCondition)
        {
            var conditionDamage = (ConditionDamage) addCondition;
            if (conditionDamage.ForceUpdate)
                return true;

            if (Ticks == -1 && conditionDamage.Ticks > 0)
                return false;

            return conditionDamage.GetTotalDamage() > GetTotalDamage();
        }
    }
}