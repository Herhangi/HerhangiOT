﻿using System;
using System.Collections.Generic;
using System.IO;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model.Items;
using HerhangiOT.GameServer.Scriptability;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Database.Model;
using HerhangiOT.ServerLibrary.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model
{
    public class Player : Creature
    {
        public static uint PlayerAutoID = 0x10000000;

        #region Constants
        public const ushort PlayerMaxSpeed = 1500;
        public const ushort PlayerMinSpeed = 0;
        #endregion

        public Group Group { get; private set; }
        public GameConnection Connection { get; set; }
        public int AccountId { get; private set; }
        public uint CharacterId { get; private set; }
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

        public ConditionFlags ConditionSupressions { get; private set; }
        public ConditionFlags ConditionImmunities { get; private set; }
        public CombatTypeFlags DamageImmunities { get; private set; }

        public long SkullTicks { get; private set; }
        public Genders Gender { get; private set; }
        public Position LoginPosition { get; private set; }
        public override sealed int StepSpeed { get { return Math.Max(PlayerMinSpeed, Math.Min(PlayerMaxSpeed, Speed)); } }

        public Town Town { get; private set; }
        public ushort[] VarStats { get; private set; }
        public ushort[] VarSkillLevels { get; private set; }
        public ushort[] SkillLevels { get; private set; }
        public byte[] SkillPercents { get; private set; }
        public uint FreeCapacity { get { return uint.MaxValue; } } //TODO: Calculate
        public Item[] Inventory { get; private set; }

        public FightModes FightMode { get; set; }
        public ChaseModes ChaseMode { get; set; }
        public SecureModes SecureMode { get; set; }

        public bool WasMounted { get; protected set; }
        public bool IsConnecting { get; set; }
        public bool IsPzLocked { get; protected set; }
        protected long LastWalkthroughAttempt { get; set; }
        protected Position LastWalkthroughPosition { get; set; }

        public int MessageBufferTicks { get; set; }
        public int MessageBufferCount { get; set; }
        public uint IdleTime { get; set; }
        public uint WalkTaskEvent { get; set; }
        public uint NextStepEvent { get; set; }
        public uint ActionTaskEvent { get; set; }
        public SchedulerTask WalkTask { get; protected set; }

        public Item TradeItem { get; protected set; }
        public TradeStates TradeState { get; protected set; }
        public List<ShopInfo> ShopItemList { get; protected set; }

        public int PurchaseCallback { get; protected set; }
        public int SaleCallback { get; protected set; }
        public Npc ShopOwner { get; protected set; }

        public HashSet<uint> ModalWindows { get; protected set; } 

        public Dictionary<byte, OpenContainer> OpenContainers;
        public Dictionary<uint, DepotLocker> DepotLockers; 
        public Dictionary<uint, DepotChest> DepotChests; 
        public Dictionary<uint, int> StorageMap; 

        public override ushort Health { get { return CharacterData.Health; } protected set { CharacterData.Health = value; } }
        public override ushort HealthMax { get { return CharacterData.HealthMax; } protected set { CharacterData.HealthMax = value; } }
        public override ushort Mana { get { return CharacterData.Mana; } protected set { CharacterData.Mana = value; } }
        public override ushort ManaMax { get { return CharacterData.ManaMax; } protected set { CharacterData.ManaMax = value; } }

        private long _nextAction;
        public long NextAction
        {
            get { return _nextAction; }
            protected set
            {
                if(value > _nextAction)
                    _nextAction = value;
            }
        }

        public Player(GameConnection connection, string accountName, string characterName) : base()
        {
            Connection = connection;
            AccountName = accountName;
            CharacterName = characterName;

            Inventory = new Item[(byte)Slots.Last + 1];
            OpenContainers = new Dictionary<byte, OpenContainer>();
            DepotLockers = new Dictionary<uint, DepotLocker>();
            DepotChests = new Dictionary<uint, DepotChest>();
            StorageMap = new Dictionary<uint, int>();

            ModalWindows = new HashSet<uint>();
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

            Group = Groups.GetGroup(CharacterData.GroupId);
            if (Group == null)
            {
                Logger.Log(LogLevels.Information, "Player({0}) has unknown group id({1})!", CharacterData.CharacterName, CharacterData.GroupId);
                return false;
            }

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

            if (CharacterData.Conditions != null)
            {
                MemoryStream conditionStream = new MemoryStream(CharacterData.Conditions);
                Condition condition = Condition.CreateCondition(conditionStream);
                while (condition != null)
                {
                    if (condition.Unserialize(conditionStream))
                    {
                        Conditions.Add(condition);
                    }
                    condition = Condition.CreateCondition(conditionStream);
                }
            }

            VocationData = Vocation.Vocations[CharacterData.Vocation];

            ulong nextMagicLevelMana = VocationData.GetManaReq(CharacterData.MagicLevel + 1U);
            if (CharacterData.ManaSpent > nextMagicLevelMana)
            {
                CharacterData.ManaSpent = 0;
            }
            MagicLevelPercent = GetPercentage(CharacterData.ManaSpent, nextMagicLevelMana);

            DefaultOutfit = new Outfit
            {
                LookType = CharacterData.LookType,
                LookHead = CharacterData.LookHead,
                LookBody = CharacterData.LookBody,
                LookLegs = CharacterData.LookLegs,
                LookFeet = CharacterData.LookFeet,
                LookAddons = CharacterData.LookAddons
            };
            CurrentOutfit = DefaultOutfit;

            if (Game.WorldType != GameWorldTypes.PvpEnforced)
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

            Town = Map.GetTown(CharacterData.TownId);
            if (Town == null)
            {
                Logger.Log(LogLevels.Error, "Character("+CharacterName+") has TownId("+CharacterData.TownId+") which doesn't exist!");
                return false;
            }

            if (LoginPosition.X == 0 && LoginPosition.Y == 0 && LoginPosition.Z == 0)
            {
                LoginPosition = new Position(Town.TemplePosition); //Breaking reference
            }

            VarStats = new ushort[(byte)Stats.Last + 1];
            VarSkillLevels = new ushort[(byte)Skills.Last + 1];
            SkillLevels = new ushort[(byte)Skills.Last + 1];
            SkillLevels[(byte)Skills.Fist] = CharacterData.SkillFist;
            SkillLevels[(byte)Skills.Club] = CharacterData.SkillClub;
            SkillLevels[(byte)Skills.Sword] = CharacterData.SkillSword;
            SkillLevels[(byte)Skills.Axe] = CharacterData.SkillAxe;
            SkillLevels[(byte)Skills.Distance] = CharacterData.SkillDistance;
            SkillLevels[(byte)Skills.Shield] = CharacterData.SkillShielding;
            SkillLevels[(byte)Skills.Fishing] = CharacterData.SkillFishing;

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

        public bool HasFlag(PlayerFlags flag)
        {
            return Group.Flags.HasFlag(flag);
        }

        private void UpdateBaseSpeed()
        {
            if(!HasFlag(PlayerFlags.SetMaxSpeed))
                BaseSpeed = (ushort)(VocationData.BaseSpeed + (2U * (CharacterData.Level - 1U)));
            else
                BaseSpeed = PlayerMaxSpeed;
        }
        private void UpdateInventoryWeight()
        {
            if (HasFlag(PlayerFlags.HasInfiniteCapacity))
                return;

            //TODO: COMPLETE THIS METHOD
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

        public bool CanWalkthroughEx(Creature creature)
        {
            if (Group.Access)
                return true;

	        Player player = creature as Player;
	        if (player == null)
		        return false;

	        Tile playerTile = player.Parent;
	        return (playerTile != null) && playerTile.Flags.HasFlag(TileFlags.ProtectionZone);
        }
        public bool CanWalkthrough(Creature creature)
        {
	        if (creature.IsInGhostMode() || Group.Access)
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

            if ((Tools.GetSystemMilliseconds() - LastWalkthroughAttempt) > 2000)
            {
                LastWalkthroughAttempt = Tools.GetSystemMilliseconds();
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
        public override bool CanSeeCreature(Creature creature)
        {
	        if (creature == this)
		        return true;

	        if (creature.IsInGhostMode() && !Group.Access)
		        return false;

	        if (!(creature is Player) && !CanSeeInvisibility() && creature.IsInvisible())
		        return false;

	        return true;
        }

        #region Creature Overrides
        public override void OnThink(uint interval)
        {
            base.OnThink(interval);

            MessageBufferTicks += (int)interval;
            if (MessageBufferTicks >= 1500)
            {
                MessageBufferTicks = 0;
                AddMessageBuffer();
            }

            //TODO: Continue this method
        }

        public sealed override CreatureTypes GetCreatureType()
        {
            return CreatureTypes.Player;
        }

        public override string GetName()
        {
            return CharacterName;
        }
        public override string GetNameDescription()
        {
            return CharacterName;
        }

        public override void AddList()
        {
            //TODO: VIP LIST
            Game.AddPlayer(this);
        }

        public override void RemoveList()
        {
            Game.RemovePlayer(this);

            //TODO: VIP LIST
        }

        public sealed override void OnWalk(ref Directions dir)
        {
            base.OnWalk(ref dir);
	        SetNextActionTask(null);
            NextAction = Tools.GetSystemMilliseconds() + GetStepDuration(dir);
        }

        public sealed override void OnWalkAborted()
        {
	        SetNextWalkActionTask(null);
	        SendCancelWalk();
        }
        public sealed override void OnWalkComplete()
        {
	        if (WalkTask != null)
	        {
	            WalkTaskEvent = DispatcherManager.Scheduler.AddEvent(WalkTask);
		        WalkTask = null;
	        }
        }

        public sealed override bool IsAttackable()
        {
            return !HasFlag(PlayerFlags.CannotBeAttacked);
        }
        public sealed override bool IsImmune(CombatTypeFlags damageType)
        {
            if (HasFlag(PlayerFlags.CannotBeAttacked))
                return true;
            return base.IsImmune(damageType);
        }
        public sealed override bool IsImmune(ConditionFlags condition)
        {
            if (HasFlag(PlayerFlags.CannotBeAttacked))
                return true;
            return base.IsImmune(condition);
        }
        public sealed override ConditionFlags GetConditionImmunities()
        {
            return ConditionImmunities;
        }
        public sealed override ConditionFlags GetConditionSuppressions()
        {
            return ConditionSupressions;
        }
        #endregion

        public void AddConditionSuppressions(ConditionFlags conditions)
        {
            ConditionSupressions |= conditions;
        }
        public void RemoveConditionSuppressions(ConditionFlags conditions)
        {
            ConditionSupressions &= ~conditions;
        }

        public void OnSendContainer(Container container)
        {
	        if (Connection == null)
		        return;

            bool hasParent = container.HasParent();
            foreach (KeyValuePair<byte, OpenContainer> it in OpenContainers)
            {
		        OpenContainer openContainer = it.Value;
		        if (openContainer.Container == container)
                {
			        Connection.SendContainer(it.Key, container, hasParent, openContainer.Index);
		        }
	        }
        }

        public ushort GetSkillLevel(Skills skill)
        {
            return (ushort)(SkillLevels[(byte)skill] + VarSkillLevels[(byte)skill]);
        }

        public bool IsPremium()
        {
            if (ConfigManager.Instance[ConfigBool.FreePremium] || HasFlag(PlayerFlags.IsAlwaysPremium))
                return true;

            return PremiumDays > 0;
        }

        public void ResetIdleTime()
        {
            IdleTime = 0;
        }

        public sealed override void SetID()
        {
            if (Id == 0)
                Id = PlayerAutoID++;
        }
        
        public void KickPlayer(bool displayEffect)
        {
	        //g_creatureEvents->playerLogout(this); TODO: Creature Events
	        if (Connection != null)
                GameConnection.Logout(this, displayEffect, true);
	        else
		        Game.RemoveCreature(this);
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
        public void SendStats()
        {
            if (Connection != null)
            {
                Connection.SendStats();
                LastStatsTrainingTime = (ushort)(CharacterData.OfflineTrainingTime / 60 / 1000); //Miliseconds to minutes
            }
        }
        public void SendSkills()
        {
            if (Connection != null)
                Connection.SendSkills();
        }
        public void SendCreatureAppear(Creature creature, Position pos, bool isLogin)
        {
            if (Connection != null)
                Connection.SendAddCreature(creature, pos, creature.Parent.GetStackposOfCreature(this, creature), isLogin);
        }

        public void SendPingBack()
        {
            if (Connection != null)
                Connection.SendPingBack();
        }

        public void SendAddContainerItem(Container container, Item item)
        {
	        if (Connection == null)
		        return;

            foreach (KeyValuePair<byte, OpenContainer> it in OpenContainers)
            {
                OpenContainer openContainer = it.Value;
                if (openContainer.Container != container)
                    continue;

		        ushort slot = openContainer.Index;
		        if (container.Id == (ushort)FixedItems.BrowseField)
                {
			        ushort containerSize = (ushort)(container.ItemList.Count - 1);
			        ushort pageEnd = (ushort)(openContainer.Index + container.MaxSize - 1);

			        if (containerSize > pageEnd)
                    {
				        slot = pageEnd;
				        item = container.GetItemByIndex(pageEnd);
			        }
                    else
                    {
				        slot = containerSize;
			        }
		        }
                else if (openContainer.Index >= container.MaxSize)
                {
			        item = container.GetItemByIndex(openContainer.Index - 1);
		        }

		        Connection.SendAddContainerItem(it.Key, slot, item);
            }
        }

        public void SendUpdateContainerItem(Container container, ushort slot, Item newItem)
        {
	        if (Connection == null)
		        return;

            foreach (KeyValuePair<byte, OpenContainer> it in OpenContainers)
            {
                OpenContainer openContainer = it.Value;
		        if (openContainer.Container != container)
			        continue;

		        if (slot < openContainer.Index)
			        continue;

		        ushort pageEnd = (ushort)(openContainer.Index + container.MaxSize);
		        if (slot >= pageEnd)
			        continue;

		        Connection.SendUpdateContainerItem(it.Key, slot, newItem);
	        }
        }
        
        public void SendRemoveContainerItem(Container container, ushort slot)
        {
	        if (Connection == null)
		        return;

            foreach (KeyValuePair<byte, OpenContainer> it in OpenContainers)
            {
		        OpenContainer openContainer = it.Value;
		        if (openContainer.Container != container)
			        continue;

		        ushort firstIndex = openContainer.Index;
		        if (firstIndex > 0 && firstIndex >= container.ItemList.Count - 1)
                {
			        firstIndex -= container.MaxSize;
			        SendContainer(it.Key, container, false, firstIndex);
		        }

		        Connection.SendRemoveContainerItem(it.Key, Math.Max(slot, firstIndex), container.GetItemByIndex(container.MaxSize + firstIndex));
	        }
        }

        public void SendContainer(byte cid, Container container, bool hasParent, ushort firstIndex)
        {
	        if (Connection != null)
				Connection.SendContainer(cid, container, hasParent, firstIndex);
		}
        
		public void SendAddTileItem(Tile tile, Position pos, Item item)
        {
			if (Connection != null)
            {
				int stackpos = tile.GetStackposOfItem(this, item);
				if (stackpos != -1)
                {
					Connection.SendAddTileItem(pos, (byte)stackpos, item);
				}
			}
		}
		public void SendUpdateTileItem(Tile tile, Position pos, Item item)
        {
			if (Connection != null)
            {
				int stackpos = tile.GetStackposOfItem(this, item);
				if (stackpos != -1)
                {
					Connection.SendUpdateTileItem(pos, (byte)stackpos, item);
				}
			}
		}
		public void SendRemoveTileThing(Position pos, int stackpos)
        {
			if (stackpos != -1 && Connection != null)
				Connection.SendRemoveTileThing(pos, (byte)stackpos);
		}
		public void SendCreatureMove(Creature creature, Position newPos, int newStackPos, Position oldPos, int oldStackPos, bool teleport)
        {
			if (Connection != null)
            {
				Connection.SendMoveCreature(creature, newPos, newStackPos, oldPos, (byte)oldStackPos, teleport);
			}
		}
        public void SendCreatureChangeVisible(Creature creature, bool visible)
        {
			if (Connection == null)
				return;

			if (creature is Player)
            {
				if (visible)
					Connection.SendCreatureOutfit(creature, creature.CurrentOutfit);
				else
					Connection.SendCreatureOutfit(creature, Outfit.EmptyOutfit);
			}
            else if (CanSeeInvisibility())
            {
				Connection.SendCreatureOutfit(creature, creature.CurrentOutfit);
			}
            else
            {
				int stackpos = creature.Parent.GetStackposOfCreature(this, creature);
				if (stackpos == -1)
					return;

				if (visible)
					Connection.SendAddCreature(creature, creature.GetPosition(), stackpos, false);
				else
					Connection.SendRemoveTileThing(creature.GetPosition(), (byte)stackpos);
			}
		}

        public void SendCancelMessage(ReturnTypes message)
        {
	        SendCancelMessage(ReturnTypeStringifier.Stringify(message));
        }

		public void SendCancelMessage(string msg)
        {
			if (Connection != null)
				Connection.SendTextMessage(new TextMessage {Type = MessageTypes.StatusSmall, Text = msg});
		}
        
        public void SendTextMessage(TextMessage message)
        {
            if (Connection != null)
                Connection.SendTextMessage(message);
        }
		public void SendTextMessage(MessageTypes type, string message)
        {
			if (Connection != null) 
				Connection.SendTextMessage(new TextMessage{ Type = type, Text = message});
		}
        
		public void SendPrivateMessage(Player speaker, SpeakTypes type, string text)
        {
			if (Connection != null)
				Connection.SendPrivateMessage(speaker, type, text);
		}
		public void SendCreatureSay(Creature creature, SpeakTypes type, string text, Position pos = null)
        {
			if (Connection != null)
				Connection.SendCreatureSay(creature, type, text, pos);
		}
        public void SendChannelsDialog()
        {
            if (Connection != null)
            {
                Connection.SendChannelsDialog();
            }
        }
		public void SendChannel(ushort channelId, string channelName, Dictionary<uint, Player> channelUsers, Dictionary<uint, Player> invitedUsers)
        {
			if (Connection != null)
				Connection.SendChannel(channelId, channelName, channelUsers, invitedUsers);
		}
		public void SendCreatePrivateChannel(ushort channelId, string channelName)
        {
			if (Connection != null)
				Connection.SendCreatePrivateChannel(channelId, channelName);
		}
		public void SendOpenPrivateChannel(string receiver)
        {
			if (Connection != null)
				Connection.SendOpenPrivateChannel(receiver);
		}

		public void SendCancelWalk()
        {
			if (Connection != null)
				Connection.SendCancelWalk();
		}

		public void SendUpdateTile(Tile tile, Position pos)
        {
			if (Connection != null)
				Connection.SendUpdateTile(tile, pos);
		}

		public void SendCreatureTurn(Creature creature)
        {
			if (Connection != null && CanSeeCreature(creature))
            {
				int stackpos = creature.Parent.GetStackposOfCreature(this, creature);
				if (stackpos != -1)
                {
					Connection.SendCreatureTurn(creature, (byte)stackpos);
				}
			}
		}

        public void SendMagicEffect(Position position, MagicEffects effect)
        {
            if(Connection != null)
                Connection.SendMagicEffect(position, effect);
        }
        
		public void SendCreatureHealth(Creature creature)
        {
			if (Connection != null)
                Connection.SendCreatureHealth(creature);
		}

        public void SendChannelEvent(ushort channelId, string playerName, ChatChannelEvents channelEvent)
        {
            if (Connection != null)
                Connection.SendChannelEvent(channelId, playerName, channelEvent);
        }
		public void SendChannelMessage(string author, string text, SpeakTypes type, ushort channel)
        {
			if (Connection != null)
				Connection.SendChannelMessage(author, text, type, channel);
		}
		public void SendToChannel(Creature creature, SpeakTypes type, string text, ushort channelId)
        {
			if (Connection != null)
                Connection.SendToChannel(creature, type, text, channelId);
		}
        
        public void SendClosePrivate(ushort channelId)
        {
	        if (channelId == Constants.ChatChannelGuild || channelId == Constants.ChatChannelParty)
            {
		        Chat.RemoveUserFromChannel(this, channelId);
	        }

	        if (Connection != null)
		        Connection.SendClosePrivate(channelId);
        }

		public void SendChangeSpeed(Creature creature, uint newSpeed)
        {
			if (Connection != null)
				Connection.SendChangeSpeed(creature, newSpeed);
		}
		public void SendCreatureChangeOutfit(Creature creature, Outfit outfit)
        {
			if (Connection != null)
				Connection.SendCreatureOutfit(creature, outfit);
		}
		public void SendCreatureLight(Creature creature)
        {
			if (Connection != null)
				Connection.SendCreatureLight(creature);
		}
        public void SendSpellCooldown(byte spellId, uint time)
        {
            if (Connection != null)
                Connection.SendSpellCooldown(spellId, time);
        }
        public void SendSpellGroupCooldown(SpellGroups groupId, uint time)
        {
            if (Connection != null)
                Connection.SendSpellGroupCooldown(groupId, time);
        }
        public void SendCreatureSkull(Creature creature)
        {
            if (Connection != null)
                Connection.SendCreatureSkull(creature);
        }
		public void SendShop(Npc npc)
        {
            if (Connection != null)
                Connection.SendShop(npc, ShopItemList);
		}
        public void SendSaleItemList()
        {
            if (Connection != null)
                Connection.SendSaleItemList(ShopItemList);
        }
		public void SendCloseShop()
        {
            if (Connection != null)
                Connection.SendCloseShop();
		}
        #endregion
        
        public void ChangeSoul(int soulChange)
        {
	        if (soulChange > 0)
	        {
	            CharacterData.Soul += (byte)Math.Min(soulChange, VocationData.SoulMax - CharacterData.Soul);
	        }
            else
	        {
	            CharacterData.Soul = (byte)Math.Max(0, CharacterData.Soul + soulChange);
	        }

	        SendStats();
        }

        public void SetVarStats(Stats stat, int modifier)
        {
	        VarStats[(byte)stat] = (ushort)(VarStats[(byte)stat] + modifier);

	        switch (stat)
            {
		        case Stats.MaxHitPoints:
			        if (Health > HealthMax)
				        ChangeHealth(HealthMax - Health);
			        else
				        Game.AddCreatureHealth(this);
			        break;

		        case Stats.MaxManaPoints:
			        if (Mana > ManaMax)
				        ChangeMana(ManaMax - Mana);
			        break;
	        }
        }

        public void SetNextWalkTask(SchedulerTask task)
        {
	        if (NextStepEvent != 0) {
		        DispatcherManager.Scheduler.StopEvent(NextStepEvent);
		        NextStepEvent = 0;
	        }

	        if (task != null)
            {
                NextStepEvent = DispatcherManager.Scheduler.AddEvent(task);
		        ResetIdleTime();
	        }
        }

        public void SetNextWalkActionTask(SchedulerTask task)
        {
	        if (WalkTaskEvent != 0)
            {
		        DispatcherManager.Scheduler.StopEvent(WalkTaskEvent);
		        WalkTaskEvent = 0;
	        }

	        WalkTask = task;
        }
        
        public void SetNextActionTask(SchedulerTask task)
        {
	        if (ActionTaskEvent != 0) {
                DispatcherManager.Scheduler.StopEvent(ActionTaskEvent);
		        ActionTaskEvent = 0;
	        }

	        if (task != null) 
            {
                ActionTaskEvent = DispatcherManager.Scheduler.AddEvent(task);
		        ResetIdleTime();
	        }
        }

        public override string GetDescription(int lookDistance)
        {
            throw new NotImplementedException();
        }

        public override bool IsPushable()
        {
            throw new NotImplementedException();
        }

        public sealed override void PostRemoveNotification(Thing thing, Thing newParent, int index, CylinderLinks link = CylinderLinks.Owner)
        {
	        if (link == CylinderLinks.Owner)
            {
		        //calling movement scripts
		        //g_moveEvents->onPlayerDeEquip(this, thing->getItem(), static_cast<slots_t>(index)); //TODO: SCRIPTING
	        }

	        bool requireListUpdate = true;

	        if (link == CylinderLinks.Owner || link == CylinderLinks.TopParent)
	        {
	            Item i = newParent as Item;

		        if (i != null)
                {
			        requireListUpdate = i.GetHoldingPlayer() != this;
		        }
                else
                {
			        requireListUpdate = newParent != this;
		        }

		        UpdateInventoryWeight();
		        UpdateItemsLight();
		        SendStats();
	        }

            Item item = thing as Item;
	        if (item != null)
	        {
	            Container container = item as Container;
	            Container topContainer = null;

		        if (container != null)
                {
			        if (container.IsRemoved() || !Position.AreInRange(GetPosition(), container.GetPosition(), 1, 1, 0))
                    {
				        AutoCloseContainers(container);
			        }
                    else if (container.GetTopParent() == this)
                    {
				        OnSendContainer(container);
			        }
                    else if ((topContainer = container.GetTopParent() as Container) != null)
                    {
                        DepotChest depot = topContainer as DepotChest; 
				        if (depot != null)
                        {
					        bool isOwner = false;

                            foreach (KeyValuePair<uint, DepotChest> depotChest in DepotChests)
                            {
                                if (depotChest.Value == depot)
                                {
                                    isOwner = true;
                                    OnSendContainer(container);
                                }
                            }

					        if (!isOwner)
                            {
						        AutoCloseContainers(container);
					        }
				        }
                        else
                        {
					        OnSendContainer(container);
				        }
			        }
                    else
                    {
				        AutoCloseContainers(container);
			        }
		        }

		        if (ShopOwner != null && requireListUpdate)
                {
			        //UpdateSaleShopList(item); TODO: SALE SHOP
		        }
	        }
        }

        private void AutoCloseContainers(Container container)
        {
            List<byte> closeList = new List<byte>();
            foreach (KeyValuePair<byte, OpenContainer> it in OpenContainers)
            {
		        Container tmpContainer = it.Value.Container;
		        while (tmpContainer != null)
                {
			        if (tmpContainer.IsRemoved() || tmpContainer == container)
                    {
				        closeList.Add(it.Key);
				        break;
			        }

                    tmpContainer = tmpContainer.GetParent() as Container;
		        }
	        }

            foreach (byte containerId in closeList)
            {
		        CloseContainer(containerId);
		        if (Connection != null)
			        Connection.SendCloseContainer(containerId);
	        }
        }
        private void CloseContainer(byte cid)
        {
            OpenContainer openContainer;
            if(!OpenContainers.TryGetValue(cid, out openContainer))
                return;

            Container container = openContainer.Container;
	        OpenContainers.Remove(cid);

	        if (container != null && container.Id == (ushort)FixedItems.BrowseField)
		        container.DecrementReferenceCounter();
        }

        public void CheckTradeState(Item item)
        {
	        if (TradeItem == null || TradeState == TradeStates.Transfer)
		        return;

	        if (TradeItem == item)
		        Game.InternalCloseTrade(this);
	        else
	        {
	            Container container = item.Parent as Container; 
		        while (container != null)
                {
			        if (container == TradeItem)
                    {
				        Game.InternalCloseTrade(this);
				        break;
			        }

                    container = container.Parent as Container; 
		        }
	        }
        }

        #region Container Operations
        public class OpenContainer
        {
            public Container Container { get; set; }
            public ushort Index { get; set; }
        }

        public void OnAddContainerItem(Item item)
        {
	        CheckTradeState(item);
        }
        
        public void OnUpdateContainerItem(Container container, Item oldItem, Item newItem)
        {
	        if (oldItem != newItem) {
		        OnRemoveContainerItem(container, oldItem);
	        }

	        if (TradeState != TradeStates.Transfer)
		        CheckTradeState(oldItem);
        }
        
        public void OnRemoveContainerItem(Container container, Item item)
        {
		    CheckTradeState(item);

		    if (TradeItem != null)
            {
                if (TradeItem.Parent != container && container.IsHoldingItem(TradeItem))
                {
				    Game.InternalCloseTrade(this);
			    }
		    }
        }
        #endregion

        public uint IsMuted()
        {
            if (HasFlag(PlayerFlags.CannotBeMuted))
                return 0;

            int muteTicks = 0;
            foreach (Condition condition in Conditions)
            {
                if (condition.ConditionType == ConditionFlags.Muted && condition.Ticks > muteTicks)
                {
                    muteTicks = condition.Ticks;
                }
            }
            return (uint)(muteTicks / 1000);
        }

        public void AddMessageBuffer()
        {
            if (MessageBufferCount > 0 && ConfigManager.Instance[ConfigInt.MaxMessageBuffer] != 0 && !HasFlag(PlayerFlags.CannotBeMuted))
		        --MessageBufferCount;
        }

        public void RemoveMessageBuffer()
        {
            if (HasFlag(PlayerFlags.CannotBeMuted))
                return;

	        int maxMessageBuffer = ConfigManager.Instance[ConfigInt.MaxMessageBuffer];
	        if (maxMessageBuffer != 0 && MessageBufferCount <= maxMessageBuffer + 1)
            {
		        if (++MessageBufferCount > maxMessageBuffer)
                {
			        uint muteCount = 1;
                    //auto it = muteCountMap.find(guid); //TODO: Mute Map
                    //if (it != muteCountMap.end()) {
                    //    muteCount = it->second;
                    //}

			        uint muteTime = 5 * muteCount * muteCount;
                    //muteCountMap[guid] = muteCount + 1;
                    Condition condition = Condition.CreateCondition(ConditionIds.Default, ConditionFlags.Muted, (int)(muteTime * 1000), 0);
                    AddCondition(condition);
                    
			        SendTextMessage(MessageTypes.StatusSmall, string.Format("You are muted for {0} seconds.", muteTime));
		        }
	        }
        }

        public ulong GetMoney()
        {
            return 0; //TODO: Program Here
        }

        public void OpenShopWindow(Npc npc, List<ShopInfo> shop)
        {
	        ShopItemList = shop;
	        SendShop(npc);
	        SendSaleItemList();
        }
        public bool CloseShopWindow(bool sendCloseShopWindow = true)
        {
            //unreference callbacks
            int onBuy;
            int onSell;

            Npc npc = GetShopOwner(out onBuy, out onSell);
            if (npc == null)
            {
                ShopItemList.Clear();
                return false;
            }

            SetShopOwner(null, -1, -1);
            npc.FireOnPlayerEndTrade(this, onBuy, onSell);

            if (sendCloseShopWindow)
                SendCloseShop();

            ShopItemList.Clear();
            return true;
        }

        #region Shop Methods
        private void SetShopOwner(Npc owner, int onBuy, int onSell)
        {
            ShopOwner = owner;
            SaleCallback = onSell;
			PurchaseCallback = onBuy;
		}

        private Npc GetShopOwner(out int onBuy, out int onSell)
        {
			onBuy = PurchaseCallback;
			onSell = SaleCallback;
			return ShopOwner;
        }
        #endregion
    }
}
