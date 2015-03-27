using System;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Model;

namespace HerhangiOT.LoginServer
{
    internal static class LoginServerData
    {
        public const double DataValidityExtensionInMinutes = 2.5;

        public static Account RetrieveAccountData(string username, string hashedPassword, bool useCache = false)
        {
            if (useCache)
            {
                if (LoginServer.OnlineAccounts.ContainsKey(username))
                {
                    if(LoginServer.OnlineAccounts[username].Password.Equals(hashedPassword, StringComparison.InvariantCulture))
                        return LoginServer.OnlineAccounts[username];
                    return null; //Password is wrong!
                }
            }

            Account acc = Database.Instance.GetAccountInformation(username, hashedPassword);

            if (acc == null) return null;

            acc.ExpiresOn = DateTime.Now.AddMinutes(DataValidityExtensionInMinutes); //Expire

            if (LoginServer.OnlineAccounts.ContainsKey(username))
            {
                acc.OnlineCharacter = LoginServer.OnlineAccounts[username].OnlineCharacter; //For another character online system!
                LoginServer.OnlineAccounts.Remove(username);
            }
            LoginServer.OnlineAccounts.Add(username, acc);

            return acc;
        }

        public static void SetUserOnline(string username, string character)
        {
            if (!LoginServer.OnlineAccounts.ContainsKey(username))
                return; //TODO: RETRIEVE ACCOUNT DATA
            
            // Extend account data expiration as it is still online
            // Store online character, it might be useful for statistics in the future
            Account account = LoginServer.OnlineAccounts[username];
            account.ExpiresOn = DateTime.Now.AddMinutes(DataValidityExtensionInMinutes);
            account.OnlineCharacter = character;
        }
    }
}
