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

        #region Command Line Operations
        private static void ClearOperation()
        {
            Console.Clear();
        }

        private static void ExitOperation()
        {
            string response;
            do
            {
                Console.Write("Are you sure to exit(yes/no): ");
                response = (Console.ReadLine() ?? string.Empty).ToLowerInvariant();
            } while (!response.Equals("yes") && !response.Equals("no"));

            if (response.Equals("yes"))
                Environment.Exit(0);
        }
        #endregion
    }
}
