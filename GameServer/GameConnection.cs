using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.GameServer.Model.Items;
using HerhangiOT.GameServer.Scriptability;
using HerhangiOT.GameServer.Utility;
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
        public string SessionKey { get; private set; }
        public string AccountName { get; private set; }
        public string PlayerName { get; private set; }
        public Player PlayerData { get; private set; }
        public OperatingSystems ClientOs { get; private set; }
        public ClientPacketType LatestRequest { get; private set; }

        private bool AcceptPackets { get; set; }
        private uint EventConnect { get; set; }

        private readonly HashSet<uint> _knownCreatureSet = new HashSet<uint>(); 

        private uint _challengeTimestamp;
        private byte _challengeRandom;
        
        private static readonly Dictionary<ClientPacketType, Action<GameConnection>> PacketHandlers = new Dictionary<ClientPacketType, Action<GameConnection>>
        {
            { ClientPacketType.Disconnect, ProcessDisconnectPacket },
            { ClientPacketType.PingBack, ProcessPingBackPacket },
            { ClientPacketType.FightModes, ProcessFightModesPacket },
            { ClientPacketType.MoveNorth, ProcessPlayerMovePacket },
            { ClientPacketType.MoveEast, ProcessPlayerMovePacket },
            { ClientPacketType.MoveSouth, ProcessPlayerMovePacket },
            { ClientPacketType.MoveWest, ProcessPlayerMovePacket },
            { ClientPacketType.MoveNorthEast, ProcessPlayerMovePacket },
            { ClientPacketType.MoveNorthWest, ProcessPlayerMovePacket },
            { ClientPacketType.MoveSouthEast, ProcessPlayerMovePacket },
            { ClientPacketType.MoveSouthWest, ProcessPlayerMovePacket },
            { ClientPacketType.PlayerSpeech, ProcessPlayerSpeechPacket },
            { ClientPacketType.TurnEast, ProcessPlayerTurnPacket },
            { ClientPacketType.TurnNorth, ProcessPlayerTurnPacket },
            { ClientPacketType.TurnSouth, ProcessPlayerTurnPacket },
            { ClientPacketType.TurnWest, ProcessPlayerTurnPacket },
            { ClientPacketType.Logout, ProcessLogoutPacket },
            { ClientPacketType.AutoWalk, ProcessAutoWalkPacket },
            { ClientPacketType.StopAutoWalk, ProcessStopAutoWalkPacket },
            { ClientPacketType.ChannelList, ProcessChannelListPacket },
            { ClientPacketType.ChannelOpen, ProcessChannelOpenPacket },
            { ClientPacketType.ChannelClose, ProcessChannelClosePacket },
            { ClientPacketType.PrivateChannelOpen, ProcessPrivateChannelOpenPacket },
            { ClientPacketType.PrivateChannelCreate, ProcessPrivateChannelCreatePacket },
            { ClientPacketType.PrivateChannelInvite, ProcessPrivateChannelInvitePacket },
            { ClientPacketType.PrivateChannelExclude, ProcessPrivateChannelExcludePacket },
        };

        #region Connection Overrides

        public override void Disconnect()
        {
            if (PlayerData != null)
                PlayerData.Connection = null;
            base.Disconnect();
        }
        public override void HandleFirstConnection(IAsyncResult ar)
        {
            try
            {
                base.HandleFirstConnection(ar);

                SendWelcomeMessage();
            }
            catch (Exception)
            {
            }
        }
        protected override void ParseHeader(IAsyncResult ar)
        {
            base.ParseHeader(ar);

            if(Stream.CanRead)
                Stream.BeginRead(InMessage.Buffer, 0, 2, ParseHeader, null);
        }
        protected override void ProcessFirstMessage(bool isChecksummed)
        {
            if (Game.GameState == GameStates.Shutdown)
            {
                Disconnect();
                return;
            }

            InMessage.GetByte(); //Protocol Id
            ClientOs = (OperatingSystems)InMessage.GetUInt16(); //Client OS
            ushort version = InMessage.GetUInt16(); //Client Version

            InMessage.SkipBytes(7);  // U32 clientVersion, U8 clientType, U16 dat revision

            if (!InMessage.RsaDecrypt())
            {
                Logger.Log(LogLevels.Information, "GameConnection: Message could not be decrypted");
                Disconnect();
                return;
            }

            XteaKey[0] = InMessage.GetUInt32();
            XteaKey[1] = InMessage.GetUInt32();
            XteaKey[2] = InMessage.GetUInt32();
            XteaKey[3] = InMessage.GetUInt32();
            IsEncryptionEnabled = true;

            InMessage.SkipBytes(1); //GameMaster Flag

            SessionKey = InMessage.GetString();

            string characterName = InMessage.GetString();

            uint challengeTimestamp = InMessage.GetUInt32();
            byte challengeRandom = InMessage.GetByte();

            if (challengeRandom != _challengeRandom || challengeTimestamp != _challengeTimestamp)
            {
                Disconnect();
                return;
            }

            if (version < Constants.ClientVersionMin || (version > Constants.ClientVersionMax))
            {
                base.DispatchDisconnect("Only clients with protocol " + Constants.ClientVersionStr + " allowed!");
                return;
            }

            if (ClientOs >= OperatingSystems.OtClientLinux)
            {
                base.DispatchDisconnect("Custom clients are not supported, yet!");
                return;
            }

            if (Game.GameState == GameStates.Startup)
            {
                base.DispatchDisconnect("Gameworld is starting up. Please wait.");
                return;
            }

            if (Game.GameState == GameStates.Maintain)
            {
                base.DispatchDisconnect("Gameworld is under maintenance. Please re-connect in a while.");
                return;
            }

            // TODO: CHECK FOR BAN

            SecretCommunication.OnCharacterAuthenticationResultArrived += OnCharacterAuthenticationResultArrived;
            SecretCommunication.CheckCharacterAuthenticity(SessionKey, characterName);
        }
        protected override void ProcessMessage()
        {
            if (!AcceptPackets) return;

            LatestRequest = (ClientPacketType)InMessage.GetByte();

            Logger.Log(LogLevels.Development, "Player({0}) sent request({1})!", PlayerName, LatestRequest);

            if (PacketHandlers.ContainsKey(LatestRequest))
                PacketHandlers[LatestRequest].Invoke(this);
            else
            {
                Logger.Log(LogLevels.Debug, "Unknown message arrived: " + LatestRequest);
            }
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void DispatchDisconnect(string message)
        {
            DispatcherManager.GameDispatcher.AddTask(() => Disconnect(message));
        }

        protected void Disconnect(string message)
        {
            OutputMessage msg = OutputMessagePool.GetOutputMessage(this, false);
            msg.AddByte((byte)ServerPacketType.DisconnectGame);
            msg.AddString(message);
            OutputMessagePool.SendImmediately(msg);
        }

        #region Login Process
        private void OnCharacterAuthenticationResultArrived(SecretNetworkResponseType response, string sessionKey, string accountName, string playerName, uint accountId)
        {
            if (!SessionKey.Equals(sessionKey, StringComparison.InvariantCulture)) return;
            
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
                        disconnectMessage = "You are online with another character!";
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
                AccountName = accountName;
                DispatcherManager.GameDispatcher.AddTask(Login);
            }
        }
        public void Login()
        {
            PlayerData = Game.GetPlayerByName(PlayerName);
            if (PlayerData == null)
            {
                PlayerData = new Player(this, AccountName, PlayerName);

                if(!PlayerData.PreloadPlayer())
                {
                    DispatchDisconnect("Your character could not be loaded.");
                    return;
                }
                //CHECK NAMELOCK

                if (Game.GameState == GameStates.Closing && !PlayerData.HasFlag(PlayerFlags.CanAlwaysLogin))
                {
                    base.DispatchDisconnect("The game is just going down.\nPlease try again later.");
                    return;
                }

                if (Game.GameState == GameStates.Closed && !PlayerData.HasFlag(PlayerFlags.CanAlwaysLogin))
                {
                    base.DispatchDisconnect("Server is currently closed.\nPlease try again later.");
                    return;
                }

                //TODO: WAITING LIST

                if (!PlayerData.LoadPlayer())
                {
                    base.DispatchDisconnect("Your character could not be loaded.");
                    return;
                }

                if (!Game.PlaceCreature(PlayerData, PlayerData.LoginPosition))
                {
                    if (!Game.PlaceCreature(PlayerData, PlayerData.LoginPosition, false, true))
                    {
                        base.DispatchDisconnect("Temple position is wrong. Contact the administrator!");
                        return;
                    }
                }
                
                //TODO: LAST LOGIN OPERATIONS
                AcceptPackets = true;
            }
            else
            {
                if (ConfigManager.Instance[ConfigBool.ReplaceKickOnLogin] && EventConnect == 0)
                {
                    if (PlayerData.Connection != null)
                    {
                        PlayerData.IsConnecting = true;
                        PlayerData.Connection.Disconnect();
                        EventConnect = DispatcherManager.Scheduler.AddEvent(SchedulerTask.CreateSchedulerTask(1000, Connect));
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
	        EventConnect = 0;

            if (PlayerData.Connection != null)
            {
                DispatchDisconnect("You are already logged in.");
		        return;
	        }

	        PlayerData.IncrementReferenceCounter();

	        //g_chat->removeUserFromAllChannels(*player); //TODO: Chat
	        PlayerData.ModalWindows.Clear();
	        PlayerData.IsConnecting = false;

	        PlayerData.Connection = this;
	        SendAddCreature(PlayerData, PlayerData.Position, 0, false);
            //TODO: Last Login Operations
            //player->lastIP = player->getIP();
            //player->lastLoginSaved = std::max<time_t>(time(nullptr), player->lastLoginSaved + 1);
	        AcceptPackets = true;
        }
        #endregion

        #region Process Packets
        private static void ProcessDisconnectPacket(GameConnection conn)
        {
            if (conn.PlayerData == null || conn.PlayerData.Health <= 0) //TODO: Player Removed
            {
                conn.Disconnect();
            }
        }
        private static void ProcessPingBackPacket(GameConnection conn)
        {
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerReceivePingBack(conn.PlayerData.Id));
        }
        private static void ProcessFightModesPacket(GameConnection conn)
        {
            NetworkMessage msg = conn.InMessage;
            FightModes fightMode = (FightModes) msg.GetByte(); // 1 - offensive, 2 - balanced, 3 - defensive
            ChaseModes chaseMode = (ChaseModes) msg.GetByte(); // 0 - stand while fightning, 1 - chase opponent
	        SecureModes secureMode = (SecureModes) msg.GetByte(); // 0 - can't attack unmarked, 1 - can attack unmarked
	        // uint8_t rawPvpMode = msg.getByte(); // pvp mode introduced in 10.0

            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerSetFightModes(conn.PlayerData.Id, fightMode, chaseMode, secureMode));
        }
        private static void ProcessPlayerMovePacket(GameConnection conn)
        {
            Directions direction = Directions.None;
            switch (conn.LatestRequest)
            {
                case ClientPacketType.MoveNorth:
                    direction = Directions.North;
                    break;
                case ClientPacketType.MoveEast:
                    direction = Directions.East;
                    break;
                case ClientPacketType.MoveSouth:
                    direction = Directions.South;
                    break;
                case ClientPacketType.MoveWest:
                    direction = Directions.West;
                    break;
                case ClientPacketType.MoveNorthWest:
                    direction = Directions.NorthWest;
                    break;
                case ClientPacketType.MoveNorthEast:
                    direction = Directions.NorthEast;
                    break;
                case ClientPacketType.MoveSouthWest:
                    direction = Directions.SouthWest;
                    break;
                case ClientPacketType.MoveSouthEast:
                    direction = Directions.SouthEast;
                    break;
            }

            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerMove(conn.PlayerData.Id, direction));
        }
        private static void ProcessPlayerSpeechPacket(GameConnection conn)
        {
            string receiver = string.Empty;
	        ushort channelId;
            NetworkMessage msg = conn.InMessage;

	        SpeakTypes type = (SpeakTypes) msg.GetByte();
	        switch (type)
            {
		        case SpeakTypes.PrivateTo:
		        case SpeakTypes.PrivateRedTo:
			        receiver = msg.GetString();
			        channelId = 0;
			        break;

		        case SpeakTypes.ChannelY:
		        case SpeakTypes.ChannelR1:
			        channelId = msg.GetUInt16();
			        break;

		        default:
			        channelId = 0;
			        break;
	        }

	        string text = msg.GetString();
	        if (text.Length > 255)
		        return;

            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerSay(conn.PlayerData.Id, channelId, type, receiver, text));
        }
        private static void ProcessPlayerTurnPacket(GameConnection conn)
        {
            Directions direction = Directions.None;
            switch (conn.LatestRequest)
            {
                case ClientPacketType.TurnEast:
                    direction = Directions.East;
                    break;
                case ClientPacketType.TurnNorth:
                    direction = Directions.North;
                    break;
                case ClientPacketType.TurnSouth:
                    direction = Directions.South;
                    break;
                case ClientPacketType.TurnWest:
                    direction = Directions.West;
                    break;
            }

            DispatcherManager.GameDispatcher.AddTask(Task.CreateTask(Constants.DispatcherTaskExpiration, () => Game.PlayerTurn(conn.PlayerData.Id, direction)));
        }
        private static void ProcessAutoWalkPacket(GameConnection conn)
        {
            NetworkMessage msg = conn.InMessage;

	        byte numdirs = msg.GetByte();
	        if (numdirs == 0 || (msg.Position + numdirs) != (msg.Length))
		        return;

	        //msg.SkipBytes(numdirs);

            Queue<Directions> path = new Queue<Directions>();
	        for (byte i = 0; i < numdirs; ++i)
            {
		        //byte rawdir = msg.GetPreviousByte();
		        byte rawdir = msg.GetByte();
		        switch (rawdir)
                {
			        case 1: path.Enqueue(Directions.East); break;
			        case 2: path.Enqueue(Directions.NorthEast); break;
			        case 3: path.Enqueue(Directions.North); break;
			        case 4: path.Enqueue(Directions.NorthWest); break;
			        case 5: path.Enqueue(Directions.West); break;
			        case 6: path.Enqueue(Directions.SouthWest); break;
			        case 7: path.Enqueue(Directions.South); break;
			        case 8: path.Enqueue(Directions.SouthEast); break;
		        }
	        }

            if (path.Count == 0)
                return;

            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerAutoWalk(conn.PlayerData.Id, path));
        }
        private static void ProcessStopAutoWalkPacket(GameConnection conn)
        {
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerStopAutoWalk(conn.PlayerData.Id));
        }
        private static void ProcessLogoutPacket(GameConnection conn)
        {
            DispatcherManager.GameDispatcher.AddTask(() => Logout(conn.PlayerData, true, false));
        }
        private static void ProcessChannelListPacket(GameConnection conn)
        {
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerRequestChannels(conn.PlayerData.Id));
        }
        private static void ProcessChannelOpenPacket(GameConnection conn)
        {
            ushort channelId = conn.InMessage.GetUInt16();
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerChannelOpen(conn.PlayerData.Id, channelId));
        }
        private static void ProcessChannelClosePacket(GameConnection conn)
        {
            ushort channelId = conn.InMessage.GetUInt16();
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerChannelClose(conn.PlayerData.Id, channelId));
        }
        private static void ProcessPrivateChannelOpenPacket(GameConnection conn)
        {
            string receiver = conn.InMessage.GetString();
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerPrivateChannelOpen(conn.PlayerData.Id, receiver));
        }
        private static void ProcessPrivateChannelCreatePacket(GameConnection conn)
        {
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerPrivateChannelCreate(conn.PlayerData.Id));
        }
        private static void ProcessPrivateChannelInvitePacket(GameConnection conn)
        {
            string name = conn.InMessage.GetString();
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerPrivateChannelInvite(conn.PlayerData.Id, name));
        }
        private static void ProcessPrivateChannelExcludePacket(GameConnection conn)
        {
            string name = conn.InMessage.GetString();
            DispatcherManager.GameDispatcher.AddTask(() => Game.PlayerPrivateChannelExclude(conn.PlayerData.Id, name));
        }

        #endregion

        #region Send Messages
        public void SendWelcomeMessage()
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
        public void SendStats()
        {
            NetworkMessage message = NetworkMessagePool.GetEmptyMessage();
            message.AddByte((byte)ServerPacketType.PlayerStatus);

            CharacterModel character = PlayerData.CharacterData;
            message.AddUInt16(character.Health);
            message.AddUInt16(character.HealthMax);

            message.AddUInt32(PlayerData.FreeCapacity);
            message.AddUInt32(character.Capacity);

            message.AddUInt64(character.Experience);

            message.AddUInt16(character.Level);
            message.AddByte(PlayerData.LevelPercent);
            message.AddDouble(0, 3); //Experience Bonus

            message.AddUInt16(character.Mana);
            message.AddUInt16(character.ManaMax);

            message.AddByte(PlayerData.EffectiveMagicLevel);
            message.AddByte(character.MagicLevel);
            message.AddByte(PlayerData.MagicLevelPercent);

            message.AddByte(character.Soul);

            message.AddUInt16(character.Stamina);
            message.AddUInt16((ushort)(PlayerData.BaseSpeed / 2U));

            Condition condition = PlayerData.GetCondition(ConditionFlags.Regeneration);
            message.AddUInt16(condition != null ? (ushort)(condition.Ticks / 1000) : (ushort)0x00);
            message.AddUInt16((ushort)(character.OfflineTrainingTime / 60U));

            WriteToOutputBuffer(message);
        }
        public void SendSkills()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        AddPlayerSkills(msg);
	        WriteToOutputBuffer(msg);
        }
        public void SendAddCreature(Creature creature, Position position, int stackpos, bool isLogin)
        {
            if(!CanSee(position))
                return;

            NetworkMessage msg;
            if (creature != PlayerData)
            {
                if (stackpos != -1)
                {
                    msg = NetworkMessagePool.GetEmptyMessage();
                    msg.AddByte((byte)ServerPacketType.TileAddThing);
                    msg.AddPosition(position);
                    msg.AddByte((byte)stackpos);

                    bool known;
                    uint removedKnown;
                    CheckCreatureAsKnown(creature.Id, out known, out removedKnown);
                    AddCreature(msg, creature, known, removedKnown);
                    WriteToOutputBuffer(msg);
                }

                if (isLogin)
                {
                    SendMagicEffect(position, MagicEffects.Teleport);
                }
                return;
            }

            msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte(0x17);

            msg.AddUInt32(PlayerData.Id);
            msg.AddUInt16(0x32); // beat duration (50)
            
	        msg.AddDouble(Creature.SpeedA, 3);
	        msg.AddDouble(Creature.SpeedB, 3);
	        msg.AddDouble(Creature.SpeedC, 3);

            msg.AddBoolean(PlayerData.AccountType >= AccountTypes.Tutor); //can report bugs?
            msg.AddBoolean(false); // can change pvp framing option
            msg.AddBoolean(false); // expert mode button enabled

            WriteToOutputBuffer(msg);

            SendPendingStateEntered();
            SendEnterWorld();
            SendMapDescription(position);

            if (isLogin)
                SendMagicEffect(position, MagicEffects.Teleport);

            SendInventoryItem(Slots.Head, PlayerData.Inventory[(byte)Slots.Head]);
            SendInventoryItem(Slots.Necklace, PlayerData.Inventory[(byte)Slots.Necklace]);
            SendInventoryItem(Slots.Backpack, PlayerData.Inventory[(byte)Slots.Backpack]);
            SendInventoryItem(Slots.Armor, PlayerData.Inventory[(byte)Slots.Armor]);
            SendInventoryItem(Slots.Right, PlayerData.Inventory[(byte)Slots.Right]);
            SendInventoryItem(Slots.Left, PlayerData.Inventory[(byte)Slots.Left]);
            SendInventoryItem(Slots.Legs, PlayerData.Inventory[(byte)Slots.Legs]);
            SendInventoryItem(Slots.Feet, PlayerData.Inventory[(byte)Slots.Feet]);
            SendInventoryItem(Slots.Ring, PlayerData.Inventory[(byte)Slots.Ring]);
            SendInventoryItem(Slots.Ammo, PlayerData.Inventory[(byte)Slots.Ammo]);

            SendStats();
            SendSkills();

            SendWorldLight();
            SendCreatureLight(creature);

            //TODO: VIP LIST

            SendBasicData();

            //TODO: ICONS
            //OutputMessagePool.AddToQueue(OutputBuffer);
        }
        public void SendBasicData()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.BasicData);
            msg.AddBoolean(PlayerData.IsPremium());
	        msg.AddUInt32(uint.MaxValue); //I do not know what this is?
	        msg.AddByte(PlayerData.VocationData.ClientId);
            msg.AddUInt16(0x00); //I do not know what this is?
	        WriteToOutputBuffer(msg);
        }
        public void SendCreatureLight(Creature creature)
        {
	        if (!CanSee(creature))
		        return;

            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        AddCreatureLight(msg, creature);
	        WriteToOutputBuffer(msg);
        }
        public void SendSpellCooldown(byte spellId, uint time)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte) ServerPacketType.SpellCooldown);
	        msg.AddByte(spellId);
	        msg.AddUInt32(time);
	        WriteToOutputBuffer(msg);
        }
        public void SendSpellGroupCooldown(SpellGroups groupId, uint time)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte) ServerPacketType.SpellGroupCooldown);
	        msg.AddByte((byte) groupId);
	        msg.AddUInt32(time);
	        WriteToOutputBuffer(msg);
        }
        public void SendWorldLight()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            AddWorldLight(msg, Game.WorldLight);
            WriteToOutputBuffer(msg);
        }
        public void SendInventoryItem(Slots slot, Item item)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        if (item != null)
            {
                msg.AddByte((byte)ServerPacketType.InventorySetSlot);
		        msg.AddByte((byte)slot);
		        msg.AddItem(item);
	        }
            else
            {
		        msg.AddByte((byte)ServerPacketType.InventoryClearSlot);
		        msg.AddByte((byte)slot);
	        }
	        WriteToOutputBuffer(msg);
        }
        public void SendPendingStateEntered()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.SelfAppear);
	        WriteToOutputBuffer(msg);
        }
        public void SendEnterWorld()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.EnterWorld);
	        WriteToOutputBuffer(msg);
        }
        public void SendMapDescription(Position pos)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.MapDescription);
	        msg.AddPosition(PlayerData.Position);
	        GetMapDescription(pos.X - 8, pos.Y - 6, pos.Z, 18, 14, msg);
	        WriteToOutputBuffer(msg);
        }
        public void SendMagicEffect(Position position, MagicEffects type)
        {
            NetworkMessage msg = new NetworkMessage();
            msg.AddByte((byte)ServerPacketType.MagicEffect);
            msg.AddPosition(position);
            msg.AddByte((byte)type);
            WriteToOutputBuffer(msg);
        }
        public void SendPingBack()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.Ping);
            WriteToOutputBuffer(msg);
        }
        public void SendAddContainerItem(byte cid, ushort slot, Item item)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.ContainerAddItem);
	        msg.AddByte(cid);
	        msg.AddUInt16(slot);
	        msg.AddItem(item);
	        WriteToOutputBuffer(msg);
        }
        public void SendUpdateContainerItem(byte cid, ushort slot, Item item)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.ContainerUpdateItem);
	        msg.AddByte(cid);
	        msg.AddUInt16(slot);
	        msg.AddItem(item);
	        WriteToOutputBuffer(msg);
        }
        public void SendRemoveContainerItem(byte cid, ushort slot, Item lastItem)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.ContainerRemoveItem);
	        msg.AddByte(cid);
	        msg.AddUInt16(slot);
	        if (lastItem != null)
		        msg.AddItem(lastItem);
	        else
		        msg.AddUInt16(0x00);
	        WriteToOutputBuffer(msg);
        }
        public void SendContainer(byte cid, Container container, bool hasParent, ushort firstIndex)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.ContainerOpen);
	        msg.AddByte(cid);

	        if (container.Id == (ushort)FixedItems.BrowseField)
            {
		        msg.AddItem(1987, 1);
		        msg.AddString("Browse Field");
	        }
            else
            {
		        msg.AddItem(container);
		        msg.AddString(container.GetName());
	        }

	        msg.AddByte((byte)container.MaxSize);
            msg.AddBoolean(hasParent);
            msg.AddBoolean(container.IsUnlocked); // Drag and drop
            msg.AddBoolean(container.HasPagination); // Pagination

	        ushort containerSize = (ushort)container.ItemList.Count;
	        msg.AddUInt16(containerSize);
	        msg.AddUInt16(firstIndex);
	        if (firstIndex < containerSize)
	        {
	            byte itemsToSend = (byte)Math.Min(Math.Min(container.MaxSize, containerSize - firstIndex), byte.MaxValue);

		        msg.AddByte(itemsToSend);
                foreach (Item item in container.ItemList)
                {
                    msg.AddItem(item);
                }
	        }
            else
            {
		        msg.AddByte(0x00);
	        }
	        WriteToOutputBuffer(msg);
        }
        public void SendAddTileItem(Position pos, byte stackpos, Item item)
        {
	        if (!CanSee(pos))
		        return;

            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.TileAddThing);
	        msg.AddPosition(pos);
	        msg.AddByte(stackpos);
	        msg.AddItem(item);
	        WriteToOutputBuffer(msg);
        }
        public void SendUpdateTileItem(Position pos, byte stackpos, Item item)
        {
            if (!CanSee(pos))
                return;

            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.TileUpdateThing);
            msg.AddPosition(pos);
            msg.AddByte(stackpos);
            msg.AddItem(item);
            WriteToOutputBuffer(msg);
        }
        public void SendRemoveTileThing(Position pos, byte stackpos)
        {
	        if (!CanSee(pos))
		        return;

            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        RemoveTileThing(msg, pos, stackpos);
	        WriteToOutputBuffer(msg);
        }
        public void SendTextMessage(TextMessage message)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.TextMessage);
	        msg.AddByte((byte)message.Type);
	        switch (message.Type)
            {
		        case MessageTypes.DamageDealt:
		        case MessageTypes.DamageReceived:
		        case MessageTypes.DamageOthers:
			        msg.AddPosition(message.Position);
			        msg.AddUInt32(message.PrimaryValue);
			        msg.AddByte((byte)message.PrimaryColor);
			        msg.AddUInt32(message.SecondaryValue);
			        msg.AddByte((byte)message.SecondaryColor);
			        break;
		        case MessageTypes.Healed:
		        case MessageTypes.HealedOthers:
		        case MessageTypes.Experience:
		        case MessageTypes.ExperienceOthers:
			        msg.AddPosition(message.Position);
			        msg.AddUInt32(message.PrimaryValue);
			        msg.AddByte((byte)message.PrimaryColor);
		            break;
	        }
	        msg.AddString(message.Text);
	        WriteToOutputBuffer(msg);
        }
        public void SendCancelWalk()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.PlayerWalkCancel);
	        msg.AddByte((byte)PlayerData.Direction);
	        WriteToOutputBuffer(msg);
        }
        public void SendUpdateTile(Tile tile, Position pos)
        {
	        if (!CanSee(pos))
		        return;

            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.TileUpdate);
	        msg.AddPosition(pos);

	        if (tile != null)
            {
		        GetTileDescription(tile, msg);
		        msg.AddByte(0x00);
		        msg.AddByte(0xFF);
	        }
            else
            {
		        msg.AddByte(0x01);
		        msg.AddByte(0xFF);
	        }

	        WriteToOutputBuffer(msg);
        }
        public void SendCloseContainer(byte cid)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.ContainerClose);
	        msg.AddByte(cid);
	        WriteToOutputBuffer(msg);
        }
        public void SendMoveCreature(Creature creature, Position newPos, int newStackPos, Position oldPos, byte oldStackPos, bool teleport)
        {
	        if (creature == PlayerData)
            {
		        if (oldStackPos >= 10)
                {
			        SendMapDescription(newPos);
		        }
                else if (teleport)
		        {
		            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
			        RemoveTileThing(msg, oldPos, oldStackPos);
			        WriteToOutputBuffer(msg);
			        SendMapDescription(newPos);
		        }
                else
		        {
		            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
			        if (oldPos.Z == 7 && newPos.Z >= 8)
                    {
				        RemoveTileThing(msg, oldPos, oldStackPos);
			        }
                    else
                    {
				        msg.AddByte((byte)ServerPacketType.CreatureMove);
				        msg.AddPosition(oldPos);
				        msg.AddByte(oldStackPos);
				        msg.AddPosition(newPos);
			        }

			        if (newPos.Z > oldPos.Z)
                    {
				        MoveDownCreature(msg, creature, newPos, oldPos);
			        }
                    else if (newPos.Z < oldPos.Z)
                    {
				        MoveUpCreature(msg, creature, newPos, oldPos);
			        }

			        if (oldPos.Y > newPos.Y)
                    { // north, for old x
                        msg.AddByte((byte)ServerPacketType.MapSliceNorth);
				        GetMapDescription(oldPos.X - 8, newPos.Y - 6, newPos.Z, 18, 1, msg);
			        }
                    else if (oldPos.Y < newPos.Y)
                    { // south, for old x
                        msg.AddByte((byte)ServerPacketType.MapSliceSouth);
				        GetMapDescription(oldPos.X - 8, newPos.Y + 7, newPos.Z, 18, 1, msg);
			        }

			        if (oldPos.X < newPos.X)
                    { // east, [with new y]
                        msg.AddByte((byte)ServerPacketType.MapSliceEast);
				        GetMapDescription(newPos.X + 9, newPos.Y - 6, newPos.Z, 1, 14, msg);
			        }
                    else if (oldPos.X > newPos.X)
                    { // west, [with new y]
                        msg.AddByte((byte)ServerPacketType.MapSliceWest);
				        GetMapDescription(newPos.X - 8, newPos.Y - 6, newPos.Z, 1, 14, msg);
			        }
			        WriteToOutputBuffer(msg);
		        }
	        }
            else if (CanSee(oldPos) && CanSee(creature.Position))
            {
		        if (teleport || (oldPos.Z == 7 && newPos.Z >= 8) || oldStackPos >= 10)
                {
			        SendRemoveTileThing(oldPos, oldStackPos);
			        SendAddCreature(creature, newPos, newStackPos, false);
		        }
                else
		        {
		            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
			        msg.AddByte((byte)ServerPacketType.CreatureMove);
			        msg.AddPosition(oldPos);
			        msg.AddByte(oldStackPos);
			        msg.AddPosition(creature.Position);
			        WriteToOutputBuffer(msg);
		        }
	        }
            else if (CanSee(oldPos))
            {
		        SendRemoveTileThing(oldPos, oldStackPos);
	        }
            else if (CanSee(creature.Position))
            {
		        SendAddCreature(creature, newPos, newStackPos, false);
	        }
        }
        public void SendCreatureTurn(Creature creature, byte stackPos)
        {
	        if (!CanSee(creature))
		        return;

            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.TileUpdateThing);
	        msg.AddPosition(creature.GetPosition());
	        msg.AddByte(stackPos);
	        msg.AddUInt16(0x63);
	        msg.AddUInt32(creature.Id);
	        msg.AddByte((byte)creature.Direction);
            msg.AddBoolean(!PlayerData.CanWalkthroughEx(creature));
	        WriteToOutputBuffer(msg);
        }
        public void SendChannelsDialog()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.ChannelList);

            List<ChatChannel> channels = Chat.GetChannelList(PlayerData);
            msg.AddByte((byte)channels.Count);
            foreach (ChatChannel channel in channels)
            {
                msg.AddUInt16(channel.Id);
                msg.AddString(channel.Name);
            }
	        WriteToOutputBuffer(msg);
        }
        public void SendChannel(ushort channelId, string channelName, Dictionary<uint, Player> channelUsers, Dictionary<uint, Player> invitedUsers)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.ChannelOpen);

	        msg.AddUInt16(channelId);
	        msg.AddString(channelName);

	        if (channelUsers != null)
            {
		        msg.AddUInt16((ushort)channelUsers.Count);
		        foreach (Player user in channelUsers.Values)
                {
			        msg.AddString(user.CharacterName);
		        }
	        }
            else
            {
		        msg.AddUInt16(0); // Channel user count
	        }

	        if (invitedUsers != null)
            {
		        msg.AddUInt16((ushort)invitedUsers.Count);
		        foreach (Player user in invitedUsers.Values)
                {
			        msg.AddString(user.CharacterName);
		        }
	        }
            else
            {
		        msg.AddUInt16(0); // Invited user count
	        }
	        WriteToOutputBuffer(msg);
        }
        public void SendCreatePrivateChannel(ushort channelId, string channelName)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.PrivateChannelCreate);
            msg.AddUInt16(channelId);
            msg.AddString(channelName);
            msg.AddUInt16(0x01); // Player count in channel
            msg.AddString(PlayerData.CharacterName); // That player
            msg.AddUInt16(0x00); // Invited player count in channel
            WriteToOutputBuffer(msg);
        }
        public void SendCreatureHealth(Creature creature)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.CreatureHealth);
	        msg.AddUInt32(creature.Id);

	        if (creature.IsHealthHidden)
            {
		        msg.AddByte(0x00);
	        }
            else
	        {
	            msg.AddByte(Tools.GetPercentage(creature.Health, creature.HealthMax));
	        }
	        WriteToOutputBuffer(msg);
        }
        public void SendCreatureOutfit(Creature creature, Outfit outfit)
        {
	        if (!CanSee(creature))
		        return;

            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.CreatureOutfit);
	        msg.AddUInt32(creature.Id);
	        AddOutfit(msg, outfit);
	        WriteToOutputBuffer(msg);
        }
        public void SendChangeSpeed(Creature creature, uint speed)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.CreatureSpeed);
	        msg.AddUInt32(creature.Id);
            msg.AddUInt16((ushort) (creature.BaseSpeed/2));
            msg.AddUInt16((ushort) (speed/2));
	        WriteToOutputBuffer(msg);
        }
        public void SendChannelEvent(ushort channelId, string playerName, ChatChannelEvents channelEvent)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.ChatChannelEvent);
            msg.AddUInt16(channelId);
            msg.AddString(playerName);
            msg.AddByte((byte)channelEvent);
            WriteToOutputBuffer(msg);
        }
        public void SendChannelMessage(string author, string text, SpeakTypes type, ushort channel)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.CreatureSpeech);
            msg.AddUInt32(0x00);
            msg.AddString(author);
            msg.AddUInt16(0x00);
            msg.AddByte((byte)type);
            msg.AddUInt16(channel);
            msg.AddString(text);
            WriteToOutputBuffer(msg);
        }
        public void SendClosePrivate(ushort channelId)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.ChannelClosePrivate);
	        msg.AddUInt16(channelId);
	        WriteToOutputBuffer(msg);
        }
        public void SendOpenPrivateChannel(string receiver)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.ChannelOpenPrivate);
            msg.AddString(receiver);
            WriteToOutputBuffer(msg);
        }

        private uint _sayToChannelStatementId;
        public void SendToChannel(Creature creature, SpeakTypes type, string text, ushort channelId)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.CreatureSpeech);

	        msg.AddUInt32(++_sayToChannelStatementId);
	        if (creature == null)
            {
		        msg.AddUInt32(0x00);
	        }
            else if (type == SpeakTypes.ChannelR2)
            {
		        msg.AddUInt32(0x00);
		        type = SpeakTypes.ChannelR1;
	        }
            else
            {
		        msg.AddString(creature.GetName());
		        //Add level only for players
                Player player = creature as Player;
		        if (player != null)
                {
			        msg.AddUInt16(player.CharacterData.Level);
		        }
                else
                {
			        msg.AddUInt16(0x00);
		        }
	        }

	        msg.AddByte((byte)type);
	        msg.AddUInt16(channelId);
	        msg.AddString(text);
	        WriteToOutputBuffer(msg);
        }

        private static uint _creatureSayStatementId;
        public void SendCreatureSay(Creature creature, SpeakTypes type, string text, Position pos = null)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.CreatureSpeech);
            msg.AddUInt32(++_creatureSayStatementId);
	        msg.AddString(creature.GetName());

	        //Add level only for players
            Player speaker = creature as Player;
	        if (speaker != null)
		        msg.AddUInt16(speaker.CharacterData.Level);
	        else
		        msg.AddUInt16(0x00);

	        msg.AddByte((byte)type);
	        if (pos != null)
		        msg.AddPosition(pos);
	        else
		        msg.AddPosition(creature.GetPosition());

	        msg.AddString(text);
	        WriteToOutputBuffer(msg);
        }

        private static uint _privateMessageStatementId;
        public void SendPrivateMessage(Player speaker, SpeakTypes type, string text)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte) ServerPacketType.CreatureSpeech);
            msg.AddUInt32(++_privateMessageStatementId);
	        if (speaker != null)
            {
		        msg.AddString(speaker.GetName());
		        msg.AddUInt16(speaker.CharacterData.Level);
	        }
            else
            {
		        msg.AddUInt32(0x00);
	        }
	        msg.AddByte((byte) type);
	        msg.AddString(text);
	        WriteToOutputBuffer(msg);
}
        #endregion

        #region Add Messages
        private void AddOutfit(NetworkMessage msg, Outfit outfit)
        {
	        msg.AddUInt16(outfit.LookType);

	        if (outfit.LookType != 0)
            {
		        msg.AddByte(outfit.LookHead);
		        msg.AddByte(outfit.LookBody);
		        msg.AddByte(outfit.LookLegs);
		        msg.AddByte(outfit.LookFeet);
		        msg.AddByte(outfit.LookAddons);
	        }
            else
            {
		        msg.AddItemId(outfit.LookTypeEx);
	        }

	        msg.AddUInt16(outfit.LookMount);
        }
        private void AddCreature(NetworkMessage msg, Creature creature, bool known, uint remove)
        {
	        CreatureTypes creatureType = creature.GetCreatureType();

	        Player otherPlayer = creature as Player;

	        if (known)
            {
		        msg.AddUInt16(0x62); //known
		        msg.AddUInt32(creature.Id);
	        }
            else
            {
		        msg.AddUInt16(0x61); //unknown
		        msg.AddUInt32(remove);
		        msg.AddUInt32(creature.Id);
		        msg.AddByte((byte)creatureType);
		        msg.AddString(creature.GetName());
	        }

            if (creature.IsHealthHidden)
                msg.AddByte(0x00);
            else
                msg.AddByte(Tools.GetPercentage(creature.Health, Math.Max(creature.HealthMax, 1U)));
            
	        msg.AddByte((byte)creature.Direction);

	        if (!creature.IsInGhostMode() && !creature.IsInvisible())
            {
		        AddOutfit(msg, creature.CurrentOutfit);
	        }
            else
            {
		        AddOutfit(msg, Outfit.EmptyOutfit);
	        }

            msg.AddByte(PlayerData.Group.Access ? (byte)0xFF : creature.InternalLight.Level);
	        msg.AddByte(creature.InternalLight.Color);

	        msg.AddUInt16((ushort)(creature.StepSpeed / 2));

            msg.AddByte((byte)SkullTypes.None);//player->getSkullClient(creature)); TODO: SKULL
            msg.AddByte((byte)PartyShields.None);//player->getPartyShield(otherPlayer)); TODO: PARTY

	        if (!known)
	            msg.AddByte((byte) GuildEmblems.None);//player->getGuildEmblem(otherPlayer)); TODO: GUILD

	        if (creatureType == CreatureTypes.Monster)
            {
		        if (creature.Master != null)
		        {
		            Player masterPlayer = creature.Master as Player;
		            if (masterPlayer != null)
		            {
				        if (masterPlayer == PlayerData)
					        creatureType = CreatureTypes.SummonOwn;
				        else
					        creatureType = CreatureTypes.SummonOthers;
			        }
		        }
	        }

	        msg.AddByte((byte)creatureType); // Type (for summons)
	        msg.AddByte((byte)creature.GetSpeechBubble());
	        msg.AddByte(0xFF); // MARK_UNMARKED

	        if (otherPlayer != null)
	        {
	            msg.AddUInt16(0xFF);//(otherPlayer->getHelpers()); TODO: HELPERS
	        }
            else
            {
		        msg.AddUInt16(0x00);
	        }

            msg.AddBoolean(!PlayerData.CanWalkthroughEx(creature)); //msg.AddByte((byte)(PlayerData.CanWalkthroughEx(creature) ? 0x00 : 0x01));
        }
        private void AddPlayerSkills(NetworkMessage msg)
        {
	        msg.AddByte((byte)ServerPacketType.PlayerSkillsUpdate);

	        for (Skills i = Skills.First; i <= Skills.Last; ++i)
            {
		        msg.AddUInt16(PlayerData.GetSkillLevel(i));
		        msg.AddUInt16(PlayerData.SkillLevels[(byte)i]);
		        msg.AddByte(PlayerData.SkillPercents[(byte)i]);
	        }
        }
        private void AddWorldLight(NetworkMessage msg, LightInfo lightInfo)
        {
	        msg.AddByte((byte)ServerPacketType.WorldLight);
            msg.AddByte(PlayerData.Group.Access ? (byte)0xFF : lightInfo.Level);
	        msg.AddByte(lightInfo.Color);
        }
        private void AddCreatureLight(NetworkMessage msg, Creature creature)
        {
	        msg.AddByte((byte)ServerPacketType.CreatureLight);
            msg.AddUInt32(creature.Id);
            msg.AddByte(PlayerData.Group.Access ? (byte)0xFF : creature.InternalLight.Level);
            msg.AddByte(creature.InternalLight.Color);
        }

        private void RemoveTileThing(NetworkMessage msg, Position pos, byte stackpos)
        {
	        if (stackpos >= 10)
		        return;

	        msg.AddByte(0x6C);
	        msg.AddPosition(pos);
	        msg.AddByte(stackpos);
        }
        
        private void MoveUpCreature(NetworkMessage msg, Creature creature, Position newPos, Position oldPos)
        {
	        if (creature != PlayerData)
		        return;

	        //floor change up
	        msg.AddByte((byte)ServerPacketType.FloorChangeUp);

	        //going to surface
	        if (newPos.Z == 7)
            {
		        int skip = -1;
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, 5, 18, 14, 3, ref skip); //(floor 7 and 6 already set)
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, 4, 18, 14, 4, ref skip);
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, 3, 18, 14, 5, ref skip);
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, 2, 18, 14, 6, ref skip);
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, 1, 18, 14, 7, ref skip);
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, 0, 18, 14, 8, ref skip);

		        if (skip >= 0)
                {
			        msg.AddByte((byte)skip);
			        msg.AddByte(0xFF);
		        }
	        }
	        //underground, going one floor up (still underground)
	        else if (newPos.Z > 7)
            {
		        int skip = -1;
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, (byte)(oldPos.Z - 3), 18, 14, 3, ref skip);

		        if (skip >= 0)
                {
			        msg.AddByte((byte)skip);
			        msg.AddByte(0xFF);
		        }
	        }

	        //moving up a floor up makes us out of sync
            //west
            msg.AddByte((byte)ServerPacketType.MapSliceWest);
	        GetMapDescription(oldPos.X - 8, oldPos.Y - 5, newPos.Z, 1, 14, msg);

            //north
            msg.AddByte((byte)ServerPacketType.MapSliceNorth);
	        GetMapDescription(oldPos.X - 8, oldPos.Y - 6, newPos.Z, 18, 1, msg);
        }
        private void MoveDownCreature(NetworkMessage msg, Creature creature, Position newPos, Position oldPos)
        {
	        if (creature != PlayerData)
		        return;

	        //floor change down
	        msg.AddByte((byte)ServerPacketType.FloorChangeDown);

	        //going from surface to underground
	        if (newPos.Z == 8)
            {
		        int skip = -1;

		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, newPos.Z, 18, 14, -1, ref skip);
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, (byte)(newPos.Z + 1), 18, 14, -2, ref skip);
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, (byte)(newPos.Z + 2), 18, 14, -3, ref skip);

		        if (skip >= 0)
                {
			        msg.AddByte((byte)skip);
			        msg.AddByte(0xFF);
		        }
	        }
	        //going further down
	        else if (newPos.Z > oldPos.Z && newPos.Z > 8 && newPos.Z < 14)
            {
		        int skip = -1;
		        GetFloorDescription(msg, oldPos.X - 8, oldPos.Y - 6, (byte)(newPos.Z + 2), 18, 14, -3, ref skip);

		        if (skip >= 0)
                {
			        msg.AddByte((byte)skip);
			        msg.AddByte(0xFF);
		        }
	        }

	        //moving down a floor makes us out of sync
	        //east
	        msg.AddByte((byte)ServerPacketType.MapSliceEast);
	        GetMapDescription(oldPos.X + 9, oldPos.Y - 7, newPos.Z, 1, 14, msg);

            //south
            msg.AddByte((byte)ServerPacketType.MapSliceSouth);
	        GetMapDescription(oldPos.X - 8, oldPos.Y + 7, newPos.Z, 18, 1, msg);
        }
        #endregion

        public static void Logout(Player player, bool displayEffect, bool forced)
        {
            if (player == null)
                return;

            if (!player.IsRemoved())
            {
		        if (!forced)
                {
                    if (!player.Group.Access)
                    {
                        if (player.Parent.Flags.HasFlag(TileFlags.NoLogout))
                        {
                            player.SendCancelMessage(ReturnTypes.YouCannotLogoutHere);
					        return;
				        }

                        if (!player.Parent.Flags.HasFlag(TileFlags.ProtectionZone) && player.HasCondition(ConditionFlags.InFight))
                        {
                            player.SendCancelMessage(ReturnTypes.YouMayNotLogoutDuringAFight);
                            return;
                        }
			        }

			        //scripting event - onLogout TODO: Scripting
                    //if (!g_creatureEvents->playerLogout(player))
                    //{
                    //    //Let the script handle the error message
                    //    return;
                    //}
		        }

                if (displayEffect && player.Health > 0)
                {
                    Game.AddMagicEffect(player.GetPosition(), MagicEffects.Poff);
		        }
	        }

            player.Connection.Disconnect();

            Game.RemoveCreature(player);
        }

        private void GetMapDescription(int x, int y, int z, int width, int height, NetworkMessage msg)
        {
	        int skip = -1;
	        int startz, endz, zstep;

	        if (z > 7)
            {
		        startz = z - 2;
	            endz = Math.Min(Map.MapMaxLayers - 1, z + 2);
                zstep = 1;
	        }
            else
            {
		        startz = 7;
		        endz = 0;
		        zstep = -1;
	        }

	        for (int nz = startz; nz != endz + zstep; nz += zstep)
            {
		        GetFloorDescription(msg, x, y, (byte)nz, width, height, z - nz, ref skip);
	        }

	        if (skip >= 0)
            {
		        msg.AddByte((byte)skip);
		        msg.AddByte(0xFF);
	        }
        }
        private void GetFloorDescription(NetworkMessage msg, int x, int y, byte z, int width, int height, int offset, ref int skip)
        {
	        for (int nx = 0; nx < width; nx++)
            {
		        for (int ny = 0; ny < height; ny++)
                {
			        Tile tile = Map.GetTile((ushort)(x + nx + offset), (ushort)(y + ny + offset), z);
			        if (tile != null)
                    {
				        if (skip >= 0)
                        {
					        msg.AddByte((byte)skip);
					        msg.AddByte(0xFF);
				        }

				        skip = 0;
				        GetTileDescription(tile, msg);
			        }
                    else if (skip == 0xFE)
                    {
				        msg.AddByte(0xFF);
				        msg.AddByte(0xFF);
				        skip = -1;
			        }
                    else
                    {
				        ++skip;
			        }
		        }
	        }
        }
        private void GetTileDescription(Tile tile, NetworkMessage msg)
        {
	        msg.AddUInt16(0x00); //environmental effects

	        int count;
	        if (tile.Ground != null)
            {
		        msg.AddItem(tile.Ground);
		        count = 1;
	        }
            else
            {
		        count = 0;
	        }

	        if (tile.Items != null)
            {
                for(int i = tile.TopItemsIndex; i < tile.Items.Count; i++)
                {
                    msg.AddItem(tile.Items[i]);

                    if(++count == 10)
                        return;
                }
	        }

	        if (tile.Creatures != null)
            {
                foreach (Creature creature in tile.Creatures)
                {
			        if (!PlayerData.CanSeeCreature(creature))
				        continue;

			        bool known;
			        uint removedKnown;
			        CheckCreatureAsKnown(creature.Id, out known, out removedKnown);
			        AddCreature(msg, creature, known, removedKnown);

			        if (++count == 10)
				        return;
		        }
	        }

            if (tile.Items != null)
            {
                for(int i = 0; i < tile.TopItemsIndex; i++)
                {
			        msg.AddItem(tile.Items[i]);

			        if (++count == 10)
				        return;
		        }
	        }
        }

        private void CheckCreatureAsKnown(uint id, out bool known, out uint removedKnown)
        {
            removedKnown = 0;
            if (!_knownCreatureSet.Add(id))
            {
                known = true;
                return;
            }

	        known = false;

	        if (_knownCreatureSet.Count > 1300)
            {
                foreach (uint idToRemove in _knownCreatureSet)
                {
                    Creature creature = Game.GetCreatureById(idToRemove);
                    if (!CanSee(creature))
                    {
                        removedKnown = idToRemove;
                        _knownCreatureSet.Remove(idToRemove);
                        return;
                    }
                }

		        // Bad situation. Let's just remove anyone.
                foreach (uint idToRemove in _knownCreatureSet)
                {
                    if (idToRemove != id)
                    {
                        removedKnown = idToRemove;
                        _knownCreatureSet.Remove(idToRemove);
                        break;
                    }
		        }
	        }
        }

        private bool CanSee(Creature c)
        {
	        if (c == null || PlayerData == null || c.IsInternalRemoved)
		        return false;

	        if (!PlayerData.CanSeeCreature(c))
		        return false;

	        return CanSee(c.Position);
        }
        private bool CanSee(Position pos)
        {
	        return CanSee(pos.X, pos.Y, pos.Z);
        }
        private bool CanSee(ushort x, ushort y, byte z)
        {
	        if (PlayerData == null)
		        return false;

	        Position myPos = PlayerData.Position;
	        if (myPos.Z <= 7) {
		        //we are on ground level or above (7 -> 0)
		        //view is from 7 -> 0
		        if (z > 7) {
			        return false;
		        }
	        } else if (myPos.Z >= 8) {
		        //we are underground (8 -> 15)
		        //view is +/- 2 from the floor we stand on
		        if (Math.Abs(myPos.Z - z) > 2) {
			        return false;
		        }
	        }

	        //negative offset means that the action taken place is on a lower floor than ourself
	        int offsetz = myPos.Z - z;
	        if ((x >= myPos.X - 8 + offsetz) && (x <= myPos.X + 9 + offsetz) &&
	                (y >= myPos.Y - 6 + offsetz) && (y <= myPos.Y + 7 + offsetz)) {
		        return true;
	        }
	        return false;
        }

        public override string ToString()
        {
            if(AccountName != null)
                return AccountName + " - " + PlayerName;
            return base.ToString();
        }
    }
}
