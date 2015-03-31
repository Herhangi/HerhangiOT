using System;
using System.Collections.Generic;

namespace HerhangiOT.GameServerLibrary.Model
{
    public abstract class Vocation
    {
        public static Dictionary<ushort, Vocation> Vocations = new Dictionary<ushort, Vocation>();

        public Dictionary<uint, ulong> ManaReqCache { get; private set; }
        public Dictionary<uint, ulong>[] SkillReqCache { get; private set; }

        //Property Initializers is set to be released with C# 6.0
        public ushort Id { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }

        public float ManaMultiplier { get; protected set; }
        public float[] SkillMultipliers { get; protected set; }//[SKILL_LAST + 1];

        public uint GainHealthTicks { get; protected set; }
        public uint GainHealthAmount { get; protected set; }
        public uint GainManaTicks { get; protected set; }
        public uint GainManaAmount { get; protected set; }
        public uint GainCap { get; protected set; }
        public uint GainMana { get; protected set; }
        public uint GainHp { get; protected set; }
        public uint FromVocation { get; protected set; }
        public uint AttackSpeed = 2000;// { get; protected set; } = 2000;
        public uint BaseSpeed = 220;// { get; protected set; } = 220;

        public ushort GainSoulTicks { get; protected set; }
        public ushort SoulMax { get; protected set; }

        public byte ClientId { get; protected set; }

        public static uint[] SkillBase = {50, 50, 50, 50, 30, 100, 20};

        public void AddToList()
        {
            Vocations.Add(Id, this);

            ManaReqCache = new Dictionary<uint, ulong>();
            SkillReqCache = new Dictionary<uint, ulong>[6];
            for (int i = 0; i < 6; i++) SkillReqCache[i] = new Dictionary<uint, ulong>();
        }

        #region Magic Level Mana Requirement
        public ulong GetManaReq(uint magicLevel)
        {
            if (ManaReqCache.ContainsKey(magicLevel))
                return ManaReqCache[magicLevel];

            ulong manaRequirement = CalculateManaReq(magicLevel);
            ManaReqCache[magicLevel] = manaRequirement;

            return manaRequirement;
        }

        protected virtual ulong CalculateManaReq(uint magicLevel)
        {
	        ulong reqMana = (ulong)(400 * Math.Pow(ManaMultiplier, magicLevel - 1));
	        ulong modResult = reqMana % 20;

	        if (modResult < 10)
		        reqMana -= modResult;
	        else
		        reqMana -= modResult + 20;

            return reqMana;
        }
        #endregion

        #region Skill Requirement
        public ulong GetSkillReq(Skills skill, uint level)
        {
            if (skill > Skills.Last)
                return 0;

            if (SkillReqCache[(byte) skill].ContainsKey(level))
                return SkillReqCache[(byte) skill][level];

            ulong tries = CalculateSkillTries(skill, level);
            SkillReqCache[(byte) skill][level] = tries;

            return tries;
        }

        protected virtual ulong CalculateSkillTries(Skills skill, uint level)
        {
	        return (ulong)(SkillBase[(byte)skill] * Math.Pow(SkillMultipliers[(byte)skill], level - 11));
        }
        #endregion
    }
}
