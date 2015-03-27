using HerhangiOT.LoginServer;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.GameServer
{
    //TODO: EXTERNAL LOGIN SERVER FUNCTIONS WILL BE IMPLEMENTED LATER
    public class SecretCommunication
    {
        #region Character Authenticity
        public delegate void CharacterAuthenticationResultDelegate(SecretNetworkResponseType response, string username, string character);
        public static event CharacterAuthenticationResultDelegate OnCharacterAuthenticationResultArrived;
        public static void FireOnCharacterAuthenticationResultArrivedEvent(SecretNetworkResponseType response, string username, string character)
        {
            if (OnCharacterAuthenticationResultArrived != null)
                OnCharacterAuthenticationResultArrived(response, username, character);
        }

        public static void CheckCharacterAuthenticity(string username, string character, byte[] password)
        {
            if (ConfigManager.Instance[ConfigBool.USE_EXTERNAL_LOGIN_SERVER])
            {
            }
            else
            {
                SecretNetworkResponseType response = LoginSecretCommunication.CheckCharacterAuthenticity(username, character, password);
                FireOnCharacterAuthenticationResultArrivedEvent(response, username, character);
            }
        }
        #endregion
    }
}
