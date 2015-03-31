using System;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Database.Model;

namespace HerhangiOT.LoginServer
{
    internal static class LoginServerData
    {
        public const double DataValidityExtensionInMinutes = 2.5;

        public static AccountModel RetrieveAccountData(string accountName, string hashedPassword, bool useCache = false)
        {
            if (useCache)
            {
                if (LoginServer.OnlineAccounts.ContainsKey(accountName))
                {
                    if(LoginServer.OnlineAccounts[accountName].Password.Equals(hashedPassword, StringComparison.InvariantCulture))
                        return LoginServer.OnlineAccounts[accountName];
                    return null; //Password is wrong!
                }
            }

            AccountModel acc = Database.Instance.GetAccountInformation(accountName, hashedPassword);

            if (acc == null) return null;

            acc.ExpiresOn = DateTime.Now.AddMinutes(DataValidityExtensionInMinutes); //Expire

            if (LoginServer.OnlineAccounts.ContainsKey(accountName))
            {
                acc.OnlineCharacter = LoginServer.OnlineAccounts[accountName].OnlineCharacter; //For another character online system!
                LoginServer.OnlineAccounts.Remove(accountName);
            }
            LoginServer.OnlineAccounts.Add(accountName, acc);

            return acc;
        }

        public static void SetAccountOnline(string accountName, string character)
        {
            if (!LoginServer.OnlineAccounts.ContainsKey(accountName))
                return; //TODO: RETRIEVE ACCOUNT DATA
            
            // Extend account data expiration as it is still online
            // Store online character, it might be useful for statistics in the future
            AccountModel account = LoginServer.OnlineAccounts[accountName];
            account.ExpiresOn = DateTime.Now.AddMinutes(DataValidityExtensionInMinutes);
            account.OnlineCharacter = character;
        }
    }
}
