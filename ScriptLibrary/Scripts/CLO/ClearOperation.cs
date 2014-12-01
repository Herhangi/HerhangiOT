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
            Console.WriteLine("Welcome to {0} - Version {1}", HerhangiOT.ServerLibrary.Constants.STATUS_SERVER_NAME, HerhangiOT.ServerLibrary.Constants.STATUS_SERVER_VERSION);
            Console.WriteLine("Developed by {0}", HerhangiOT.ServerLibrary.Constants.STATUS_SERVER_DEVELOPERS);
            Console.WriteLine("-----------------------------------------------------");
        }
    }
}
