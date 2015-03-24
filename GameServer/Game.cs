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

        public GameStates GameState;
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
