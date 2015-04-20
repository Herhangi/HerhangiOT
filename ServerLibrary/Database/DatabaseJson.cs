using System.Collections.Generic;
using System.IO;
using HerhangiOT.ServerLibrary.Database.Model;
using Newtonsoft.Json;

namespace HerhangiOT.ServerLibrary.Database
{
    class DatabaseJson : Database
    {
        public DatabaseJson()
        { }

        public override List<GameWorldModel> GetGameWorlds()
        {
            string json = File.ReadAllText("Data/gameworlds.json");
            List<GameWorldModel> result = JsonConvert.DeserializeObject<List<GameWorldModel>>(json);

            return result;
        }

        public override AccountModel GetAccountInformation(string accountName, string password)
        {
            string filePath = ConfigManager.Instance[ConfigStr.DatabaseJsonAccountPath] + accountName + ".json";
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            AccountModel result = JsonConvert.DeserializeObject<AccountModel>(json);

            if (!result.Password.Equals(password))
                return null;

            return result;
        }

        public override AccountModel GetAccountInformationWithoutPassword(string accountName)
        {
            string filePath = ConfigManager.Instance[ConfigStr.DatabaseJsonAccountPath] + accountName + ".json";
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<AccountModel>(json);
        }

        public override CharacterModel GetCharacterInformation(string characterName)
        {
            string filePath = ConfigManager.Instance[ConfigStr.DatabaseJsonCharacterPath] + characterName + ".json";
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<CharacterModel>(json);
        }
    }
}
