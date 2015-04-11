using System;

namespace HerhangiOT.ServerLibrary
{
    public enum LogLevels { Error = 0, Operation = 1, Warning = 2, Information = 3, Debug = 4, Development = 5 }

    public static class Logger
    {
        private static int _operationStartedOn;
        public static LogLevels MinConsoleLogLevel = LogLevels.Information;

        public static void Log(LogLevels logLevel, string message)
        {
            if (logLevel <= MinConsoleLogLevel)
                Console.WriteLine("[{1:HH:mm:ss}]{0,-13}: {2}", "[" + logLevel + "]", DateTime.Now, message);
        }

        public static void Log(LogLevels logLevel, string format, params object[] arguments)
        {
            if (logLevel <= MinConsoleLogLevel)
                Console.WriteLine("[{1:HH:mm:ss}]{0,-13}: {2}", "[" + logLevel + "]", DateTime.Now, string.Format(format, arguments));
        }

        public static void LogOperationStart(string text)
        {
            if (LogLevels.Operation > MinConsoleLogLevel) return;

            _operationStartedOn = Environment.TickCount;
            Console.Write("[{0:HH:mm:ss}]: {1}...", DateTime.Now, text);
        }

        public static void LogOperationDone()
        {
            if (LogLevels.Operation > MinConsoleLogLevel) return;

            if (Console.WindowWidth - Console.CursorLeft - 14 < 0)
                Console.Write(new string('.', Console.WindowWidth - Console.CursorLeft));

            int elapsed = Environment.TickCount - _operationStartedOn;
            string text = String.Format("Done ({0:0.000}s)", elapsed / 1000.0);
            
            Console.Write(new string('.', Console.WindowWidth - Console.CursorLeft - 14));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void LogOperationFailed(string errorText = "")
        {
            //This has highest log level priority! Not checking!
            if (Console.WindowWidth - Console.CursorLeft - 14 < 0)
                Console.Write(new string('.', Console.WindowWidth - Console.CursorLeft));

            Console.Write(new string('.', Console.WindowWidth - Console.CursorLeft - 14));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error!");
            Console.ResetColor();
            if(!string.IsNullOrWhiteSpace(errorText))
                Console.WriteLine(errorText);
        }

        public static void LogOperationCached()
        {
            if (LogLevels.Operation > MinConsoleLogLevel) return;

            if (Console.WindowWidth - Console.CursorLeft - 16 < 0)
                Console.Write(new string('.', Console.WindowWidth - Console.CursorLeft));

            int elapsed = Environment.TickCount - _operationStartedOn;
            string text = String.Format("Cached ({0:0.000}s)", elapsed / 1000.0);

            Console.Write(new string('.', Console.WindowWidth - Console.CursorLeft - 16));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
