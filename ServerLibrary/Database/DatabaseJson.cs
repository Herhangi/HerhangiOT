using System.Collections.Generic;
using System.IO;
using HerhangiOT.ServerLibrary.Model;
using Newtonsoft.Json;

namespace HerhangiOT.ServerLibrary.Database
{
    class DatabaseJson : Database
    {
        public DatabaseJson()
        {
            
        }

        public override List<GameWorld> GetGameWorlds()
        {
            string json = File.ReadAllText("Data/gameworlds.json");
            List<GameWorld> result = JsonConvert.DeserializeObject<List<GameWorld>>(json);

            return result;
        }

        public override Account GetAccountInformation(string username, string password)
        {
            string filePath = ConfigManager.Instance[ConfigStr.DATABASE_JSON_ACCOUNT_PATH] + username + ".json"; 
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            Account result = JsonConvert.DeserializeObject<Account>(json);

            if (!result.Password.Equals(password))
                return null;

            return result;
        }
    }
}
