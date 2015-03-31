using HerhangiOT.LoginServer;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.GameServer
{
    //TODO: EXTERNAL LOGIN SERVER FUNCTIONS WILL BE IMPLEMENTED LATER
    public class SecretCommunication
    {
        #region Character Authenticity
        public delegate void CharacterAuthenticationResultDelegate(SecretNetworkResponseType response, string accountName, string character, uint accountId);
        public static event CharacterAuthenticationResultDelegate OnCharacterAuthenticationResultArrived;
        public static void FireOnCharacterAuthenticationResultArrivedEvent(SecretNetworkResponseType response, string accountName, string character, uint accountId)
        {
            if (OnCharacterAuthenticationResultArrived != null)
                OnCharacterAuthenticationResultArrived(response, accountName, character, accountId);
        }

        public static void CheckCharacterAuthenticity(string accountName, string character, byte[] password)
        {
            if (ConfigManager.Instance[ConfigBool.UseExternalLoginServer])
            {
            }
            else
            {
                uint accountId;
                SecretNetworkResponseType response = LoginSecretCommunication.CheckCharacterAuthenticity(accountName, character, password, out accountId);
                FireOnCharacterAuthenticationResultArrivedEvent(response, accountName, character, accountId);
            }
        }
        #endregion
    }
}
