using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HerhangiOT.ServerLibrary.Database.Model;

namespace HerhangiOT.ServerLibrary.Database
{
    public class DatabaseMssql : Database
    {
        private SqlConnection _connection; 

        public DatabaseMssql()
        {
            try
            {
                string a = ConfigManager.Instance[ConfigStr.DatabaseMssqlConnectionString];
                _connection = new SqlConnection(ConfigManager.Instance[ConfigStr.DatabaseMssqlConnectionString]);
                _connection.Open();
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        public override List<GameWorldModel> GetGameWorlds()
        {
            SqlCommand command = new SqlCommand("GetGameWorlds", _connection);
            command.CommandType = CommandType.StoredProcedure;

            SqlDataReader reader = command.ExecuteReader();

            List<GameWorldModel> result = new List<GameWorldModel>();
            
            while (reader.Read())
            {
                GameWorldModel world = new GameWorldModel();
                world.GameWorldId = (byte)reader.GetInt32(0);
                world.GameWorldName = reader.GetString(1);
                world.GameWorldIP = reader.GetString(2);
                world.GameWorldPort = (ushort)reader.GetInt32(3);
            }
            return result;
        }

        public override AccountModel GetAccountInformation(string accountName, string password)
        {
            throw new NotImplementedException();
        }

        public override AccountModel GetAccountInformationWithoutPassword(string accountName)
        {
            throw new NotImplementedException();
        }

        public override CharacterModel GetCharacterInformation(string characterName)
        {
            throw new NotImplementedException();
        }
    }
}
