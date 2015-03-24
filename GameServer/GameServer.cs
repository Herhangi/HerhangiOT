using System;
using System.Net;
using System.Net.Sockets;
using HerhangiOT.ServerLibrary;

namespace HerhangiOT.GameServer
{
    public class GameServer
    {
        private TcpListener _listener;

        public void Start()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            _listener = new TcpListener(IPAddress.Any, ConfigManager.Instance[ConfigInt.GAME_SERVER_PORT]);
            _listener.Start();
            _listener.BeginAcceptSocket(GameListenerCallback, _listener);
            Logger.Log(LogLevels.Operation, "GameServer Listening for clients");
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Logger.Log(LogLevels.Operation, "GameServer Shutting Down!");
            _listener.Stop();
        }

        private void GameListenerCallback(IAsyncResult ar)
        {
            GameConnection requester = new GameConnection();
            requester.HandleFirstConnection(ar);

            _listener.BeginAcceptSocket(GameListenerCallback, _listener);
        }
    }
}
