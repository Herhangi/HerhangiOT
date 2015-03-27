using System;
using HerhangiOT.ServerLibrary.Model;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.LoginServer
{
    public class LoginSecretCommunication
    {
        public static SecretNetworkResponseType CheckCharacterAuthenticity(string username, string character, byte[] password)
        {
            byte[] hash = LoginServer.PasswordHasher.ComputeHash(password);
            string hashedPassword = string.Empty;
            foreach (byte b in hash)
                hashedPassword += b.ToString("x2");

            SecretNetworkResponseType response = SecretNetworkResponseType.Success;
            Account account = LoginServerData.RetrieveAccountData(username, hashedPassword, true);

            if (account == null)
                response = SecretNetworkResponseType.InvalidAccountData;
            else
            {
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
                            LoginServerData.SetUserOnline(username, character);
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
