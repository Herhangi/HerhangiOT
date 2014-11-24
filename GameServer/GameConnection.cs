using System;
using System.Collections.Generic;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Networking;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer
{
    class GameConnection : Connection
    {
        private static Random _rng = new Random();

        private bool _didReceiveFirstMessage;
        private uint _challengeTimestamp;
        private byte _challengeRandom;
        
        private static Dictionary<ClientPacketType, Action<GameConnection>> _packetHandlers = new Dictionary<ClientPacketType, Action<GameConnection>>()
        {
            {ClientPacketType.GameServerRequest, HandleLoginPacket}
        };

        public override void HandleFirstConnection(IAsyncResult ar)
        {
            base.HandleFirstConnection(ar);

            SendWelcomeSignal();
        }

        private void SendWelcomeSignal()
        {
            OutputMessage message = new OutputMessage();
            message.FreeMessage();

            message.SkipBytes(sizeof(uint));

            //Message Length
            message.AddUInt16(0x0006);
            message.AddByte((byte)ServerPacketType.WelcomeToGameServer);

            _challengeTimestamp = (uint)Environment.TickCount;
            message.AddUInt32(_challengeTimestamp);

            _challengeRandom = (byte)_rng.Next(0, 256);
            message.AddByte(_challengeRandom);

            // Write Adler Checksum
            message.SkipBytes(-12);
            message.AddUInt32(Tools.AdlerChecksum(message.Buffer, 12, message.Length - 4));

            Send(message);
        }


        protected override void ProcessMessage()
        {
            if (_didReceiveFirstMessage)
            {
                HandleLoginPacket(this);
            }
            else
            {
                ClientPacketType requestType = (ClientPacketType)InMessage.GetByte();

                if(_packetHandlers.ContainsKey(requestType))
                    _packetHandlers[requestType].Invoke(this);
            }
        }

        private static void HandleLoginPacket(GameConnection conn)
        {
            conn._didReceiveFirstMessage = true;
            ClientPacketType requestType = (ClientPacketType)conn.InMessage.GetByte();

            conn.InMessage.GetByte(); //Protocol Id
            conn.InMessage.GetUInt16(); //Client OS
            ushort version = conn.InMessage.GetUInt16(); //Client Version

            conn.InMessage.SkipBytes(5);  // U32 clientVersion, U8 clientType
            
            if (!conn.InMessage.RsaDecrypt())
            {
                Logger.Log(LogLevels.Information, "GameConnection: Message could not be decrypted");
                conn.Disconnect();
                return;
            }

            conn.XteaKey[0] = conn.InMessage.GetUInt32();
            conn.XteaKey[1] = conn.InMessage.GetUInt32();
            conn.XteaKey[2] = conn.InMessage.GetUInt32();
            conn.XteaKey[3] = conn.InMessage.GetUInt32();
            conn.IsEncryptionEnabled = true;

            conn.InMessage.SkipBytes(1); //GameMaster Flag
            string accountName = conn.InMessage.GetString();
            string characterName = conn.InMessage.GetString();
            string password = conn.InMessage.GetString();

            Console.Write("");
        }
    }
}
