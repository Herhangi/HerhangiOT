using System;

namespace HerhangiOT.ServerLibrary.Database.Model
{
    public class CharacterModel
    {
        public int AccountId { get; set; }
        public int CharacterId { get; set; }
        public string CharacterName { get; set; }
        public int GroupId { get; set; }
        public ushort Level { get; set; }
        public byte Vocation { get; set; }
        public ushort Health { get; set; }
        public ushort HealthMax { get; set; }
        public ulong Experience { get; set; }
        public int LookBody { get; set; }
        public int LookFeet { get; set; }
        public int LookHead { get; set; }
        public int LookLegs { get; set; }
        public int LookType { get; set; }
        public int LookAddons { get; set; }
        public byte MagicLevel { get; set; }
        public ushort Mana { get; set; }
        public ushort ManaMax { get; set; }
        public ulong ManaSpent { get; set; }
        public byte Soul { get; set; }
        public ushort TownId { get; set; }
        public ushort PosX { get; set; }
        public ushort PosY { get; set; }
        public byte PosZ { get; set; }
        public int Conditions { get; set; }
        public uint Capacity { get; set; }
        public byte Gender { get; set; }
        public DateTime? LastLogin { get; set; }
        public string LastIp { get; set; }
        public bool Save { get; set; }
        public byte Skull { get; set; }
        public DateTime? SkullUntil { get; set; }
        public DateTime? LastLogout { get; set; }
        public byte Blessings { get; set; }
        public int OnlineTime { get; set; }
        public long Deletion { get; set; }
        public ulong Balance { get; set; }
        public ushort Stamina { get; set; }
        public ushort OfflineTrainingTime { get; set; }
        public int OfflineTrainingSkill { get; set; }

        public uint SkillFist { get; set; }
        public ulong SkillFistTries { get; set; }
        public uint SkillClub { get; set; }
        public ulong SkillClubTries { get; set; }
        public uint SkillSword { get; set; }
        public ulong SkillSwordTries { get; set; }
        public uint SkillAxe { get; set; }
        public ulong SkillAxeTries { get; set; }
        public uint SkillDistance { get; set; }
        public ulong SkillDistanceTries { get; set; }
        public uint SkillShielding { get; set; }
        public ulong SkillShieldingTries { get; set; }
        public uint SkillFishing { get; set; }
        public ulong SkillFishingTries { get; set; }
    }
}
