using System;
using HerhangiOT.ScriptLibrary;

namespace HerhangiOT.GameServer.Scripts.CLO
{
    public class ExitCommand : CommandLineOperation
    {
        public override void Setup()
        {
            Command = "exit";
            IsUsableInGameServer = true;
            IsUsableInGameServer = true;
        }

        public override void Operation()
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
    }
}
