using System;
using System.Collections.Generic;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model.Items;
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
        public override sealed int StepSpeed { get { return Math.Max(PlayerMinSpeed, Math.Min(PlayerMaxSpeed, Speed)); } }

        public Town Town { get; private set; }
        public ushort[] VarSkillLevels { get; private set; }
        public ushort[] SkillLevels { get; private set; }
        public byte[] SkillPercents { get; private set; }
        public uint FreeCapacity { get { return uint.MaxValue; } } //TODO: Calculate
        public Item[] Inventory { get; private set; }

        public FightModes FightMode { get; set; }
        public ChaseModes ChaseMode { get; set; }
        public SecureModes SecureMode { get; set; }

        public bool IsPzLocked { get; protected set; }
        protected long LastWalkthroughAttempt { get; set; }
        protected Position LastWalkthroughPosition { get; set; }

        public uint IdleTime { get; set; }
        public uint WalkTaskEvent { get; set; }
        public uint ActionTaskEvent { get; set; }
        public SchedulerTask WalkTask { get; protected set; }

        public Item TradeItem { get; protected set; }
        public TradeStates TradeState { get; protected set; }

        public Npc ShopOwner { get; protected set; }

        public Dictionary<byte, OpenContainer> OpenContainers;
        public Dictionary<uint, DepotLocker> DepotLockers; 
        public Dictionary<uint, DepotChest> DepotChests; 
        public Dictionary<uint, int> StorageMap; 

        public override ushort Health { get { return CharacterData.Health; } protected set { CharacterData.Health = value; } }
        public override ushort HealthMax { get { return CharacterData.HealthMax; } protected set { CharacterData.HealthMax = value; } }

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
                LoginPosition = new Position(Town.TemplePosition); //Breaking reference
            }

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
        public override bool CanSeeCreature(Creature creature)
        {
	        if (creature == this)
		        return true;

	        if (creature.IsInGhostMode())// && !group->access) { TODO: GROUP ACCESS
		        return false;

	        if (!(creature is Player) && !CanSeeInvisibility() && creature.IsInvisible())
		        return false;

	        return true;
        }

        #region Creature Overrides
        public sealed override CreatureTypes GetCreatureType()
        {
            return CreatureTypes.Player;
        }

        public override string GetName()
        {
            return CharacterName;
        }

        public override void AddList()
        {
            //TODO: VIP LIST
            Game.Instance.AddPlayer(this);
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
        #endregion

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
            if (ConfigManager.Instance[ConfigBool.FreePremium]) //hasFlag(PlayerFlag_IsAlwaysPremium)) { //TODO: PLAYER FLAGS
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
			        ushort pageEnd = (ushort)(openContainer.Index + container.MaxSize);

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

        public void SendCancelMessage(ReturnTypes message)
        {
	        SendCancelMessage(ReturnTypeStringifier.Stringify(message));
        }

		public void SendCancelMessage(string msg)
        {
			if (Connection != null)
				Connection.SendTextMessage(new TextMessage {Type = MessageTypes.StatusSmall, Text = msg});
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
        #endregion
        
        public void SetNextWalkActionTask(SchedulerTask task)
        {
	        if (WalkTaskEvent != 0) {
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
		        Game.Instance.InternalCloseTrade(this);
	        else
	        {
	            Container container = item.Parent as Container; 
		        while (container != null)
                {
			        if (container == TradeItem)
                    {
				        Game.Instance.InternalCloseTrade(this);
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
				    Game.Instance.InternalCloseTrade(this);
			    }
		    }
        }
        #endregion
    }
}
