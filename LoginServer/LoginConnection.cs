using System;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database.Model;
using HerhangiOT.ServerLibrary.Networking;
using HerhangiOT.ServerLibrary.Threading;

namespace HerhangiOT.LoginServer
{
    public class LoginConnection : Connection
    {
        protected ushort Version;

        protected override void ProcessFirstMessage(bool isChecksummed)
        {
            if (!isChecksummed)
            {
                ProcessStatusMessage();
                return;
            }

            InMessage.GetByte(); //Protocol Id
            InMessage.GetUInt16(); //Client OS
            Version = InMessage.GetUInt16(); //Client Version

            if (Version < Constants.ClientVersionMin || Version > Constants.ClientVersionMax)
            {
                Disconnect("Only clients with protocol " + Constants.ClientVersionStr + " allowed!", Version);
                return;
            }

            //This is 10.76 server, only handling 10.76 client bytes
            InMessage.SkipBytes(17);
            /*
             * Skipped bytes:
             * 4 bytes: protocolVersion
             * 12 bytes: dat, spr, pic signatures (4 bytes each)
             * 1 byte: 0
             * 
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
            
            string accountName = InMessage.GetString();
            byte[] password = InMessage.GetBytes(InMessage.GetUInt16());

            DispatcherManager.DatabaseDispatcher.AddTask(Task.CreateTask(() => HandleLoginPacket(accountName, password)));
        }

        private void ProcessStatusMessage()
        {
            Logger.Log(LogLevels.Information, "Status message handling is not implemented yet!");
        }

        private void HandleLoginPacket(string accountName, byte[] password)
        {
            //TODO: Check IP Ban

            if (string.IsNullOrEmpty(accountName))
            {
                Disconnect("Invalid account name.", Version);
                return;
            }

            byte[] hash = LoginServer.PasswordHasher.ComputeHash(password);
            string hashedPassword = string.Empty;
            foreach (byte b in hash)
                hashedPassword += b.ToString("x2");

            string sessionKey;
            AccountModel acc = LoginServerData.RetrieveAccountData(accountName, hashedPassword, out sessionKey);

            if (acc == null)
            {
                Disconnect("Account name or password is not correct.", Version);
                return;
            }

            OutputMessage message = OutputMessagePool.GetOutputMessage(this, false);
            message.AddByte((byte)ServerPacketType.MOTD);
            message.AddString(LoginServer.MOTD);

            message.AddByte((byte)ServerPacketType.SessionKey);
            message.AddString(sessionKey);

            message.AddByte((byte)ServerPacketType.CharacterList);
            message.AddByte((byte)LoginServer.GameWorlds.Count);
            foreach (GameWorldModel world in LoginServer.GameWorlds.Values)
            {
                message.AddByte(world.GameWorldId);
                message.AddString(world.GameWorldName);
                message.AddString(world.GameWorldIP);
                message.AddUInt16(world.GameWorldPort);
                message.AddByte(0);
            }

            message.AddByte((byte)acc.Characters.Count);
            foreach (AccountCharacterModel character in acc.Characters)
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

            message.DisconnectAfterMessage = true;
            OutputMessagePool.AddToQueue(message);
        }
    }
}
