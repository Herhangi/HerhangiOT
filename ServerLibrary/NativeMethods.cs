using System.Runtime.InteropServices;

namespace HerhangiOT.ServerLibrary
{
    public enum ConsoleCtrlEvents
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT,
        CTRL_CLOSE_EVENT,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT
    }

    public class NativeMethods
    {
        public delegate void ConsoleCtrlHandlerDelegate(ConsoleCtrlEvents CtrlEvent);

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate Handler, bool Add);
    }
}
