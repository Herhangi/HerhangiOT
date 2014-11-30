using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Model;
using HerhangiOT.ServerLibrary.Threading;

namespace HerhangiOT.LoginServer
{
    public class LoginServer
    {
        public static string MOTD { get; private set; }
        public static List<GameWorld> GameWorlds { get; private set; }
        public static HashAlgorithm PasswordHasher { get; private set; }

        private TcpListener _listener;
        private List<Socket> _loginRequesters = new List<Socket>();
 
        public void Start()
        {
            ConfigManager.ConfigsLoaded += ConfigManager_ConfigsLoaded;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            if (ConfigManager.Instance[ConfigBool.USE_EXTERNAL_LOGIN_SERVER])
            {
                GameWorlds = Database.Instance.GetGameWorlds();
            }
            else
            {
                GameWorlds = new List<GameWorld>();
                GameWorlds.Add(new GameWorld()
                {
                    GameWorldId = 1,
                    GameWorldName = ConfigManager.Instance[ConfigStr.SERVER_NAME],
                    GameWorldIP = ConfigManager.Instance[ConfigStr.GAME_SERVER_IP],
                    GameWorldPort = (ushort)ConfigManager.Instance[ConfigInt.GAME_SERVER_PORT]
                });
            }

            switch (ConfigManager.Instance[ConfigStr.PASSWORD_HASH_ALGORITHM])
            {
                case "sha1":
                    PasswordHasher = SHA1.Create();
                    break;
                case "md5":
                    PasswordHasher = MD5.Create();
                    break;
                case "plain":
                    throw new NotImplementedException();
                default:
                    Logger.Log(LogLevels.Error, "Unknown password hash algorithm detected!");
                    return;
            }
            MOTD = string.Format("{0}\n{1}", ConfigManager.Instance[ConfigInt.MOTD_NUM], ConfigManager.Instance[ConfigStr.MOTD]);
            _listener = new TcpListener(IPAddress.Any, ConfigManager.Instance[ConfigInt.LOGIN_PORT]);
            _listener.Start();
            _listener.BeginAcceptSocket(LoginListenerCallback, _listener);
            Logger.Log(LogLevels.Operation, "LoginServer Listening for clients");
        }

        void ConfigManager_ConfigsLoaded()
        {
            MOTD = string.Format("{0}\n{1}", ConfigManager.Instance[ConfigInt.MOTD_NUM], ConfigManager.Instance[ConfigStr.MOTD]);
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Logger.Log(LogLevels.Operation, "Login Server Shutting Down!");
            _listener.Stop();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log(((e.IsTerminating) ? LogLevels.Error : LogLevels.Warning), e.ExceptionObject.ToString());
            
            if (e.IsTerminating)
            {
                Logger.Log(LogLevels.Information, "Login LoginServer Shutting Down!");
                _listener.Stop();
            }
        }

        private void LoginListenerCallback(IAsyncResult ar)
        {
            LoginConnection requester = new LoginConnection();
            requester.HandleFirstConnection(ar);

            _listener.BeginAcceptSocket(LoginListenerCallback, _listener);
        }

        #region Command Line Operations
        public Dictionary<string, Action> CommandLineOperations = new Dictionary<string, Action>()
        {
            {"exit", ExitOperation}
        };

        private static void ExitOperation()
        {
            string response;
            do
            {
                Console.Write("Are you sure to exit(yes/no): ");
                response = (Console.ReadLine() ?? string.Empty).ToLowerInvariant();
            } while ( !response.Equals("yes") && !response.Equals("no") );

            if (response.Equals("yes"))
                Environment.Exit(0);
        }
        #endregion
    }
}
