﻿using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class Abilities
    {
        public CombatTypeFlags ElementType;
        public ushort ElementDamage;

        public int[] SkillModifiers { get; set; }

        public short[] AbsorbPercent { get; set; }
        public short[] FieldAbsorbPercent { get; set; }

        public int[] StatModifiers { get; set; }
        public int[] StatModifiersPercent { get; set; }

        public int Speed;
        public bool ManaShield;
        public bool Invisible;
        public bool Regeneration;

        public uint HealthGain;
        public uint HealthTicks;
        public uint ManaGain;
        public uint ManaTicks;

        public uint ConditionImmunities;
        public ConditionFlags ConditionSupressions;

        #region Late Initializers For Arrays
        public void InitializeSkillModifiers()
        {
            if (SkillModifiers == null)
                SkillModifiers = new int[(byte)Skills.Last + 1];
        }

        public void InitializeAbsorbPercent()
        {
            if (AbsorbPercent == null)
                AbsorbPercent = new short[(ushort)CombatTypeFlags.Count + 1];
        }

        public void InitializeFieldAbsorbPercent()
        {
            if (FieldAbsorbPercent == null)
                FieldAbsorbPercent = new short[(ushort)CombatTypeFlags.Count + 1];
        }

        public void InitializeStatModifiers()
        {
            if (StatModifiers == null)
                StatModifiers = new int[(byte)Stats.Last + 1];
        }

        public void InitializeStatModifiersPercent()
        {
            if (StatModifiersPercent == null)
                StatModifiersPercent = new int[(byte)Stats.Last + 1];
        }
        #endregion
    }
}
