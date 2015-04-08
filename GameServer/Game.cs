using System.Collections.Generic;
using HerhangiOT.GameServer.Model;
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

        public static void Initialize()
        {
            _instance = new Game { GameState = GameStates.Startup };
            _instance.WorldLight = new LightInfo {Color = 0xD7, Level = Constants.LightLevelDay};
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
            //TODO: Fill Method
            return null;
        }

        public Player GetPlayerByName(string playerName)
        {
            string lowercaseName = playerName.ToLowerInvariant();
            if (OnlinePlayers.ContainsKey(lowercaseName))
                return OnlinePlayers[lowercaseName];
            return null;
        }
    }
}
