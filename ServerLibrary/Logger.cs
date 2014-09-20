using System;

namespace HerhangiOT.ServerLibrary
{
    public enum LogLevels { Error = 0, Operation = 1, Warning = 2, Information = 3, Debug = 4 }

    public static class Logger
    {
        private static int _operationStartedOn;

        public static void Log(LogLevels logLevel, string message)
        {
            Console.WriteLine("[{1:HH:mm:ss}]{0,-13}: {2}", "[" + logLevel + "]", DateTime.Now, message);
        }

        public static void LogOperationStart(string text)
        {
            _operationStartedOn = Environment.TickCount;
            Console.Write("[{0:HH:mm:ss}]: {1}...", DateTime.Now, text);
        }

        public static void LogOperationDone()
        {
            int elapsed = Environment.TickCount - _operationStartedOn;
            string text = String.Format("Done ({0:0.000}s)", elapsed / 1000.0);
            
            Console.Write(new string('.', Console.WindowWidth - Console.CursorLeft - 14));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void LogOperationFailed(string errorText = "")
        {
            Console.Write(new string('.', Console.WindowWidth - Console.CursorLeft - 14));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error!");
            Console.ResetColor();
            if(!string.IsNullOrWhiteSpace(errorText))
                Console.WriteLine(errorText);
        }
    }
}
