using System;
using System.Collections.Generic;
using HerhangiOT.ServerLibrary.Database.Model;

namespace HerhangiOT.ServerLibrary.Database
{
    public abstract class Database
    {
        protected static bool IsInitialized;

        public static Database Instance { get; protected set; }

        public static bool Initialize()
        {
            if (IsInitialized)
                return false;

            Logger.LogOperationStart(string.Format("Initializing database connection<{0}>", ConfigManager.Instance[ConfigStr.DatabaseType]));

            try
            {
                switch (ConfigManager.Instance[ConfigStr.DatabaseType])
                {
                    case "mssql":
                        Instance = new DatabaseMssql();
                        break;
                    case "json":
                        Instance = new DatabaseJson();
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.LogOperationFailed(e.ToString());
                return false;
            }

            Logger.LogOperationDone();
            return true;
        }

        public abstract List<GameWorldModel> GetGameWorlds();
        public abstract AccountModel GetAccountInformation(string accountName, string password);
        public abstract AccountModel GetAccountInformationWithoutPassword(string accountName);
        public abstract CharacterModel GetCharacterInformation(string characterName);
    }
}
