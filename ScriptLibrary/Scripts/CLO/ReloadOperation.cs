using System;
using HerhangiOT.ServerLibrary;

namespace HerhangiOT.ScriptLibrary.Scripts.CLO
{
    public class ReloadOperation : CommandLineOperation
    {
        public override void Setup()
        {
            Command = "reload";
        }

        public override void Operation(string[] args)
        {
            if (args.Length < 2)
            {
                Logger.Log(LogLevels.Warning, "Invalid number of arguments for 'reload' operation!");
                return;
            }

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "clo":
                        HerhangiOT.ScriptLibrary.ScriptManager.LoadCommandLineOperations();
                        break;
                }
            }
        }
    }
}
