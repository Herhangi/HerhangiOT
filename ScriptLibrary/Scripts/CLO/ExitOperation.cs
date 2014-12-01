using System;

namespace HerhangiOT.ScriptLibrary.Scripts.CLO
{
    public class ExitOperation : CommandLineOperation
    {
        public override void Setup()
        {
            Command = "exit";
        }

        public override void Operation(string[] args)
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
