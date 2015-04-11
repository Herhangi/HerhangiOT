using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Networking;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer
{
    public class SecretServerConnection : Connection
    {
        public const long ConnectionTimeout = 2000;
        public static SecretServerConnection Instance { get; private set; }

        public TcpClient Client { get; set; }
        public OutputMessage OutMessage { get; set; }
        public bool IsServerApproved { get; set; }
        public bool IsConnectionEstablished { get; set; }

        protected Dictionary<SecretNetworkPacketType, Action> PacketHandlers = new Dictionary<SecretNetworkPacketType, Action>
        {
            {SecretNetworkPacketType.Authentication, ProcessAuthenticationPacket}
        };

        protected override void ProcessFirstMessage(bool isChecksummed)
        {
            
        }

        protected override void ProcessMessage()
        {
            SecretNetworkPacketType packet = (SecretNetworkPacketType)InMessage.GetByte();

            if (!PacketHandlers.ContainsKey(packet))
            {
                Logger.Log(LogLevels.Warning, "SECRET: Unknown packet type arrived from login server!");
                return;
            }
            Action handler = PacketHandlers[packet];
            handler.Invoke();
        }

        public static bool Initialize()
        {
            Logger.LogOperationStart("Connecting to secret channel");

            Instance = new SecretServerConnection();

            Instance.IsSecretConnection = true;
            Instance.InMessage = new NetworkMessage();
            Instance.OutMessage = new OutputMessage { IsRecycledMessage = false };
            Instance.Connect();

            long start = Tools.GetSystemMilliseconds();
            while (!Instance.IsConnectionEstablished)
            {
                Thread.Sleep(100); //Stall until connection established

                long timePassd = (Tools.GetSystemMilliseconds() - start);
                if (timePassd > ConnectionTimeout)
                {
                    Logger.LogOperationFailed();
                    return false;
                }
            }
            return true;
        }

        public void Connect()
        {
            Client = new TcpClient();
            Client.BeginConnect(ConfigManager.Instance[ConfigStr.LoginServerIP],
                ConfigManager.Instance[ConfigInt.LoginServerSecretPort], HandleFirstConnection, Client);
        }

        public override void HandleFirstConnection(IAsyncResult ar)
        {
            Client.EndConnect(ar);
            Stream = Client.GetStream();
            InMessage = new NetworkMessage();
            IsChecksumEnabled = true;
            XteaKey = new uint[4];
            IsConnectionEstablished = true;
            Logger.LogOperationDone();

            Stream.BeginRead(InMessage.Buffer, 0, 2, ParseHeader, null);
            SendLoginMessage();
        }

        private void SendLoginMessage()
        {
            OutMessage.Reset();
            OutMessage.AddByte((byte)SecretNetworkPacketType.Authentication);
            OutMessage.AddByte((byte)ConfigManager.Instance[ConfigInt.GameServerId]);
            OutMessage.AddString(ConfigManager.Instance[ConfigStr.GameServerSecret]);
            OutMessage.WriteMessageLength();

            Stream.Write(OutMessage.Buffer, OutMessage.HeaderPosition, OutMessage.Length);
        }

        
        #region Packet Processors
        private static void ProcessAuthenticationPacket()
        {
            SecretNetworkResponseType response = (SecretNetworkResponseType)Instance.InMessage.GetByte();

            if (response == SecretNetworkResponseType.Success)
            {
                Instance.IsServerApproved = true;
            }
            else
            {
                Logger.Log(LogLevels.Error, "SECRET CONNECTION REFUSED WITH: " + response + "! PLEASE FIX!!!");
                Program.ExitApplication();
            }
        }
        #endregion
    }
}
