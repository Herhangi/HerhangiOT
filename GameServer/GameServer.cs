﻿using System;
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
            _listener = new TcpListener(IPAddress.Any, ConfigManager.Instance[ConfigInt.GAME_SERVER_PORT]);
            _listener.Start();
            _listener.BeginAcceptSocket(GameListenerCallback, _listener);
        }

        private void GameListenerCallback(IAsyncResult ar)
        {
            GameConnection requester = new GameConnection();
            requester.HandleFirstConnection(ar);

            _listener.BeginAcceptSocket(GameListenerCallback, _listener);
        }
    }
}
