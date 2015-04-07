using System;
using HerhangiOT.ServerLibrary.Database.Model;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.LoginServer
{
    public class LoginSecretCommunication
    {
        public static SecretNetworkResponseType CheckCharacterAuthenticity(string sessionKey, string character, out string accountName, out uint accountId)
        {
            accountId = 0;
            accountName = string.Empty;
            SecretNetworkResponseType response = SecretNetworkResponseType.Success;
            AccountModel account = LoginServerData.RetrieveAccountData(sessionKey);
            
            if (account == null)
                response = SecretNetworkResponseType.SessionCouldNotBeFound;
            else
            {
                accountId = account.AccountId;
                accountName = account.AccountName;

                if (LoginServer.OnlineCharactersByAccount.ContainsKey(account.AccountName) && !LoginServer.OnlineCharactersByAccount[account.AccountName].Equals(character))
                    response = SecretNetworkResponseType.AnotherCharacterOnline;
                else
                {
                    bool isCharacterFound = false;
                    for (int i = 0; i < account.Characters.Count; i++)
                    {
                        if (account.Characters[i].CharacterName.Equals(character, StringComparison.InvariantCulture))
                        {
                            isCharacterFound = true;
                            LoginServerData.SetCharacterOnline(sessionKey, character);
                            break;
                        }
                    }

                    if(!isCharacterFound)
                        response = SecretNetworkResponseType.CharacterCouldNotBeFound;
                }
            }

            return response;
        }
    }
}
