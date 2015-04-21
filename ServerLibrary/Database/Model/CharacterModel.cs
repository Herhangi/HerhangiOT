using System;

namespace HerhangiOT.ServerLibrary.Database.Model
{
    public class CharacterModel
    {
        public int AccountId { get; set; }
        public uint CharacterId { get; set; }
        public string CharacterName { get; set; }
        public ushort GroupId { get; set; }
        public ushort Level { get; set; }
        public byte Vocation { get; set; }
        public ushort Health { get; set; }
        public ushort HealthMax { get; set; }
        public ulong Experience { get; set; }
        public byte LookBody { get; set; }
        public byte LookFeet { get; set; }
        public byte LookHead { get; set; }
        public byte LookLegs { get; set; }
        public byte LookType { get; set; }
        public byte LookAddons { get; set; }
        public byte MagicLevel { get; set; }
        public ushort Mana { get; set; }
        public ushort ManaMax { get; set; }
        public ulong ManaSpent { get; set; }
        public byte Soul { get; set; }
        public ushort TownId { get; set; }
        public ushort PosX { get; set; }
        public ushort PosY { get; set; }
        public byte PosZ { get; set; }
        public byte[] Conditions { get; set; }
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

        public ushort SkillFist { get; set; }
        public ulong SkillFistTries { get; set; }
        public ushort SkillClub { get; set; }
        public ulong SkillClubTries { get; set; }
        public ushort SkillSword { get; set; }
        public ulong SkillSwordTries { get; set; }
        public ushort SkillAxe { get; set; }
        public ulong SkillAxeTries { get; set; }
        public ushort SkillDistance { get; set; }
        public ulong SkillDistanceTries { get; set; }
        public ushort SkillShielding { get; set; }
        public ulong SkillShieldingTries { get; set; }
        public ushort SkillFishing { get; set; }
        public ulong SkillFishingTries { get; set; }
    }
}
