using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace HerhangiOT.ServerLibrary.Threading
{
    public enum DispatcherState
    {
        Running,
        Closing,
        Terminated
    }

    public class Dispatcher
    {
        protected Thread Thread;
        protected object TaskLock;
        protected List<Task> TaskList;
        protected DispatcherState State;

        protected AutoResetEvent Signaler;

        public Dispatcher()
        {
            TaskLock = new object();
            TaskList = new List<Task>();
            State = DispatcherState.Terminated;
        }

        public void AddTask(Task task, bool pushFront = false)
        {
            bool needSignal = false;

            lock (TaskLock)
            {
                if (State == DispatcherState.Running)
                {
                    needSignal = TaskList.Count == 0;

                    if (pushFront)
                    {
                        TaskList.Insert(0, task);
                    }
                    else
                    {
                        TaskList.Add(task);
                    }
                }
                else
                {
                    //DISPOSE TASK
                }
            }

            if (needSignal)
            {
                Signaler.Set();
            }
        }

        public void Start()
        {
            State = DispatcherState.Running;
            Thread = new Thread(new ThreadStart(DispatcherThread));
            Thread.Start();
            Signaler = new AutoResetEvent(false);
        }

        public void Stop()
        {
            lock (TaskLock)
            {
                State = DispatcherState.Closing;
            }
        }

        public void Shutdown()
        {
            lock (TaskLock)
            {
                State = DispatcherState.Terminated;
                Flush();
            }

            Signaler.Set();
        }

        public void Join()
        {
            Thread.Join();
        }

        protected void DispatcherThread()
        {
            //OutputMessagePool* outputPool = OutputMessagePool::getInstance();

            while (State != DispatcherState.Terminated)
            {
                Task task = null;
                Console.WriteLine("1");
                if (TaskList.Count == 0)
                {
                    Signaler.WaitOne();
                }
                Console.WriteLine("2");

                if (TaskList.Count != 0 && (State != DispatcherState.Terminated))
                {
                    task = TaskList[0];
                    TaskList.RemoveAt(0);
                }

                if (!task.IsExpired())
                {
                    //outputPool->startExecutionFrame();
                    task._action.Invoke();
                    //outputPool->sendAll();

                    //g_game.clearSpectatorCache();
                }
            }
        }

        protected void Flush()
        {

        }
    }

    // WE WILL HAVE 2 DISPATCHERS ONE FOR DATABASE OPERATIONS OTHER FOR GAME OPERATIONS
    // CREATING NEW TASK: new Task(0, () => METHODNAME());

}
