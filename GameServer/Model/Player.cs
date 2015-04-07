using System;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Database.Model;

namespace HerhangiOT.GameServer.Model
{
    public class Player : Creature
    {
        #region Constants
        public const ushort PlayerMaxSpeed = 1500;
        public const ushort PlayerMinSpeed = 0;
        #endregion

        public GameConnection Connection { get; private set; }
        public int AccountId { get; private set; }
        public int CharacterId { get; private set; }
        public string AccountName { get; private set; }
        public string CharacterName { get; private set; }
        public ushort PremiumDays { get; private set; }
        public AccountTypes AccountType { get; private set; }

        public CharacterModel CharacterData { get; private set; }
        public Vocation VocationData { get; private set; }

        public byte LevelPercent { get; private set; }
        public byte MagicLevelPercent { get; private set; }
        public byte EffectiveMagicLevel { get; private set; }
        public ushort LastStatsTrainingTime { get; private set; }

        public SkullTypes Skull { get; private set; }
        public long SkullTicks { get; private set; }
        public Genders Gender { get; private set; }
        public Position LoginPosition { get; private set; }

        public ushort BaseSpeed { get; private set; }
        public Town Town { get; private set; }
        public byte[] SkillPercents { get; private set; }
        public uint FreeCapacity { get { return uint.MaxValue; } } //TODO: Calculate

        public bool IsPzLocked { get; protected set; }
        protected long LastWalkthroughAttempt { get; set; }
        protected Position LastWalkthroughPosition { get; set; }

        public new ushort Health { get { return CharacterData.Health; } protected set { CharacterData.Health = value; } }
        public new ushort HealthMax { get { return CharacterData.HealthMax; } protected set { CharacterData.HealthMax = value; } }

        public Player(GameConnection connection, string accountName, string characterName)
        {
            Connection = connection;
            AccountName = accountName;
            CharacterName = characterName;
        }

        public bool PreloadPlayer()
        {
            AccountModel account = Database.Instance.GetAccountInformationWithoutPassword(AccountName);

            if (account == null)
                return false;

            CharacterData = Database.Instance.GetCharacterInformation(CharacterName);

            if (CharacterData == null)
                return false;

            //TODO : DELETION CONTROL

            //TODO: GROUPS

            AccountId = CharacterData.AccountId;
            CharacterId = CharacterData.CharacterId;
            AccountType = (AccountTypes)account.AccountType;

            if (ConfigManager.Instance[ConfigBool.FreePremium])
            {
                PremiumDays = ushort.MaxValue;
            }
            else
            {
                if (!account.PremiumUntil.HasValue || account.PremiumUntil < DateTime.Now)
                    PremiumDays = 0;
                else
                {
                    TimeSpan premiumLeft = account.PremiumUntil.Value - DateTime.Now;
                    PremiumDays = (ushort)premiumLeft.TotalDays;
                }
            }
            
            return true;
        }

        public bool LoadPlayer()
        {
            Gender = (Genders)CharacterData.Gender;

            ulong currentLevelExperience = GetExpForLevel(CharacterData.Level);
            ulong nextLevelExperience = GetExpForLevel(CharacterData.Level + 1U);
            if (CharacterData.Experience < currentLevelExperience || CharacterData.Experience > nextLevelExperience)
            {
                CharacterData.Experience = currentLevelExperience;
            }

            if (currentLevelExperience < nextLevelExperience)
                LevelPercent = GetPercentage(CharacterData.Experience - currentLevelExperience, nextLevelExperience - currentLevelExperience);
            else
                LevelPercent = 0;

            //TODO: Conditions

            VocationData = Vocation.Vocations[CharacterData.Vocation];

            ulong nextMagicLevelMana = VocationData.GetManaReq(CharacterData.MagicLevel + 1U);
            if (CharacterData.ManaSpent > nextMagicLevelMana)
            {
                CharacterData.ManaSpent = 0;
            }
            MagicLevelPercent = GetPercentage(CharacterData.ManaSpent, nextMagicLevelMana);

            //TODO: OUTFIT

            if (Game.Instance.WorldType != GameWorldTypes.PvpEnforced)
            {
                if (CharacterData.SkullUntil.HasValue && CharacterData.SkullUntil > DateTime.Now)
                {
                    long skullSeconds = (long)((CharacterData.SkullUntil.Value - DateTime.Now).TotalSeconds);
                    if (skullSeconds > 0)
                    {
                        //ensure that we round up the number of ticks
                        SkullTicks = (skullSeconds + 2) * 1000;

                        SkullTypes skull = (SkullTypes)CharacterData.Skull;
                        if (skull == SkullTypes.Red || skull == SkullTypes.Black)
                        {
                            Skull = skull;
                        }
                    }
                }
            }

            LoginPosition = new Position(CharacterData.PosX, CharacterData.PosY, CharacterData.PosZ);

            Town = Map.Instance.GetTown(CharacterData.TownId);
            if (Town == null)
            {
                Logger.Log(LogLevels.Error, "Character("+CharacterName+") has TownId("+CharacterData.TownId+") which doesn't exist!");
                return false;
            }

            if (LoginPosition.X == 0 && LoginPosition.Y == 0 && LoginPosition.Z == 0)
            {
                LoginPosition = Town.TemplePosition;
            }

            SkillPercents = new byte[(byte)Skills.Last + 1];
            SkillPercents[(byte)Skills.Fist] = GetPercentage(CharacterData.SkillFistTries, VocationData.GetSkillReq(Skills.Fist, CharacterData.SkillFist + 1U));
            SkillPercents[(byte)Skills.Club] = GetPercentage(CharacterData.SkillClubTries, VocationData.GetSkillReq(Skills.Club, CharacterData.SkillClub + 1U));
            SkillPercents[(byte)Skills.Sword] = GetPercentage(CharacterData.SkillSwordTries, VocationData.GetSkillReq(Skills.Sword, CharacterData.SkillSword + 1U));
            SkillPercents[(byte)Skills.Axe] = GetPercentage(CharacterData.SkillAxeTries, VocationData.GetSkillReq(Skills.Axe, CharacterData.SkillAxe + 1U));
            SkillPercents[(byte)Skills.Distance] = GetPercentage(CharacterData.SkillDistanceTries, VocationData.GetSkillReq(Skills.Distance, CharacterData.SkillDistance + 1U));
            SkillPercents[(byte)Skills.Shield] = GetPercentage(CharacterData.SkillShieldingTries, VocationData.GetSkillReq(Skills.Shield, CharacterData.SkillShielding + 1U));
            SkillPercents[(byte)Skills.Fishing] = GetPercentage(CharacterData.SkillFishingTries, VocationData.GetSkillReq(Skills.Fishing, CharacterData.SkillFishing + 1U));

            //TODO: GUILD
            //TODO: Spells

            //TODO: Items
            //TODO: Depot
            //TODO: Inbox

            //TODO: Storage

            UpdateBaseSpeed();
            UpdateInventoryWeight();
            UpdateItemsLight(true);
            return true;
        }

        private void UpdateBaseSpeed()
        {
            //TODO: Group Flags
            //if (!hasFlag(PlayerFlag_SetMaxSpeed))
            //{
                BaseSpeed = (ushort)(VocationData.BaseSpeed + (2U * (CharacterData.Level - 1U)));
            //}
            //else
            //{
            //    BaseSpeed = PlayerMaxSpeed;
            //}
        }
        private void UpdateInventoryWeight()
        {
            //TODO: COMPLETE THIS METHOD
            //if (hasFlag(PlayerFlag_HasInfiniteCapacity))
            //    return;

            //inventoryWeight = 0;
            //for (int i = CONST_SLOT_FIRST; i <= CONST_SLOT_LAST; ++i)
            //{
            //    const Item* item = inventory[i];
            //    if (item)
            //    {
            //        inventoryWeight += item->getWeight();
            //    }
            //}
        }
        private void UpdateItemsLight(bool isInternal =false)
        {
            //TODO: COMPLETE THIS METHOD
            //LightInfo maxLight;
            //LightInfo curLight;

            //for (int32_t i = CONST_SLOT_FIRST; i <= CONST_SLOT_LAST; ++i) {
            //    Item* item = inventory[i];
            //    if (item) {
            //        item->getLight(curLight);

            //        if (curLight.level > maxLight.level) {
            //            maxLight = curLight;
            //        }
            //    }
            //}

            //if (itemsLight.level != maxLight.level || itemsLight.color != maxLight.color) {
            //    itemsLight = maxLight;

            //    if (!isInternal)
            //    {
            //        g_game.changeLight(this);
            //    }
            //}
        }

        private void SendStats()
        {
            if (Connection != null)
            {
                
                LastStatsTrainingTime = (ushort)(CharacterData.OfflineTrainingTime / 60 / 1000); //Miliseconds to minutes
            }
        }

        public bool CanWalkthroughEx(Creature creature)
        {
            //if (group->access) TODO: GROUP ACCESS
            //    return true;

	        Player player = creature as Player;
	        if (player == null)
		        return false;

	        Tile playerTile = player.Parent;
	        return (playerTile != null) && playerTile.Flags.HasFlag(TileFlags.ProtectionZone);
        }
        public bool CanWalkthrough(Creature creature)
        {
            //TODO: Player Group
	        if (creature.IsInGhostMode())// (group->access || creature.IsInGhostMode()) {
		        return true;

	        Player player = creature as Player;
	        if (player == null)
		        return false;

	        Tile playerTile = player.Parent;
	        if (playerTile == null || !playerTile.Flags.HasFlag(TileFlags.ProtectionZone))
		        return false;

	        Item playerTileGround = playerTile.Ground;
	        if (playerTileGround == null || !playerTileGround.HasWalkStack)
		        return false;

            if ((DateTime.Now.Ticks - LastWalkthroughAttempt) > 2000)
            {
                LastWalkthroughAttempt = DateTime.Now.Ticks;
		        return false;
	        }

	        if (creature.Position != LastWalkthroughPosition)
	        {
	            LastWalkthroughPosition = creature.Position;
		        return false;
	        }

            LastWalkthroughPosition = creature.Position;
	        return true;
        }
        public bool CanSeeCreature(Creature creature)
        {
	        if (creature == this)
		        return true;

	        if (creature.IsInGhostMode())// && !group->access) { TODO: GROUP ACCESS
		        return false;

	        if (!(creature is Player) && !CanSeeInvisibility() && creature.IsInvisible())
		        return false;

	        return true;
        }

        public sealed override CreatureTypes GetCreatureType()
        {
            return CreatureTypes.Player;
        }

        #region Static Methods
        public static byte GetPercentage(ulong current, ulong max)
        {
            if (max == 0)
                return 0;

            byte result = (byte)((current*100)/max);
            if (result > 100)
                return 0;

            return result;
        }
		public static ulong GetExpForLevel(uint level)
        {
			level--;
			return ((50UL * level * level * level) - (150UL * level * level) + (400UL * level)) / 3UL;
        }
        #endregion

        #region Send Methods

        public void SendCreatureAppear(Creature creature, Position pos, bool isLogin)
        {
            if (Connection != null)
                Connection.SendAddCreature(creature, pos, creature.Parent.GetStackposOfCreature(this, creature), isLogin);
        }
        #endregion
    }
}
