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

            bool forceCompilation = false;
            foreach (string arg in args)
            {
                if (arg.Equals("--force"))
                {
                    forceCompilation = true;
                    break;
                }
            }

            for (int i = 1; i < args.Length; i++)
            {
                if(args[i].StartsWith("--")) continue;

                switch (args[i])
                {
                    case "clo":
                        HerhangiOT.ScriptLibrary.ScriptManager.LoadCommandLineOperations(forceCompilation);
                        break;
                    default:
                        Logger.Log(LogLevels.Information, "Argument '"+args[i]+"' is invalid in 'reload' operation!");
                        break;
                }
            }
        }
    }
}
