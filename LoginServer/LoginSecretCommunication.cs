using System;
using HerhangiOT.ServerLibrary.Database.Model;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.LoginServer
{
    public class LoginSecretCommunication
    {
        public static SecretNetworkResponseType CheckCharacterAuthenticity(string accountName, string character, byte[] password, out uint accountId)
        {
            byte[] hash = LoginServer.PasswordHasher.ComputeHash(password);
            string hashedPassword = string.Empty;
            foreach (byte b in hash)
                hashedPassword += b.ToString("x2");

            accountId = 0;
            SecretNetworkResponseType response = SecretNetworkResponseType.Success;
            AccountModel account = LoginServerData.RetrieveAccountData(accountName, hashedPassword, true);
            
            if (account == null)
                response = SecretNetworkResponseType.InvalidAccountData;
            else
            {
                accountId = account.AccountId;

                if (!string.IsNullOrEmpty(account.OnlineCharacter) && !account.OnlineCharacter.Equals(character, StringComparison.InvariantCulture))
                    response = SecretNetworkResponseType.AnotherCharacterOnline;
                else
                {
                    bool isCharacterFound = false;
                    for (int i = 0; i < account.Characters.Count; i++)
                    {
                        if (account.Characters[i].CharacterName.Equals(character, StringComparison.InvariantCulture))
                        {
                            isCharacterFound = true;
                            LoginServerData.SetAccountOnline(accountName, character);
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
