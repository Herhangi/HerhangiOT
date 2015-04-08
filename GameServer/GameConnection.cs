using System;
using System.Collections.Generic;
using System.Security.Policy;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
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
        public OperatingSystems ClientOs { get; private set; }
        public Player PlayerData { get; private set; }
        
        private HashSet<uint> _knownCreatureSet = new HashSet<uint>(); 

        private uint _challengeTimestamp;
        private byte _challengeRandom;
        
        private static readonly Dictionary<ClientPacketType, Action<GameConnection>> PacketHandlers = new Dictionary<ClientPacketType, Action<GameConnection>>
        {
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

        protected override void ParseHeader(IAsyncResult ar)
        {
            base.ParseHeader(ar);

            if(Stream.CanRead)
                Stream.BeginRead(InMessage.Buffer, 0, 2, ParseHeader, null);
        }

        protected override void ProcessFirstMessage(bool isChecksummed)
        {
            if (Game.Instance.GameState == GameStates.Shutdown)
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

            if (version < Constants.CLIENT_VERSION_MIN || (version > Constants.CLIENT_VERSION_MAX))
            {
                DispatchDisconnect("Only clients with protocol " + Constants.CLIENT_VERSION_STR + " allowed!");
                return;
            }

            if (ClientOs >= OperatingSystems.OtClientLinux)
            {
                DispatchDisconnect("Custom clients are not supported, yet!");
                return;
            }

            if (Game.Instance.GameState == GameStates.Startup)
            {
                DispatchDisconnect("Gameworld is starting up. Please wait.");
                return;
            }

            if (Game.Instance.GameState == GameStates.Maintain)
            {
                DispatchDisconnect("Gameworld is under maintenance. Please re-connect in a while.");
                return;
            }

            // TODO: CHECK FOR BAN

            SecretCommunication.OnCharacterAuthenticationResultArrived += OnCharacterAuthenticationResultArrived;
            SecretCommunication.CheckCharacterAuthenticity(SessionKey, characterName);
        }

        protected override void ProcessMessage()
        {
            ClientPacketType requestType = (ClientPacketType)InMessage.GetByte();

            if(PacketHandlers.ContainsKey(requestType))
                PacketHandlers[requestType].Invoke(this);
            else
            {
                Logger.Log(LogLevels.Debug, "Unknown message arrived: " + requestType.ToString());
            }
        }

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
                Login();
            }
        }

        public void Login()
        {
            PlayerData = Game.Instance.GetPlayerByName(PlayerName);
            if (PlayerData == null)
            {
                PlayerData = new Player(this, AccountName, PlayerName);

                if(!PlayerData.PreloadPlayer())
                {
                    DispatchDisconnect("Your character could not be loaded.");
                    return;
                }
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

                if (!Game.Instance.PlaceCreature(PlayerData, PlayerData.LoginPosition))
                {
                    if (!Game.Instance.PlaceCreature(PlayerData, PlayerData.LoginPosition, false, true))
                    {
                        DispatchDisconnect("Temple position is wrong. Contact the administrator!");
                        return;
                    }
                }
                
                //LAST LOGIN OPERATIONS
            }
            else
            {
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

            //Condition* condition = player->getCondition(CONDITION_REGENERATION);
            message.AddUInt16(0);//condition ? condition->getTicks() / 1000 : 0x00); TODO: CONDITIONS
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
            OutputMessagePool.AddToQueue(OutputBuffer);
        }
        
        private void SendBasicData()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.BasicData);
            msg.AddBoolean(PlayerData.IsPremium());
	        msg.AddUInt32(uint.MaxValue); //I do not know what this is?
	        msg.AddByte(PlayerData.VocationData.ClientId);
            msg.AddUInt16(0x00); //I do not know what this is?
	        WriteToOutputBuffer(msg);
        }

        private void SendCreatureLight(Creature creature)
        {
	        if (!CanSee(creature))
		        return;

            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        AddCreatureLight(msg, creature);
	        WriteToOutputBuffer(msg);
        }

        private void SendWorldLight()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            AddWorldLight(msg, Game.Instance.WorldLight);
            WriteToOutputBuffer(msg);
        }

        private void SendInventoryItem(Slots slot, Item item)
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

        private void SendPendingStateEntered()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
	        msg.AddByte((byte)ServerPacketType.SelfAppear);
	        WriteToOutputBuffer(msg);
        }

        private void SendEnterWorld()
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.EnterWorld);
	        WriteToOutputBuffer(msg);
        }

        private void SendMapDescription(Position pos)
        {
            NetworkMessage msg = NetworkMessagePool.GetEmptyMessage();
            msg.AddByte((byte)ServerPacketType.MapDescription);
	        msg.AddPosition(PlayerData.Position);
	        GetMapDescription(pos.X - 8, pos.Y - 6, pos.Z, 18, 14, msg);
	        WriteToOutputBuffer(msg);
        }

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

            msg.AddByte(0xFF);//player->isAccessPlayer() ? 0xFF : lightInfo.level); TODO: ACCESS
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
            msg.AddByte(lightInfo.Level);// (player->isAccessPlayer() ? 0xFF : lightInfo.level)); TODO: ACCESS
	        msg.AddByte(lightInfo.Color);
        }

        private void AddCreatureLight(NetworkMessage msg, Creature creature)
        {
	        msg.AddByte((byte)ServerPacketType.CreatureLight);
	        msg.AddUInt32(creature.Id);
            msg.AddByte(creature.InternalLight.Level);//(player->isAccessPlayer() ? 0xFF : lightInfo.level)); TODO: ACCESS
            msg.AddByte(creature.InternalLight.Color);
        }
        #endregion

        public void SendMagicEffect(Position position, MagicEffects type)
        {
	        NetworkMessage msg = new NetworkMessage();
	        msg.AddByte((byte)ServerPacketType.MagicEffect);
	        msg.AddPosition(position);
	        msg.AddByte((byte)type);
	        WriteToOutputBuffer(msg);
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
			        Tile tile = Map.Instance.GetTile((ushort)(x + nx + offset), (ushort)(y + ny + offset), z);
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
                    Creature creature = Game.Instance.GetCreatureById(idToRemove);
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
    }
}
