namespace HerhangiOT.ServerLibrary.Threading
{
    public class DispatcherManager
    {
        public static Dispatcher GameDispatcher;
        public static Dispatcher NetworkDispatcher;
        public static Dispatcher DatabaseDispatcher;

        public static void Start()
        {
            GameDispatcher = new Dispatcher();
            GameDispatcher.Start();

            NetworkDispatcher = new Dispatcher();
            NetworkDispatcher.Start();

            DatabaseDispatcher = new Dispatcher();
            DatabaseDispatcher.Start();
        }
    }
}
