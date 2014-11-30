using System;
using System.Diagnostics;
using System.Threading;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Model;
using HerhangiOT.ServerLibrary.Networking;
using HerhangiOT.ServerLibrary.Threading;

namespace HerhangiOT.LoginServer
{
    public class LoginConnection : Connection
    {
        protected override void ProcessMessage()
        {
            InMessage.GetByte(); //Protocol Id
            InMessage.GetUInt16(); //Client OS
            ushort version = InMessage.GetUInt16(); //Client Version

            if (version < Constants.CLIENT_VERSION_MIN || version > Constants.CLIENT_VERSION_MAX)
            {
                Disconnect("Only clients with protocol " + Constants.CLIENT_VERSION_STR + " allowed!");
                return;
            }

            //This is 10.41 server, only handling 10.41 client bytes 
            InMessage.SkipBytes(17);
            /*
             * Skipped bytes:
             * 4 bytes: protocolVersion (only 971+)
             * 12 bytes: dat, spr, pic signatures (4 bytes each)
             * 1 byte: 0 (only 971+)
             */
            
            if (!InMessage.RsaDecrypt())
            {
                Logger.Log(LogLevels.Information, "LoginConnection: Message could not be decrypted");
                Disconnect();
                return;
            }

            XteaKey[0] = InMessage.GetUInt32();
            XteaKey[1] = InMessage.GetUInt32();
            XteaKey[2] = InMessage.GetUInt32();
            XteaKey[3] = InMessage.GetUInt32();
            IsEncryptionEnabled = true;
            
            string username = InMessage.GetString();
            byte[] password = InMessage.GetBytes(InMessage.GetUInt16());

            DispatcherManager.DatabaseDispatcher.AddTask(new Task(() => HandleLoginPacket(username, password)));
        }

        private void HandleLoginPacket(string username, byte[] password)
        {
            if (string.IsNullOrEmpty(username))
            {
                Disconnect("Invalid account name.");
                return;
            }

            byte[] hash = LoginServer.PasswordHasher.ComputeHash(password);
            string hashedPassword = string.Empty;
            foreach (byte b in hash)
                hashedPassword += b.ToString("x2");

            Account acc = Database.Instance.GetAccountInformation(username, hashedPassword);

            if (acc == null)
            {
                Disconnect("Account name or password is not correct.");
                return;
            }

            OutputMessage message = new OutputMessage();
            message.AddByte((byte)ServerPacketType.MOTD);
            message.AddString(LoginServer.MOTD);

            message.AddByte((byte)ServerPacketType.CharacterList);
            message.AddByte((byte)LoginServer.GameWorlds.Count);
            foreach (GameWorld world in LoginServer.GameWorlds)
            {
                message.AddByte((byte)world.GameWorldId);
                message.AddString(world.GameWorldName);
                message.AddString(world.GameWorldIP);
                message.AddUInt16(world.GameWorldPort);
                message.AddByte(0);
            }

            message.AddByte((byte)acc.Characters.Count);
            foreach (AccountCharacter character in acc.Characters)
            {
                message.AddByte((byte)character.ServerId);
                message.AddString(character.CharacterName);
            }

            if (!acc.PremiumUntil.HasValue)
                message.AddUInt16(0xFFFF);
            else
            {
                TimeSpan premiumLeft = acc.PremiumUntil.Value - DateTime.Now;
                message.AddUInt16((ushort)premiumLeft.TotalDays);
            }

            Send(message);
            Disconnect();
        }
    }
}
