using System;

namespace HerhangiOT.GameServer.Enums
{
    [Flags]
    public enum PlayerFlags : ulong
    {
        CannotUseCombat = 1U << 0,
        CannotAttackPlayer = 1U << 1,
        CannotAttackMonster = 1U << 2,
        CannotBeAttacked = 1U << 3,
        CanConvinceAll = 1U << 4,
        CanSummonAll = 1U << 5,
        CanIllusionAll = 1U << 6,
        CanSenseInvisibility = 1U << 7,
        IgnoredByMonsters = 1U << 8,
        NotGainInFight = 1U << 9,
        HasInfiniteMana = 1U << 10,
        HasInfiniteSoul = 1U << 11,
        HasNoExhaustion = 1U << 12,
        CannotUseSpells = 1U << 13,
        CannotPickupItem = 1U << 14,
        CanAlwaysLogin = 1U << 15,
        CanBroadcast = 1U << 16,
        CanEditHouses = 1U << 17,
        CannotBeBanned = 1U << 18,
        CannotBePushed = 1U << 19,
        HasInfiniteCapacity = 1U << 20,
        CanPushAllCreatures = 1U << 21,
        CanTalkRedPrivate = 1U << 22,
        CanTalkRedChannel = 1U << 23,
        TalkOrangeHelpChannel = 1U << 24,
        NotGainExperience = 1U << 25,
        NotGainMana = 1U << 26,
        NotGainHealth = 1U << 27,
        NotGainSkill = 1U << 28,
        SetMaxSpeed = 1U << 29,
        SpecialVIP = 1U << 30,
        NotGenerateLoot = 1U << 31,
        CanTalkRedChannelAnonymous = 1U << 33,
        IgnoreProtectionZone = 1U << 33,
        IgnoreSpellCheck = 1U << 34,
        IgnoreWeaponCheck = 1U << 35,
        CannotBeMuted = 1U << 36,
        IsAlwaysPremium = 1U << 37,
    }
}
