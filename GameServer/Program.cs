using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HerhangiOT.ScriptLibrary;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
using HerhangiOT.ServerLibrary.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer
{
    class Program
    {
        private static GameServer _gameServer = new GameServer();
        private static LoginServer.LoginServer _loginServer;
        
        private const string RsaP = "14299623962416399520070177382898895550795403345466153217470516082934737582776038882967213386204600674145392845853859217990626450972452084065728686565928113";
        private const string RsaQ = "7630979195970404721891201847792002125535401292779123937207447574596692788513647179235335529307251350570728407373705564708871762033017096809910315212884101";

        static void Main(string[] args)
        {
            ExternalMethods.SetConsoleCtrlHandler(ConsoleCtrlOperationHandler, true);

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
            if (!LoadCLO())
                ExitApplication();

            while (true)
            {
                string command = Console.ReadLine();

                if (command == null) continue;

                if(_gameServer.CommandLineOperations.ContainsKey(command))
                    _gameServer.CommandLineOperations[command].Invoke();
                else if (_loginServer != null && _loginServer.CommandLineOperations.ContainsKey(command))
                    _loginServer.CommandLineOperations[command].Invoke();
                else
                {
                    Logger.Log(LogLevels.Warning, "Command is unknown!");
                }
            }
        }

        public static bool LoadCLO()
        {
            Logger.LogOperationStart("Loading Command Line Operations");

            Assembly cloAssembly;
            List<string> externalAssemblies = new List<string>();
            externalAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            externalAssemblies.Add(Assembly.GetAssembly(typeof(HerhangiOT.ScriptLibrary.CommandLineOperation)).Location);

            if (!Directory.Exists("CompiledDllCache"))
                Directory.CreateDirectory("CompiledDllCache");

            if (!ScriptManager.CompileCsScripts("Scripts/CLO", "CompiledDllCache/CLO.dll", externalAssemblies, out cloAssembly))
                return false;

            try
            {
                foreach (Type clo in cloAssembly.GetTypes())
                {
                    if (clo.BaseType == typeof(CommandLineOperation))
                    {
                        CommandLineOperation voc = (CommandLineOperation)Activator.CreateInstance(clo);
                        voc.Setup();
                        voc.InviteToGameServerCommandList(_gameServer);
                        if(_loginServer != null)
                            voc.InviteToLoginServerCommandList(_loginServer);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogOperationFailed(e.ToString());
                return false;
            }

            Logger.LogOperationDone();
            return true;
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
