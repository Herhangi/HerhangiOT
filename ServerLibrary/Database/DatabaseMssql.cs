using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using HerhangiOT.ServerLibrary.Model;

namespace HerhangiOT.ServerLibrary.Database
{
    class DatabaseMssql : Database
    {
        private SqlConnection _connection; 

        public DatabaseMssql()
        {
            try
            {
                string a = ConfigManager.Instance[ConfigStr.DATABASE_MSSQL_CONNECTION_STRING];
                _connection = new SqlConnection(ConfigManager.Instance[ConfigStr.DATABASE_MSSQL_CONNECTION_STRING]);
                _connection.Open();
            }
            catch (Exception e)
            {
                
                throw;
            }
        }

        public override List<GameWorld> GetGameWorlds()
        {
            SqlCommand command = new SqlCommand("GetGameWorlds", _connection);
            command.CommandType = CommandType.StoredProcedure;

            SqlDataReader reader = command.ExecuteReader();

            List<GameWorld> result = new List<GameWorld>();
            
            while (reader.Read())
            {
                GameWorld world = new GameWorld();
                world.GameWorldId = reader.GetInt32(0);
                world.GameWorldName = reader.GetString(1);
                world.GameWorldIP = reader.GetString(2);
                world.GameWorldPort = (ushort)reader.GetInt32(3);
            }
            return result;
        }

        public override Account GetAccountInformation(string username, string password)
        {
            throw new NotImplementedException();
        }
    }
}
