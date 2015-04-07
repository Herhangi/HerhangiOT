using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Database.Model;

namespace HerhangiOT.LoginServer
{
    public class LoginServer
    {
        public static string MOTD { get; private set; }
        public static Dictionary<byte, GameWorldModel> GameWorlds { get; private set; }
        public static Dictionary<byte, GameServerConnection> GameServerConnections { get; private set; }

        public static Dictionary<string, AccountModel> OnlineAccounts { get; private set; } //Was storing account name, converted to sessionKey with 10.76 update
        public static Dictionary<string, string> OnlineCharactersByAccount { get; private set; }
        public static HashAlgorithm PasswordHasher { get; protected set; }

        private TcpListener _listener;
        private TcpListener _secretServerListener;
        private List<Socket> _loginRequesters = new List<Socket>();
 
        public void Start()
        {
            ConfigManager.ConfigsLoaded += ConfigManager_ConfigsLoaded;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            OnlineAccounts = new Dictionary<string, AccountModel>();
            OnlineCharactersByAccount = new Dictionary<string, string>();
            GameServerConnections = new Dictionary<byte, GameServerConnection>();
            if (ConfigManager.Instance[ConfigBool.UseExternalLoginServer])
            {
                GameWorlds = Database.Instance.GetGameWorlds().ToDictionary(i => i.GameWorldId);
            }
            else
            {
                GameWorlds = new Dictionary<byte, GameWorldModel>
                {
                    {
                        (byte)ConfigManager.Instance[ConfigInt.GameServerId], new GameWorldModel
                        {
                            GameWorldId = (byte)ConfigManager.Instance[ConfigInt.GameServerId],
                            GameWorldName = ConfigManager.Instance[ConfigStr.StatusServerName],
                            GameWorldIP = ConfigManager.Instance[ConfigStr.GameServerIP],
                            GameWorldPort = (ushort) ConfigManager.Instance[ConfigInt.GameServerPort],
                            Secret = ConfigManager.Instance[ConfigStr.GameServerSecret]
                        }
                    }
                };
            }

            switch (ConfigManager.Instance[ConfigStr.PasswordHashAlgorithm])
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
            MOTD = string.Format("{0}\n{1}", ConfigManager.Instance[ConfigInt.MOTDNum], ConfigManager.Instance[ConfigStr.MOTD]);
            _listener = new TcpListener(IPAddress.Any, ConfigManager.Instance[ConfigInt.LoginServerPort]);
            _listener.Start();
            _listener.BeginAcceptSocket(LoginListenerCallback, _listener);

            _secretServerListener = new TcpListener(IPAddress.Any, ConfigManager.Instance[ConfigInt.LoginServerSecretPort]);
            _secretServerListener.Start();
            _secretServerListener.BeginAcceptSocket(MasterServerListenerCallback, _secretServerListener);

            Logger.Log(LogLevels.Operation, "LoginServer Listening for clients");
        }

        void ConfigManager_ConfigsLoaded()
        {
            MOTD = string.Format("{0}\n{1}", ConfigManager.Instance[ConfigInt.MOTDNum], ConfigManager.Instance[ConfigStr.MOTD]);
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

        private void MasterServerListenerCallback(IAsyncResult ar)
        {
            GameServerConnection requester = new GameServerConnection();
            requester.HandleFirstConnection(ar);

            _listener.BeginAcceptSocket(LoginListenerCallback, _listener);
        }

        #region Command Line Operations

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
