using System;
using HerhangiOT.ScriptLibrary;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.LoginServer
{
    class Program
    {
        private static readonly LoginServer LoginServer = new LoginServer();

        private const string RsaP = "14299623962416399520070177382898895550795403345466153217470516082934737582776038882967213386204600674145392845853859217990626450972452084065728686565928113";
        private const string RsaQ = "7630979195970404721891201847792002125535401292779123937207447574596692788513647179235335529307251350570728407373705564708871762033017096809910315212884101";

        static void Main(string[] args)
        {
            Tools.Initialize();

            ConfigManager.Load("config_login.lua");
            Rsa.SetKey(RsaP, RsaQ);

            LoginServer.Start();

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
                        Logger.Log(LogLevels.Warning, "Command '" + command[0] + "' could not be executed in this environment!");
                    }
                }
                else
                {
                    Logger.Log(LogLevels.Warning, "Command is unknown!");
                }
            }
        }
    }
}
