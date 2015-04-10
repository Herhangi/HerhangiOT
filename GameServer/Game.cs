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

    public class Game
    {
        #region Singleton Implementation
        private static Game _instance;
        public static Game Instance { get { return _instance; } }
        #endregion

        private GameStates _gameState;
        public GameStates GameState
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
        public GameWorldTypes WorldType;

        public LightInfo WorldLight { get; set; }

        public static Dictionary<string, Player> OnlinePlayers = new Dictionary<string, Player>();
        public static Dictionary<uint, Player> OnlinePlayersById = new Dictionary<uint, Player>();

        public Dictionary<Tile, Container> BrowseFields = new Dictionary<Tile, Container>(); 

        public static void Initialize()
        {
            _instance = new Game { GameState = GameStates.Startup };
            _instance.WorldLight = new LightInfo {Color = 0xD7, Level = Constants.LightLevelDay};
        }

        public ReturnTypes InternalMoveCreature(Creature creature, Directions direction, CylinderFlags flags = CylinderFlags.None)
        {
	        Position currentPos = creature.Position;
	        Position destPos = Position.GetNextPosition(direction, currentPos);

	        bool diagonalMovement = (direction & Directions.DiagonalMask) != 0;
	        if (creature is Player && !diagonalMovement)
            {
		        //try go up
		        if (currentPos.Z != 8 && creature.Parent.HasHeight(3))
                {
			        Tile tmpTile = Map.Instance.GetTile(currentPos.Z, currentPos.Y, (byte)(currentPos.Z - 1));
			        if (tmpTile == null || (tmpTile.Ground == null && !tmpTile.HasProperty(ItemProperties.BlockSolid))) {
				        tmpTile = Map.Instance.GetTile(destPos.X, destPos.Y, (byte)(destPos.Z - 1));
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
			        Tile tmpTile = Map.Instance.GetTile(destPos.X, destPos.Y, destPos.Z);
			        if (currentPos.Z != 7 && (tmpTile == null || (tmpTile.Ground == null && !tmpTile.HasProperty(ItemProperties.BlockSolid))))
                    {
				        tmpTile = Map.Instance.GetTile(destPos.X, destPos.Y, (byte)(destPos.Z + 1));
				        if (tmpTile != null && tmpTile.HasHeight(3))
                        {
					        flags |= CylinderFlags.IgnoreBlockItem | CylinderFlags.IgnoreBlockCreature;
					        destPos.Z++;
				        }
			        }
		        }
	        }

	        Tile toTile = Map.Instance.GetTile(destPos.X, destPos.Y, destPos.Z);
	        if (toTile == null)
		        return ReturnTypes.NotPossible;

	        return InternalMoveCreature(creature, toTile, flags);
        }
        
        ReturnTypes InternalMoveCreature(Creature creature, Tile toTile, CylinderFlags flags = CylinderFlags.None)
        {
	        //check if we can move the creature to the destination
	        ReturnTypes ret = toTile.QueryAdd(0, creature, 1, flags);
	        if (ret != ReturnTypes.NoError)
		        return ret;

	        Map.Instance.MoveCreature(creature, toTile);
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
		        Map.Instance.MoveCreature(creature, subCylinder);

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


        private bool InternalPlaceCreature(Creature creature,Position position, bool extendedPosition = false, bool forced = false)
        {
	        if (creature.Parent != null)
		        return false;

	        if (!Map.Instance.PlaceCreature(position, creature, extendedPosition, forced)) {
		        return false;
	        }

	        //creature.IncrementReferenceCounter(); //TODO
	        creature.SetID();
	        creature.AddList();
	        return true;
        }

        public void InternalCloseTrade(Player player)
        {
            //TODO: FILL THIS METHOD
        }

        public bool PlaceCreature(Creature creature, Position position, bool extendedPosition = false, bool forced = false)
        {
            if (!InternalPlaceCreature(creature, position, extendedPosition, forced)) {
		        return false;
	        }

            HashSet<Creature> spectators = new HashSet<Creature>();
            Map.Instance.GetSpectators(ref spectators, creature.Position, true);
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

        public void AddPlayer(Player player)
        {
            string lowercaseName = player.CharacterName.ToLowerInvariant();
            OnlinePlayers[lowercaseName] = player;
            //TODO: Wildcard tree
            OnlinePlayersById[player.Id] = player;
        }

        public Creature GetCreatureById(uint id)
        {
            if (id <= Player.PlayerAutoID)
                return GetPlayerById(id);
            if (id <= Monster.MonsterAutoID)
                return null;
            //TODO: Monster and NPC
            return null;
        }

        public Player GetPlayerByName(string playerName)
        {
            string lowercaseName = playerName.ToLowerInvariant();
            if (OnlinePlayers.ContainsKey(lowercaseName))
                return OnlinePlayers[lowercaseName];
            return null;
        }

        public Player GetPlayerById(uint playerId)
        {
            if (OnlinePlayersById.ContainsKey(playerId))
                return OnlinePlayersById[playerId];
            return null;
        }
     
        public void CheckCreatureWalk(uint creatureId)
        {
	        Creature creature = GetCreatureById(creatureId);
	        if (creature != null && creature.Health > 0)
            {
		        creature.OnWalk();
		        Cleanup();
	        }
        }
        
        public void CheckCreatureAttack(uint creatureId)
        {
            Creature creature = GetCreatureById(creatureId);
	        if (creature != null && creature.Health > 0)
            {
		        creature.OnAttacking(0);
	        }
        }

        private void Cleanup()
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

        public void ReleaseCreature(Creature creature)
        {
	        //ToReleaseCreatures.push_back(creature); //TODO: MICRO MEMORY MANAGEMENT, WE MIGHT NOT NEED THIS
        }
        public void ReleaseItem(Item item)
        {
            //ToReleaseItems.push_back(item); //TODO: MICRO MEMORY MANAGEMENT, WE MIGHT NOT NEED THIS
        }

        #region Game Operations
        public void PlayerReceivePingBack(uint playerId)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;

            player.SendPingBack();
        }

        public void PlayerSetFightModes(uint playerId, FightModes fightMode, ChaseModes chaseMode, SecureModes secureMode)
        {
            Player player = GetPlayerById(playerId);
            if (player == null)
                return;

            player.FightMode = fightMode;
            player.ChaseMode = chaseMode;
            player.SecureMode = secureMode;
        }

        public void PlayerMove(uint playerId, Directions direction)
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
