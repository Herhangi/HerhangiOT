using System.Collections.Generic;
using System.Linq;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.GameServer.Model.Items;
using HerhangiOT.ServerLibrary;

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
        #region Singleton Implementation
        #endregion

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

        public static void Initialize()
        {
            GameState = GameStates.Startup;
            WorldLight = new LightInfo {Color = 0xD7, Level = Constants.LightLevelDay};
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

	        //creature.IncrementReferenceCounter(); //TODO
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
                //for (Creature* spectator : list) {
                //    spectator->onCreatureAppear(creature, true);
                //}

                //creature->getParent()->postAddNotification(creature, nullptr, 0);

                //addCreatureCheck(creature);
                //creature->onPlacedCreature();
	            return true;
        }

        public static void AddPlayer(Player player)
        {
            string lowercaseName = player.CharacterName.ToLowerInvariant();
            OnlinePlayers[lowercaseName] = player;
            //TODO: Wildcard tree
            OnlinePlayersById[player.Id] = player;
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

        #region Game Operations
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

	        if (text[0] == '/')// && player->isAccessPlayer()) { TODO: Player Access
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
			        //g_chat->talkToChannel(*player, type, text, channelId); TODO: Chat channels
			        break;

		        case SpeakTypes.PrivatePn:
			        PlayerSpeakToNpc(player, text);
			        break;

		        case SpeakTypes.Broadcast:
			        PlayerBroadcastMessage(player, text);
			        break;
	        }
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

	        if (type == SpeakTypes.PrivateRedTo)// && (player->hasFlag(PlayerFlag_CanTalkRedPrivate) || player.AccountType >= AccountTypes.GameMaster)) TODO: Player Flags
            {
		        type = SpeakTypes.PrivateRedFrom;
	        }
            else
            {
		        type = SpeakTypes.PrivateFrom;
	        }

	        toPlayer.SendPrivateMessage(player, type, text);
	        toPlayer.OnCreatureSay(player, type, text);

	        if (toPlayer.IsInGhostMode())// && !player->isAccessPlayer()) TODO: ACCESS PLAYER
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
            //if (!player.HasFlag(PlayerFlag_CanBroadcast)) { //TODO: Player Flags
            //    return false;
            //}

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
    }
}
