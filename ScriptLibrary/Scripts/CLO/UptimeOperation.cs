using System;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ScriptLibrary.Scripts.CLO
{
    class UptimeOperation : CommandLineOperation
    {
        public override void Setup()
        {
            Command = "uptime";
        }

        public override void Operation(string[] args)
        {
            Console.WriteLine("System is online for {0:n1} seconds!", Tools.Uptime/1000.0);
        }
    }
}
