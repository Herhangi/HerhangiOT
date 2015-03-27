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

        public static void Initialize()
        {
            _instance = new Game { GameState = GameStates.Startup };
        }

        public static void CharacterLogin()
        {
            
        }
    }
}
