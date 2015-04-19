using System.Collections.Generic;
using System.Linq;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.GameServer.Model.Items;
using HerhangiOT.GameServer.Scriptability;
using HerhangiOT.GameServer.Scriptability.ChatChannels;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Threading;

namespace HerhangiOT.GameServer
{
    public enum GameWorldTypes
    {
        Pvp = 1,
        NoPvp,
        PvpEnforced
    }

    public enum GameStates
    {
        Startup,
        Init,
        Normal,
        Closed,
        Shutdown,
        Closing,
        Maintain
    }

    public static class Game
    {
        private static int _lastCheckedBucket = -1;

        private static GameStates _gameState;
        public static GameStates GameState
        {
            get { return _gameState; }
            set
            {
                if(_gameState == GameStates.Shutdown) return; //Shutdown cannot be stopped!

                if(_gameState == value) return; //Prevent doing same procedures!

                _gameState = value;
                switch (_gameState)
                {
                    case GameStates.Init:
                        //TODO: Program HERE
                        Groups.Load();
                        break;
                    case GameStates.Shutdown:
                        //TODO: Program HERE
                        break;
                    case GameStates.Closed:
                        //TODO: Program HERE
                        break;
                }
            }
        }
        public static GameWorldTypes WorldType { get; set; }

        public static LightInfo WorldLight { get; set; }

        public static Dictionary<string, Player> OnlinePlayers = new Dictionary<string, Player>();
        public static Dictionary<uint, Player> OnlinePlayersById = new Dictionary<uint, Player>();

        public static Dictionary<uint, Monster> Monsters = new Dictionary<uint, Monster>();
        public static Dictionary<uint, Npc> Npcs = new Dictionary<uint, Npc>();

        public static Dictionary<Tile, Container> BrowseFields = new Dictionary<Tile, Container>(); 
        public static Dictionary<uint, BedItem> BedSleepers = new Dictionary<uint, BedItem>(); 

        private static readonly List<Creature>[] CheckCreatureBuckets = new List<Creature>[Constants.JobCheckCreatureBucketCount]; 

        public static void Initialize()
        {
            GameState = GameStates.Startup;
            WorldLight = new LightInfo {Color = 0xD7, Level = Constants.LightLevelDay};

            for(int i = 0; i < Constants.JobCheckCreatureBucketCount; i++)
                CheckCreatureBuckets[i] = new List<Creature>();
        }

        public static void StartJobs()
        {
            DispatcherManager.Jobs.AddJob("CheckCreatures", JobTask.CreateJobTask(Constants.JobCheckCreatureInterval, 0, CheckCreatures));
        }


        public static Item TransformItem(Item item, ushort newId, int newCount = -1)
        {
            //TODO: Fill Method
            return null;
        }

        #region Internal Operations
        public static ReturnTypes InternalMoveCreature(Creature creature, Directions direction, CylinderFlags flags = CylinderFlags.None)
        {
	        Position currentPos = creature.Position;
	        Position destPos = Position.GetNextPosition(direction, currentPos);

	        bool diagonalMovement = (direction & Directions.DiagonalMask) != 0;
	        if (creature is Player && !diagonalMovement)
            {
		        //try go up
		        if (currentPos.Z != 8 && creature.Parent.HasHeight(3))
                {
			        Tile tmpTile = Map.GetTile(currentPos.Z, currentPos.Y, (byte)(currentPos.Z - 1));
			        if (tmpTile == null || (tmpTile.Ground == null && !tmpTile.HasProperty(ItemProperties.BlockSolid))) {
				        tmpTile = Map.GetTile(destPos.X, destPos.Y, (byte)(destPos.Z - 1));
				        if (tmpTile != null && tmpTile.Ground != null && !tmpTile.HasProperty(ItemProperties.BlockSolid)) {
                            flags |= CylinderFlags.IgnoreBlockItem | CylinderFlags.IgnoreBlockCreature;

					        if (!tmpTile.Flags.HasFlag(TileFlags.FloorChange))
                            {
						        destPos.Z--;
					        }
				        }
			        }
		        }
                else
                {
			        //try go down
			        Tile tmpTile = Map.GetTile(destPos.X, destPos.Y, destPos.Z);
			        if (currentPos.Z != 7 && (tmpTile == null || (tmpTile.Ground == null && !tmpTile.HasProperty(ItemProperties.BlockSolid))))
                    {
				        tmpTile = Map.GetTile(destPos.X, destPos.Y, (byte)(destPos.Z + 1));
				        if (tmpTile != null && tmpTile.HasHeight(3))
                        {
					        flags |= CylinderFlags.IgnoreBlockItem | CylinderFlags.IgnoreBlockCreature;
					        destPos.Z++;
				        }
			        }
		        }
	        }

	        Tile toTile = Map.GetTile(destPos.X, destPos.Y, destPos.Z);
	        if (toTile == null)
		        return ReturnTypes.NotPossible;

	        return InternalMoveCreature(creature, toTile, flags);
        }
        
        private static ReturnTypes InternalMoveCreature(Creature creature, Tile toTile, CylinderFlags flags = CylinderFlags.None)
        {
	        //check if we can move the creature to the destination
	        ReturnTypes ret = toTile.QueryAdd(0, creature, 1, flags);
	        if (ret != ReturnTypes.NoError)
		        return ret;

	        Map.MoveCreature(creature, toTile);
	        if (creature.Parent != toTile) {
		        return ReturnTypes.NoError;
	        }

	        int index = 0;
	        Item toItem = null;
	        Tile subCylinder;
	        Tile toCylinder = toTile;
	        uint n = 0;

	        while ((subCylinder = toTile.QueryDestination(ref index, creature, ref toItem, ref flags) as Tile) != toCylinder)
            {
		        Map.MoveCreature(creature, subCylinder);

		        if (creature.GetParent() != subCylinder) {
			        //could happen if a script move the creature
			        break;
		        }

		        toCylinder = subCylinder;
		        flags = 0;

		        //to prevent infinite loop
		        if (++n >= Map.MapMaxLayers)
			        break;
	        }

	        return ReturnTypes.NoError;
        }

        private static bool InternalPlaceCreature(Creature creature,Position position, bool extendedPosition = false, bool forced = false)
        {
	        if (creature.Parent != null)
		        return false;

	        if (!Map.PlaceCreature(position, creature, extendedPosition, forced)) {
		        return false;
	        }

	        creature.IncrementReferenceCounter();
	        creature.SetID();
	        creature.AddList();
	        return true;
        }

        public static void InternalCloseTrade(Player player)
        {
            //TODO: FILL THIS METHOD
        }
        #endregion

        public static bool PlaceCreature(Creature creature, Position position, bool extendedPosition = false, bool forced = false)
        {
            if (!InternalPlaceCreature(creature, position, extendedPosition, forced)) {
		        return false;
	        }

            HashSet<Creature> spectators = new HashSet<Creature>();
            Map.GetSpectators(ref spectators, creature.Position, true);
            foreach (Creature spectator in spectators)
            {
                Player tmpPlayer = spectator as Player;

                if (tmpPlayer != null)
                {
                    tmpPlayer.SendCreatureAppear(creature, creature.Position, true);
                }
            }

            foreach (Creature spectator in spectators)
            {
                spectator.OnCreatureAppear(creature, true);
            }

            creature.Parent.PostAddNotification(creature, null, 0);

            AddCreatureCheck(creature);
            creature.OnPlacedCreature();
	        return true;
        }

        private static int _lastAddedCreatureBucket = -1;
        public static void AddCreatureCheck(Creature creature)
        {
            creature.CreatureCheck = true;

            if(creature.InCheckCreaturesVector)
                return;

            _lastAddedCreatureBucket = (_lastAddedCreatureBucket + 1)%Constants.JobCheckCreatureBucketCount;
            creature.InCheckCreaturesVector = true;
            CheckCreatureBuckets[_lastAddedCreatureBucket].Add(creature);
            creature.IncrementReferenceCounter();
        }

        public static void RemoveCreatureCheck(Creature creature)
        {
	        if (creature.InCheckCreaturesVector)
		        creature.CreatureCheck = false;
        }

        public static bool RemoveCreature(Creature creature, bool isLogout = true)
        {
	        if (creature.IsRemoved())
		        return false;

	        Tile tile = creature.Parent;

            List<int> oldStackPosVector = new List<int>();

	        HashSet<Creature> list = new HashSet<Creature>();
	        Map.GetSpectators(ref list, tile.GetPosition(), true);
	        foreach (Player spectator in list.OfType<Player>())
            {
			    oldStackPosVector.Add(spectator.CanSeeCreature(creature) ? tile.GetStackposOfCreature(spectator, creature) : -1);
	        }

	        tile.RemoveCreature(creature);

	        Position tilePosition = tile.GetPosition();

	        //send to client
	        int i = 0;
	        foreach (Player spectator in list.OfType<Player>())
            {
			    spectator.SendRemoveTileThing(tilePosition, oldStackPosVector[i++]);
	        }

	        //event method
	        foreach (Creature spectator in list)
            {
		        spectator.OnRemoveCreature(creature, isLogout);
	        }

	        creature.Parent.PostRemoveNotification(creature, null, 0);

	        creature.RemoveList();
	        creature.IsInternalRemoved = true;
	        ReleaseCreature(creature);

	        RemoveCreatureCheck(creature);

	        foreach (Creature summon in creature.Summons)
	        {
	            summon.SkillLoss = false;
		        RemoveCreature(summon);
	        }
	        return true;
        }



        public static void AddPlayer(Player player)
        {
            string lowercaseName = player.CharacterName.ToLowerInvariant();
            OnlinePlayers[lowercaseName] = player;
            //TODO: Wildcard tree
            OnlinePlayersById[player.Id] = player;
        }

        public static void RemovePlayer(Player player)
        {
            string lowercaseName = player.CharacterName.ToLowerInvariant();
            OnlinePlayers.Remove(lowercaseName);
            //TODO: Wildcard tree
            OnlinePlayersById.Remove(player.Id);
        }

        #region Get Operations
        // Using TryGetValue as it seems faster: http://stackoverflow.com/questions/9382681/what-is-more-efficient-dictionary-trygetvalue-or-containskeyitem
        public static Creature GetCreatureById(uint id)
        {
            if (id <= Player.PlayerAutoID)
                return GetPlayerById(id);
            if (id <= Monster.MonsterAutoID)
                return GetMonsterById(id);
            if (id <= Npc.NpcAutoID)
                return GetNpcById(id);
            return null;
        }

        public static Player GetPlayerByName(string playerName)
        {
            string lowercaseName = playerName.ToLowerInvariant();
            Player player;
            OnlinePlayers.TryGetValue(lowercaseName, out player);
            return player;
        }

        public static Npc GetNpcById(uint npcId)
        {
            Npc npc;
            Npcs.TryGetValue(npcId, out npc);
            return npc;
        }
        public static Player GetPlayerById(uint playerId)
        {
            Player player;
            OnlinePlayersById.TryGetValue(playerId, out player);
            return player;
        }
        public static Monster GetMonsterById(uint monsterId)
        {
            Monster monster;
            Monsters.TryGetValue(monsterId, out monster);
            return monster;
        }
        #endregion
        
        public static void AddMagicEffect(Position position, MagicEffects effect)
        {
	        HashSet<Creature> list = new HashSet<Creature>();
	        Map.GetSpectators(ref list, position, true, true);
	        AddMagicEffect(list, position, effect);
        }
        public static void AddMagicEffect(HashSet<Creature> spectators, Position position, MagicEffects effect)
        {
            foreach (Player spectator in spectators.OfType<Player>())
            {
                spectator.SendMagicEffect(position, effect);
            }
        }

        public static void AddCreatureHealth(Creature target)
        {
            HashSet<Creature> spectators = new HashSet<Creature>();
            Map.GetSpectators(ref spectators, target.GetPosition(), true, true);
            AddCreatureHealth(spectators, target);
        }

        public static void AddCreatureHealth(HashSet<Creature> spectators, Creature target)
        {
            foreach (Player spectator in spectators.OfType<Player>())
            {
                spectator.SendCreatureHealth(target);
            }
        }

        public static void CheckCreatureWalk(uint creatureId)
        {
	        Creature creature = GetCreatureById(creatureId);
	        if (creature != null && creature.Health > 0)
            {
		        creature.OnWalk();
		        Cleanup();
	        }
        }
        
        public static void CheckCreatureAttack(uint creatureId)
        {
            Creature creature = GetCreatureById(creatureId);
	        if (creature != null && creature.Health > 0)
            {
		        creature.OnAttacking(0);
	        }
        }

        private static void Cleanup()
        {
            //TODO: free memory, MICRO MEMORY MANAGEMENT, WE MIGHT NOT NEED THIS
            //for (auto creature : ToReleaseCreatures) {
            //    creature->decrementReferenceCounter();
            //}
            //ToReleaseCreatures.clear();

            //for (auto item : ToReleaseItems) {
            //    item->decrementReferenceCounter();
            //}
            //ToReleaseItems.clear();

            //for (Item* item : toDecayItems) {
            //    const uint32_t dur = item->getDuration();
            //    if (dur >= EVENT_DECAYINTERVAL * EVENT_DECAY_BUCKETS) {
            //        decayItems[lastBucket].push_back(item);
            //    } else {
            //        decayItems[(lastBucket + 1 + dur / 1000) % EVENT_DECAY_BUCKETS].push_back(item);
            //    }
            //}
            //toDecayItems.clear();
        }

        public static void ReleaseCreature(Creature creature)
        {
	        //ToReleaseCreatures.push_back(creature); //TODO: MICRO MEMORY MANAGEMENT, WE MIGHT NOT NEED THIS
        }
        public static void ReleaseItem(Item item)
        {
            //ToReleaseItems.push_back(item); //TODO: MICRO MEMORY MANAGEMENT, WE MIGHT NOT NEED THIS
        }

        
        private static bool InternalCreatureTurn(Creature creature, Directions direction)
        {
	        if (creature.Direction == direction)
		        return false;

	        creature.Direction = direction;

	        HashSet<Creature> spectators = new HashSet<Creature>();
	        Map.GetSpectators(ref spectators, creature.GetPosition(), true, true);
	        foreach (Player spectator in spectators.OfType<Player>())
            {
		        spectator.SendCreatureTurn(creature);
	        }
	        return true;
        }

        #region Game Operations
        private static void CheckCreatures()
        {
            _lastCheckedBucket = (_lastCheckedBucket + 1) % Constants.JobCheckCreatureBucketCount;
            List<Creature> creaturesToCheck = CheckCreatureBuckets[_lastCheckedBucket];

            for(int i = creaturesToCheck.Count - 1; i > -1; i--)
            {
                Creature creature = creaturesToCheck[i];

                if (creature.CreatureCheck)
                {
                    if (creature.Health > 0)
                    {
                        creature.OnThink(Constants.JobCheckCreatureInterval);
                        creature.OnAttacking(Constants.JobCheckCreatureInterval);
                        creature.ExecuteConditions(Constants.JobCheckCreatureInterval);
                    }
                    else
                    {
                        creature.OnDeath();
                    }
                }
                else
                {
                    creature.InCheckCreaturesVector = false;
                    creaturesToCheck.RemoveAt(i);
                    ReleaseCreature(creature);
                }
            }

	        Cleanup();
        }

        public static void PlayerReceivePingBack(uint playerId)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;

            player.SendPingBack();
        }

        public static void PlayerSetFightModes(uint playerId, FightModes fightMode, ChaseModes chaseMode, SecureModes secureMode)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;

            player.FightMode = fightMode;
            player.ChaseMode = chaseMode;
            player.SecureMode = secureMode;
        }

        public static void PlayerMove(uint playerId, Directions direction)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;
            
	        player.ResetIdleTime();
	        player.SetNextWalkActionTask(null);
	        player.StartAutoWalk(direction);
        }

        public static void PlayerAutoWalk(uint playerId, Queue<Directions> directions)
        {
	        Player player = GetPlayerById(playerId);
	        if (player == null)
		        return;

	        player.ResetIdleTime();
	        player.SetNextWalkTask(null);
	        player.StartAutoWalk(directions);
        }

        public static void PlayerStopAutoWalk(uint playerId)
        {
	        Player player = GetPlayerById(playerId);
	        if (player == null)
		        return;

            player.CancelNextWalk = true;
        }

        public static void PlayerTurn(uint playerId, Directions direction)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;

            //if (!g_events->eventPlayerOnTurn(player, direction)) TODO: Events
            //    return;

	        player.ResetIdleTime();
            InternalCreatureTurn(player, direction);
        }

        public static void PlayerSay(uint playerId, ushort channelId, SpeakTypes type, string receiver, string text)
        {
	        Player player = GetPlayerById(playerId);
	        if (player == null || string.IsNullOrWhiteSpace(text))
		        return;

	        player.ResetIdleTime();

	        uint muteTime = player.IsMuted();
	        if (muteTime > 0)
	        {
	            string message = string.Format("You are still muted for {0} seconds.", muteTime);
		        player.SendTextMessage(MessageTypes.StatusSmall, message);
		        return;
	        }

	        if (PlayerSayCommand(player, text))
		        return;

	        if (PlayerSaySpell(player, type, text))
		        return;

	        if (text[0] == '/' && player.Group.Access)
		        return;

	        if (type != SpeakTypes.PrivatePn)
		        player.RemoveMessageBuffer();

	        switch (type)
            {
		        case SpeakTypes.Say:
			        InternalCreatureSay(player, SpeakTypes.Say, text, false);
			        break;

		        case SpeakTypes.Whisper:
			        PlayerWhisper(player, text);
			        break;

		        case SpeakTypes.Yell:
			        PlayerYell(player, text);
			        break;

		        case SpeakTypes.PrivateTo:
		        case SpeakTypes.PrivateRedTo:
			        PlayerSpeakTo(player, type, receiver, text);
			        break;

		        case SpeakTypes.ChannelO:
		        case SpeakTypes.ChannelY:
		        case SpeakTypes.ChannelR1:
                    Chat.TalkToChannel(player, type, text, channelId);
			        break;

		        case SpeakTypes.PrivatePn:
			        PlayerSpeakToNpc(player, text);
			        break;

		        case SpeakTypes.Broadcast:
			        PlayerBroadcastMessage(player, text);
			        break;
	        }
        }
        public static void PlayerRequestChannels(uint playerId)
        {
	        Player player = GetPlayerById(playerId);
	        if (player == null)
		        return;

	        player.SendChannelsDialog();
        }
        public static void PlayerChannelOpen(uint playerId, ushort channelId)
        {
	        Player player = GetPlayerById(playerId);
	        if (player == null)
		        return;

            ChatChannel channel = Chat.AddUserToChannel(player, channelId);
            if(channel == null)
                return;

            Dictionary<uint, Player> invitedUsers = channel.Invites;
            Dictionary<uint, Player> users = null;
            if (!channel.IsPublicChannel)
                users = channel.Users;

            player.SendChannel(channel.Id, channel.Name, users, invitedUsers);
        }
        public static void PlayerChannelClose(uint playerId, ushort channelId)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;

            Chat.RemoveUserFromChannel(player, channelId);
        }
        public static void PlayerPrivateChannelCreate(uint playerId)
        {
            Player player = GetPlayerById(playerId);
            if (player == null || !player.IsPremium())
                return;

	        ChatChannel channel = Chat.CreateChannel(player, Constants.ChatChannelPrivate);
	        if (channel == null || !channel.AddUser(player))
		        return;

	        player.SendCreatePrivateChannel(channel.Id, channel.Name);
        }
        public static void PlayerPrivateChannelOpen(uint playerId, string receiver)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;
            
            //if (!IOLoginData::formatPlayerName(receiver)) { //TODO: Learn what this is
            //    player->sendCancelMessage("A player with this name does not exist.");
            //    return;
            //}

            player.SendOpenPrivateChannel(receiver);
        }
        public static void PlayerPrivateChannelInvite(uint playerId, string name)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;

	        PrivateChatChannel channel = Chat.GetPrivateChannel(player);
	        if (channel == null)
		        return;

	        Player invitePlayer = GetPlayerByName(name);
	        if (invitePlayer == null)
		        return;

	        if (player == invitePlayer)
		        return;

	        channel.InvitePlayer(player, invitePlayer);
        }
        public static void PlayerPrivateChannelExclude(uint playerId, string name)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;

	        PrivateChatChannel channel = Chat.GetPrivateChannel(player);
	        if (channel == null)
		        return;

	        Player excludePlayer = GetPlayerByName(name);
	        if (excludePlayer == null)
		        return;

	        if (player == excludePlayer)
		        return;

	        channel.ExcludePlayer(player, excludePlayer);
        }

        public static void KickPlayer(uint playerId, bool displayEffect)
        {
	        Player player = GetPlayerById(playerId);
	        if (player == null)
		        return;

	        player.KickPlayer(displayEffect);
        }

        #region Talk Actions
        private static bool PlayerSayCommand(Player player, string text)
        {
            //TODO: Player Commands
            //char firstCharacter = text.front();
            //for (char commandTag : commandTags) {
            //    if (commandTag == firstCharacter) {
            //        if (commands.exeCommand(*player, text)) {
            //            return true;
            //        }
            //    }
            //}
	        return false;
        }

        private static bool PlayerSaySpell(Player player, SpeakTypes type, string text)
        {
            //TODO: Player Spells
            //        std::string words = text;

            //TalkActionResult_t result = g_talkActions->playerSaySpell(player, type, words);
            //if (result == TALKACTION_BREAK) {
            //    return true;
            //}

            //result = g_spells->playerSaySpell(player, words);
            //if (result == TALKACTION_BREAK) {
            //    if (!g_config.getBoolean(ConfigManager::EMOTE_SPELLS)) {
            //        return internalCreatureSay(player, TALKTYPE_SAY, words, false);
            //    } else {
            //        return internalCreatureSay(player, TALKTYPE_MONSTER_SAY, words, false);
            //    }

            //} else if (result == TALKACTION_FAILED) {
            //    return true;
            //}
	        return false;
        }
        
        private static void PlayerWhisper(Player player, string text)
        {
	        HashSet<Creature> list = new HashSet<Creature>();
	        Map.GetSpectators(ref list, player.GetPosition(), false, false, Map.MaxClientViewportX, Map.MaxClientViewportX, Map.MaxClientViewportY, Map.MaxClientViewportY);

	        //send to client
	        foreach (Player spectator in list.OfType<Player>())
	        {
			    if (!Position.AreInRange(player.GetPosition(), spectator.GetPosition(), 1, 1, 0))
                {
				    spectator.SendCreatureSay(player, SpeakTypes.Whisper, "pspsps");
			    }
                else
                {
				    spectator.SendCreatureSay(player, SpeakTypes.Whisper, text);
			    }
	        }

	        //event method
	        foreach (Creature spectator in list)
            {
		        spectator.OnCreatureSay(player, SpeakTypes.Whisper, text);
	        }
        }
        
        private static bool PlayerYell(Player player, string text)
        {
	        if (player.CharacterData.Level == 1)
            {
		        player.SendTextMessage(MessageTypes.StatusSmall, "You may not yell as long as you are on level 1.");
		        return false;
	        }

            //if (player.HasCondition(CONDITION_YELLTICKS)) { //TODO: Conditions
            //    player.SendCancelMessage(ReturnTypes.YouAreExhausted);
            //    return false;
            //}

	        if (player.AccountType < AccountTypes.GameMaster)
            {
                //Condition* condition = Condition::createCondition(CONDITIONID_DEFAULT, CONDITION_YELLTICKS, 30000, 0); //TODO: Conditions
                //player->addCondition(condition);
	        }

	        InternalCreatureSay(player, SpeakTypes.Yell, text.ToUpperInvariant(), false);
	        return true;
        }

        private static bool PlayerSpeakTo(Player player, SpeakTypes type, string receiver, string text)
        {
	        Player toPlayer = GetPlayerByName(receiver);
	        if (toPlayer == null)
            {
		        player.SendTextMessage(MessageTypes.StatusSmall, "A player with this name is not online.");
		        return false;
	        }

	        if (type == SpeakTypes.PrivateRedTo && (player.HasFlag(PlayerFlags.CanTalkRedPrivate) || player.AccountType >= AccountTypes.GameMaster))
            {
		        type = SpeakTypes.PrivateRedFrom;
	        }
            else
            {
		        type = SpeakTypes.PrivateFrom;
	        }

	        toPlayer.SendPrivateMessage(player, type, text);
	        toPlayer.OnCreatureSay(player, type, text);

	        if (toPlayer.IsInGhostMode() && !player.Group.Access)
            {
		        player.SendTextMessage(MessageTypes.StatusSmall, "A player with this name is not online.");
	        }
            else
            {
		        player.SendTextMessage(MessageTypes.StatusSmall, string.Format("Message sent to {0}.", toPlayer.GetName()));
	        }
	        return true;
        }
        
        private static void PlayerSpeakToNpc(Player player, string text)
        {
	        HashSet<Creature> list = new HashSet<Creature>();
	        Map.GetSpectators(ref list, player.GetPosition());
	        foreach (Npc spectator in list.OfType<Npc>())
            {
                spectator.OnCreatureSay(player, SpeakTypes.PrivatePn, text);
	        }
        }
        
        private static bool PlayerBroadcastMessage(Player player, string text)
        {
            if (!player.HasFlag(PlayerFlags.CanBroadcast))
                return false;

            Logger.Log(LogLevels.Information, "{0} broadcasted: \"{1}\".", player.GetName(), text);

            foreach (Player onlinePlayer in OnlinePlayers.Values)
            {
                onlinePlayer.SendPrivateMessage(player, SpeakTypes.Broadcast, text);
            }

	        return true;
        }

        private static void InternalCreatureSay(Creature creature, SpeakTypes type, string text, bool ghostMode, HashSet<Creature> spectatorsPtr = null, Position pos= null)
        {
	        if (pos == null)
            {
		        pos = creature.GetPosition();
	        }

            HashSet<Creature> spectators;
            if (spectatorsPtr == null || spectatorsPtr.Count == 0)
            {
                spectators = new HashSet<Creature>();
                if(type != SpeakTypes.Yell && type != SpeakTypes.MonsterYell)
                    Map.GetSpectators(ref spectators, pos, false, false, Map.MaxClientViewportX, Map.MaxClientViewportX, Map.MaxClientViewportY, Map.MaxClientViewportY);
                else
                    Map.GetSpectators(ref spectators, pos, true, false, 18, 18, 14, 14);
            }
            else
            {
                spectators = spectatorsPtr;
            }

	        //send to client
	        foreach (Creature spectator in spectators)
	        {
	            Player tmpPlayer = spectator as Player;
		        if (tmpPlayer != null)
                {
			        if (!ghostMode || tmpPlayer.CanSeeCreature(creature))
                    {
				        tmpPlayer.SendCreatureSay(creature, type, text, pos);
			        }
		        }
	        }

	        //event method
	        foreach (Creature spectator in spectators)
            {
		        spectator.OnCreatureSay(creature, type, text);
	        }
        }
        #endregion
        #endregion

        #region Bed Methods
        public static BedItem GetBedBySleeper(uint guid)
        {
            BedItem bed;
            BedSleepers.TryGetValue(guid, out bed);
            return bed;
        }
        
        public static void SetBedSleeper(BedItem bed, uint guid)
        {
	        BedSleepers[guid] = bed;
        }

        public static void RemoveBedSleeper(uint guid)
        {
            BedSleepers.Remove(guid);
        }
        #endregion
    }
}
