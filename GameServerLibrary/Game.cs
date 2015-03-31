using System.Collections.Generic;
using HerhangiOT.GameServerLibrary.Model;

namespace HerhangiOT.GameServerLibrary
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

        public static Dictionary<string, Player> OnlinePlayers = new Dictionary<string, Player>();

        public static void Initialize()
        {
            _instance = new Game { GameState = GameStates.Startup };
        }
        

        private bool InternalPlaceCreature(Creature creature,Position position, bool extendedPosition = false, bool forced = false)
        {
	        if (creature.Parent != null)
		        return false;

	        if (!Map.Instance.PlaceCreature(position, creature, extendedPosition, forced)) {
		        return false;
	        }

	        //creature.IncrementReferenceCounter(); //TODO
	        //creature->setID();
	        //creature->addList();
	        return true;
        }

        public bool PlaceCreature(Creature creature, Position position, bool extendedPosition = false, bool forced = false)
        {
            	if (!InternalPlaceCreature(creature, position, extendedPosition, forced)) {
		            return false;
	            }

                //SpectatorVec list;
                //map.getSpectators(list, creature->getPosition(), true);
                //for (Creature* spectator : list) {
                //    if (Player* tmpPlayer = spectator->getPlayer()) {
                //        tmpPlayer->sendCreatureAppear(creature, creature->getPosition(), true);
                //    }
                //}

                //for (Creature* spectator : list) {
                //    spectator->onCreatureAppear(creature, true);
                //}

                //creature->getParent()->postAddNotification(creature, nullptr, 0);

                //addCreatureCheck(creature);
                //creature->onPlacedCreature();
	            return true;
        }
    }
}
