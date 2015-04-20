using System;

namespace HerhangiOT.ScriptLibrary.Scripts.CLO
{
    public class ClearOperation : CommandLineOperation
    {
        public override void Setup()
        {
            Command = "clear";
        }

        public override void Operation(string[] args)
        {
            Console.Clear();
            Console.WriteLine("Welcome to {0} - Version {1}", HerhangiOT.ServerLibrary.Constants.ServerName, HerhangiOT.ServerLibrary.Constants.ServerVersion);
            Console.WriteLine("Developed by {0}", HerhangiOT.ServerLibrary.Constants.ServerDevelopers);
            Console.WriteLine("-----------------------------------------------------");
        }
    }
}
