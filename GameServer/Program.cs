using System;
using System.Collections.Generic;
using System.Diagnostics;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
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
            Console.Title = Constants.STATUS_SERVER_NAME;
            Console.Clear();
            Console.WriteLine("Welcome to {0} - Version {1}", Constants.STATUS_SERVER_NAME, Constants.STATUS_SERVER_VERSION);
            Console.WriteLine("Developed by {0}", Constants.STATUS_SERVER_DEVELOPERS);
            Console.WriteLine("-----------------------------------------------------");
            
            // Loading config.lua
            if(!ConfigManager.Load())
                return;

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
            Rsa.SetKey(RsaP, RsaQ);

            // Initializing Database connection
            if(!Database.Initialize())
                return;
            // Loading vocations
            if(!Vocation.Load())
                return;

            // Loading items




            if (ConfigManager.Instance[ConfigBool.USE_EXTERNAL_LOGIN_SERVER])
            {
                
            }
            else
            {
                _loginServer = new LoginServer.LoginServer();
                _loginServer.Start();
            }

            _gameServer.Start();

            while (true)
            {
                string command = Console.ReadLine() ?? string.Empty;

                if (_loginServer.CommandLineOperations.ContainsKey(command))
                    _loginServer.CommandLineOperations[command].Invoke();
                else
                {
                    Logger.Log(LogLevels.Warning, "Command is unknown!");
                }
            }
        }
    }
}
