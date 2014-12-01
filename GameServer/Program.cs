using System;
using System.Diagnostics;
using HerhangiOT.ScriptLibrary;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer
{
    public class Program
    {
        public static uint Uptime { get { return (uint)(DateTime.Now - _startTime).Ticks; } }
        private static GameServer _gameServer = new GameServer();
        private static LoginServer.LoginServer _loginServer;

        private static DateTime _startTime;
        private const string RsaP = "14299623962416399520070177382898895550795403345466153217470516082934737582776038882967213386204600674145392845853859217990626450972452084065728686565928113";
        private const string RsaQ = "7630979195970404721891201847792002125535401292779123937207447574596692788513647179235335529307251350570728407373705564708871762033017096809910315212884101";

        static void Main(string[] args)
        {
            ExternalMethods.SetConsoleCtrlHandler(ConsoleCtrlOperationHandler, true);

            Tools.Initialize();

            Console.Title = Constants.STATUS_SERVER_NAME;
            Console.Clear();
            Console.WriteLine("Welcome to {0} - Version {1}", Constants.STATUS_SERVER_NAME, Constants.STATUS_SERVER_VERSION);
            Console.WriteLine("Developed by {0}", Constants.STATUS_SERVER_DEVELOPERS);
            Console.WriteLine("-----------------------------------------------------");
            
            // Start Threads
            DispatcherManager.Start();

            // Loading config.lua
            if (!ConfigManager.Load("config.lua"))
                ExitApplication();

            // Setting up process priority
            switch (ConfigManager.Instance[ConfigStr.DEFAULT_PRIORITY])
            {
                case "realtime":
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
                    break;
                case "high":
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                    break;
                case "higher":
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
                    break;
            }
            
            // Setting up RSA cyrpto
            if(!Rsa.SetKey(RsaP, RsaQ))
                ExitApplication();

            // Initializing Database connection
            if (!Database.Initialize())
                ExitApplication();
            //DATABASE MANAGER UPDATE DATABASE

            // Loading vocations
            if (!Vocation.Load())
                ExitApplication();

            // Loading items
            if(!ItemManager.Load())
                ExitApplication();
            
            // Loading scripts
            if(!ScriptManager.LoadCsScripts() || !ScriptManager.LoadLuaScripts())
                ExitApplication();

            // LOAD CREATURES HERE
            // LOAD OUTFITS HERE

            switch (ConfigManager.Instance[ConfigStr.WORLD_TYPE])
            {
                case "pvp":
                    Game.WorldType = GameWorldTypes.Pvp;
                    break;
                case "no-pvp":
                    Game.WorldType = GameWorldTypes.NoPvp;
                    break;
                case "pvp-enforced":
                    Game.WorldType = GameWorldTypes.PvpEnforced;
                    break;
            }
            Logger.Log(LogLevels.Operation, "Setting Game World Type: " + Game.WorldType);

            // Initialize Game State

            if (ConfigManager.Instance[ConfigBool.USE_EXTERNAL_LOGIN_SERVER])
            {
                
            }
            else
            {
                _loginServer = new LoginServer.LoginServer();
                _loginServer.Start();
            }

            _gameServer.Start();

            // Loading Command Line Operations
            if (!ScriptManager.LoadCommandLineOperations())
                ExitApplication();

            while (true)
            {
                string input = Console.ReadLine();

                if (input == null) continue;
                input = input.Trim();
                input = input.ToLowerInvariant();

                string[] command = input.Split(' ');

                if (ScriptManager.CommandLineOperations.ContainsKey(command[0]))
                {
                    try
                    {
                        ScriptManager.CommandLineOperations[command[0]].Invoke(command);
                    }
                    catch (Exception)
                    {
                        Logger.Log(LogLevels.Warning, "Command '"+command[0]+"' could not be executed in this environment!");
                    }
                }
                else
                {
                    Logger.Log(LogLevels.Warning, "Command is unknown!");
                }
            }
        }

        private static void ConsoleCtrlOperationHandler(ConsoleCtrlEvents ctrlEvent)
        {
            Logger.Log(LogLevels.Warning, "Command Line Operations Disabled!");
        }

        static void ExitApplication()
        {
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
