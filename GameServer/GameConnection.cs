using System;
using System.Collections.Generic;
using HerhangiOT.GameServerLibrary;
using HerhangiOT.GameServerLibrary.Model;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database.Model;
using HerhangiOT.ServerLibrary.Enums;
using HerhangiOT.ServerLibrary.Networking;
using HerhangiOT.ServerLibrary.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer
{
    public class GameConnection : Connection
    {
        private static readonly Random Rng = new Random();

        public uint AccountId { get; private set; }
        public string AccountName { get; private set; }
        public string PlayerName { get; private set; }
        public OperatingSystems ClientOs { get; private set; }
        public Player PlayerData { get; private set; }

        private bool _didReceiveFirstMessage;
        private uint _challengeTimestamp;
        private byte _challengeRandom;
        
        private static readonly Dictionary<ClientPacketType, Action<GameConnection>> PacketHandlers = new Dictionary<ClientPacketType, Action<GameConnection>>
        {
            {ClientPacketType.GameServerRequest, HandleLoginPacket}
        };

        public override void HandleFirstConnection(IAsyncResult ar)
        {
            base.HandleFirstConnection(ar);

            SendWelcomeMessage();
        }

        private void SendWelcomeMessage()
        {
            OutputMessage message = new OutputMessage();
            message.FreeMessage();

            message.SkipBytes(sizeof(uint));

            //Message Length
            message.AddUInt16(0x0006);
            message.AddByte((byte)ServerPacketType.WelcomeToGameServer);

            _challengeTimestamp = (uint)Environment.TickCount;
            message.AddUInt32(_challengeTimestamp);

            _challengeRandom = (byte)Rng.Next(0, 256);
            message.AddByte(_challengeRandom);

            // Write Adler Checksum
            message.SkipBytes(-12);
            message.AddUInt32(Tools.AdlerChecksum(message.Buffer, 12, message.Length - 4));

            Send(message);
        }

        protected override void ProcessMessage()
        {
            if (!_didReceiveFirstMessage)
            {
                HandleLoginPacket(this);
            }
            else
            {
                ClientPacketType requestType = (ClientPacketType)InMessage.GetByte();

                if(PacketHandlers.ContainsKey(requestType))
                    PacketHandlers[requestType].Invoke(this);
            }
        }

        private static void HandleLoginPacket(GameConnection conn)
        {
            conn._didReceiveFirstMessage = true;
            conn.InMessage.GetByte();
            //conn.InMessage.GetByte(); //Protocol Id
            conn.ClientOs = (OperatingSystems)conn.InMessage.GetUInt16(); //Client OS
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
            conn.AccountName = conn.InMessage.GetString();
            string characterName = conn.InMessage.GetString();
            byte[] password = conn.InMessage.GetBytes(conn.InMessage.GetUInt16());

            uint challengeTimestamp = conn.InMessage.GetUInt32();
            byte challengeRandom = conn.InMessage.GetByte();

            if (challengeRandom != conn._challengeRandom || challengeTimestamp != conn._challengeTimestamp)
            {
                conn.Disconnect();
                return;
            }
            
	        if (version < Constants.CLIENT_VERSION_MIN || (version > Constants.CLIENT_VERSION_MAX))
            {
		        conn.DispatchDisconnect("Only clients with protocol " + Constants.CLIENT_VERSION_STR + " allowed!");
		        return;
	        }

            if (conn.ClientOs >= OperatingSystems.CLIENTOS_OTCLIENT_LINUX)
            {
                conn.DispatchDisconnect("Custom clients are not supported, yet!");
                return;
            }

            if (string.IsNullOrEmpty(conn.AccountName))
            {
                conn.DispatchDisconnect("You must enter your account name.");
                return;
            }

            if (Game.Instance.GameState == GameStates.Startup)
            {
                conn.DispatchDisconnect("Gameworld is starting up. Please wait.");
                return;
            }

            if (Game.Instance.GameState == GameStates.Maintain)
            {
                conn.DispatchDisconnect("Gameworld is under maintenance. Please re-connect in a while.");
                return;
            }

            // TODO: CHECK FOR BAN

            SecretCommunication.OnCharacterAuthenticationResultArrived += conn.OnCharacterAuthenticationResultArrived;
            SecretCommunication.CheckCharacterAuthenticity(conn.AccountName, characterName, password);
        }

        private void OnCharacterAuthenticationResultArrived(SecretNetworkResponseType response, string accountName, string playerName, uint accountId)
        {
            if (!AccountName.Equals(accountName, StringComparison.InvariantCulture)) return;
            
            SecretCommunication.OnCharacterAuthenticationResultArrived -= OnCharacterAuthenticationResultArrived;

            if (response != SecretNetworkResponseType.Success)
            {
                string disconnectMessage;

                switch (response)
                {
                    case SecretNetworkResponseType.InvalidAccountData:
                        disconnectMessage = "Your account information has been changed! Please log in again...";
                        break;
                    case SecretNetworkResponseType.AnotherCharacterOnline:
                        disconnectMessage = "You are online with another playerName!";
                        break;
                    case SecretNetworkResponseType.CharacterCouldNotBeFound:
                        disconnectMessage = "Character could not be found!";
                        break;
                    default:
                        disconnectMessage = "An improbable error occured! Please log in again. Contact server admins if problem persists!";
                        break;
                }

                DispatchDisconnect(disconnectMessage);
            }
            else
            {
                //LOGIN
                AccountId = accountId;
                PlayerName = playerName;
                Login();
            }
        }

        public void Login()
        {
            if (!Game.OnlinePlayers.ContainsKey(PlayerName))
            {
                PlayerData = new Player(this, AccountName, PlayerName);
                PlayerData.PreloadPlayer();

                //CHECK NAMELOCK

                //TODO: GROUP FLAGS
                if (Game.Instance.GameState == GameStates.Closing)
                {
                    DispatchDisconnect("The game is just going down.\nPlease try again later.");
                    return;
                }

                //TODO: GROUP FLAGS
                if (Game.Instance.GameState == GameStates.Closed)
                {
                    DispatchDisconnect("Server is currently closed.\nPlease try again later.");
                    return;
                }

                //TODO: WAITING LIST

                if (!PlayerData.LoadPlayer())
                {
                    DispatchDisconnect("Your character could not be loaded.");
                    return;
                }

                //LOAD PLAYER

                //PLACE CHARACTER

                //LAST LOGIN OPERATIONS
            }
            else
            {
                PlayerData = Game.OnlinePlayers[PlayerName];

                if (ConfigManager.Instance[ConfigBool.ReplaceKickOnLogin])
                {
                    if (PlayerData.Connection != null)
                    {
                        PlayerData.Connection.Disconnect();
                        DispatcherManager.GameDispatcher.AddTask(new Task(Connect));
                        return;
                    }

                    Connect();
                }
                else
                {
                    DispatchDisconnect("You are already logged in!");
                }
            }
        }

        private void Connect()
        {
            
        }

        public void SendStats()
        {
            OutputMessage message = OutputMessagePool.GetOutputMessage();
            message.AddByte((byte)ServerPacketType.PlayerStatus);

            CharacterModel character = PlayerData.CharacterData;
            message.AddUInt16(character.Health);
            message.AddUInt16(character.HealthMax);

            message.AddUInt32(PlayerData.FreeCapacity);
            message.AddUInt32(character.Capacity);

            message.AddUInt64(character.Experience);

            message.AddUInt16(character.Level);
        }
    }
}
