using System;
using System.Collections.Generic;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.LoginServer
{
    public class GameServerConnection : Connection
    {
        private bool _isLoggedIn;
        private byte _gameServerId;
        protected OutputMessage OutMessage = new OutputMessage {IsRecycledMessage = false};
        
        protected static Dictionary<SecretNetworkPacketType, Action<GameServerConnection>> PacketHandlers = new Dictionary<SecretNetworkPacketType, Action<GameServerConnection>>
        {
            {SecretNetworkPacketType.Authentication, ProcessAuthenticationPacket}
        };

        public GameServerConnection()
        {
            IsSecretConnection = true;
        }

        protected override void ProcessMessage()
        {
            SecretNetworkPacketType packet = (SecretNetworkPacketType) InMessage.GetByte();

            if (!PacketHandlers.ContainsKey(packet))
            {
                Logger.Log(LogLevels.Warning, "SECRET: Unknown packet type arrived from game server: "+GetServerName());
                return;
            }
            Action<GameServerConnection> handler = PacketHandlers[packet];
            handler.Invoke(this);
        }

        protected string GetServerName()
        {
            if (!_isLoggedIn)
                return "UNAUTHENTICATED";
            return LoginServer.GameWorlds[_gameServerId].GameWorldName;
        }

        #region Packet Processors
        private static void ProcessAuthenticationPacket(GameServerConnection conn)
        {
            byte gameServerId = conn.InMessage.GetByte();
            string secret = conn.InMessage.GetString();

            Logger.Log(LogLevels.Information, "SECRET: New game server connected with id: " + gameServerId);
            if (LoginServer.GameWorlds.ContainsKey(gameServerId))
            {
                if (!LoginServer.GameWorlds[gameServerId].Secret.Equals(secret, StringComparison.InvariantCulture))
                {
                    conn._isLoggedIn = true;
                    conn._gameServerId = gameServerId;

                    if (LoginServer.GameServerConnections.ContainsKey(gameServerId))
                    {
                        LoginServer.GameServerConnections[gameServerId].Disconnect("NEW_CONNECTION_REPLACEMENT");
                        LoginServer.GameServerConnections.Remove(gameServerId);
                        Logger.Log(LogLevels.Warning, "SECRET: Game server connection replaced! ID: " + gameServerId);
                    }
                    LoginServer.GameServerConnections.Add(gameServerId, conn);
                    conn.SendAuthenticationResponse(SecretNetworkResponseType.Success);
                    return; //GAME SERVER AUTHENTICATED
                }

                conn.SendAuthenticationResponse(SecretNetworkResponseType.InvalidGameServerSecret);
                Logger.Log(LogLevels.Warning, "SECRET: Game server connection with wrong secret! ID: " + gameServerId);
            }
            else
            {
                conn.SendAuthenticationResponse(SecretNetworkResponseType.InvalidGameServerId);
                Logger.Log(LogLevels.Warning, "SECRET: Game server connection with unknown Id! ID: " + gameServerId);
            }
        }
        #endregion

        #region Response Senders
        public void SendAuthenticationResponse(SecretNetworkResponseType response)
        {
            OutMessage.Reset();
            OutMessage.AddByte((byte)SecretNetworkPacketType.Authentication);
            OutMessage.AddByte((byte)response);
            if (response != SecretNetworkResponseType.Success)
                OutMessage.DisconnectAfterMessage = true;
            Send(OutMessage);
        }
        #endregion
    }
}
