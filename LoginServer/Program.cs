using System;
using System.Collections.Generic;
using System.Linq;
using HerhangiOT.ScriptLibrary;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Database;
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
            Console.WriteLine("Standalone Login Server is not yet supported!");
            Console.ReadKey();
            return;

            Tools.Initialize();

            ConfigManager.Load("config_login.lua");
            Rsa.SetKey(RsaP, RsaQ);

            Database.Initialize();

            LoginServer.Start();

            while (true)
            {
                string input = Console.ReadLine();

                if (input == null) continue;

                string[] firstPass = input.Split('"');
                List<string> secondPass = firstPass[0].Trim().Split(' ').ToList();

                if (firstPass.Length > 1)
                {
                    for (int i = 1; i < firstPass.Length; i++)
                    {
                        if (i % 2 == 1)
                        {
                            secondPass.Add(firstPass[i]);
                        }
                        else
                        {
                            secondPass.AddRange(firstPass[i].Trim().Split(' '));
                        }
                    }
                }
                string[] command = secondPass.ToArray();

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
