using System;
using System.Collections.Generic;
using HerhangiOT.ServerLibrary.Model;

namespace HerhangiOT.ServerLibrary.Database
{
    public abstract class Database
    {
        private static bool _isInitialized;

        public static Database Instance { get; private set; }

        public static bool Initialize()
        {
            if (_isInitialized)
                return false;

            Logger.LogOperationStart("Initializing database connection");

            try
            {
                switch (ConfigManager.Instance[ConfigStr.DATABASE_TYPE])
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

        public abstract List<GameWorld> GetGameWorlds();
        public abstract Account GetAccountInformation(string username, string password);
    }
}
