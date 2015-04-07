using System;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Database.Model;

namespace HerhangiOT.LoginServer
{
    internal static class LoginServerData
    {
        public const double DataValidityExtensionInMinutes = 2.5;

        public static AccountModel RetrieveAccountData(string sessionKey)
        {
            if (LoginServer.OnlineAccounts.ContainsKey(sessionKey))
                return LoginServer.OnlineAccounts[sessionKey];
            return null;
        }

        public static AccountModel RetrieveAccountData(string accountName, string hashedPassword, out string sessionKey)
        {
            sessionKey = null;
            AccountModel acc = Database.Instance.GetAccountInformation(accountName, hashedPassword);

            if (acc == null) return null;

            acc.ExpiresOn = DateTime.Now.AddMinutes(DataValidityExtensionInMinutes); //Expire

            do
            {
                sessionKey = Guid.NewGuid().ToString();
            } while (LoginServer.OnlineAccounts.ContainsKey(sessionKey)); //We do not want sessionKeys to collide, this is too low probability, however can cause big time issue if not checked

            LoginServer.OnlineAccounts[sessionKey] = acc;
            return acc;
        }

        public static void SetCharacterOnline(string sessionKey, string character)
        {
            if (!LoginServer.OnlineAccounts.ContainsKey(sessionKey))
                return; //This should not happen
            
            // Extend account data expiration as it is still online
            // Store online character, it might be useful for statistics in the future
            LoginServer.OnlineAccounts[sessionKey].ExpiresOn = DateTime.Now.AddMinutes(DataValidityExtensionInMinutes);
            string accountName = LoginServer.OnlineAccounts[sessionKey].AccountName;
            LoginServer.OnlineCharactersByAccount[accountName] = character;
        }
    }
}
