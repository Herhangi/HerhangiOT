using HerhangiOT.LoginServer;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.GameServer
{
    //TODO: EXTERNAL LOGIN SERVER FUNCTIONS WILL BE IMPLEMENTED LATER
    public class SecretCommunication
    {
        #region Character Authenticity
        public delegate void CharacterAuthenticationResultDelegate(SecretNetworkResponseType response, string sessionKey, string accountName, string character, uint accountId);
        public static event CharacterAuthenticationResultDelegate OnCharacterAuthenticationResultArrived;
        public static void FireOnCharacterAuthenticationResultArrivedEvent(SecretNetworkResponseType response, string sessionKey, string accountName, string character, uint accountId)
        {
            if (OnCharacterAuthenticationResultArrived != null)
                OnCharacterAuthenticationResultArrived(response, sessionKey, accountName, character, accountId);
        }

        public static void CheckCharacterAuthenticity(string sessionKey, string character)
        {
            if (ConfigManager.Instance[ConfigBool.UseExternalLoginServer])
            {
            }
            else
            {
                uint accountId;
                string accountName;
                SecretNetworkResponseType response = LoginSecretCommunication.CheckCharacterAuthenticity(sessionKey, character, out accountName, out accountId);
                FireOnCharacterAuthenticationResultArrivedEvent(response, sessionKey, accountName, character, accountId);
            }
        }
        #endregion
    }
}
