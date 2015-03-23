namespace HerhangiOT.GameServer
{
    public enum GameWorldTypes
    {
        Pvp,
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
        public static GameWorldTypes WorldType;

        public static void CharacterLogin()
        {
            
        }
    }
}
