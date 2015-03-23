using System.Linq;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Model;
using HerhangiOT.ServerLibrary.Threading;

namespace HerhangiOT.GameServer
{
    public class DatabaseOperations
    {
        public static void CheckCharacterAuthenticity(Connection conn, string username, string character, byte[] password)
        {
            byte[] hash = LoginServer.LoginServer.PasswordHasher.ComputeHash(password);
            string hashedPassword = string.Empty;
            foreach (byte b in hash)
                hashedPassword += b.ToString("x2");

            Account acc = Database.Instance.GetAccountInformation(username, hashedPassword);
            if (acc == null)
            {
                conn.DispatchDisconnect("Account name or password is not correct.");
                return;
            }

            bool doesCharacterExist = acc.Characters.Any(t => t.CharacterName.Equals(character));
            if (doesCharacterExist)
            {
                DispatcherManager.DatabaseDispatcher.AddTask(new Task(
                    () => RetrieveCharacterData(conn, character)
                ));
            }
            else
            {
                conn.DispatchDisconnect("Character not found.");
            }
        }

        public static void RetrieveCharacterData(Connection conn, string character)
        {
            
        }
    }
}
