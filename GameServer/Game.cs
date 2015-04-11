using System.Collections.Generic;
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
        #endregion
    }
}
