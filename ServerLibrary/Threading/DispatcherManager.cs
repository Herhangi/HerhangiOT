namespace HerhangiOT.ServerLibrary.Threading
{
    public class DispatcherManager
    {
        public static Dispatcher GameDispatcher;
        public static Dispatcher NetworkDispatcher;
        public static Dispatcher DatabaseDispatcher;
        public static Scheduler Scheduler;
        public static JobManager Jobs;

        public static void Start()
        {
            Scheduler = new Scheduler();
            Scheduler.Start();

            GameDispatcher = new Dispatcher();
            GameDispatcher.Start();

            NetworkDispatcher = new Dispatcher();
            NetworkDispatcher.Start();

            DatabaseDispatcher = new Dispatcher();
            DatabaseDispatcher.Start();

            Jobs = new JobManager();
            Jobs.Start();
        }
    }
}
