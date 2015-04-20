using System.Threading;
using System.Collections.Generic;

namespace HerhangiOT.ServerLibrary.Threading
{
    public class Scheduler
    {
        protected HashSet<uint> ActiveEventIds { get; set; }
        protected DispatcherState State { get; set; }
        protected uint LastEventId { get; set; }
        protected SortedSet<SchedulerTask> EventList { get; set; }

        protected Thread Thread;
        protected object EventLock;

        public Scheduler()
        {
            LastEventId = 0;
            EventLock = new object();
            ActiveEventIds = new HashSet<uint>();
            EventList = new SortedSet<SchedulerTask>(new SchedulerTask.SchedulerTaskComparer());
            State = DispatcherState.Terminated;
        }

        public uint AddEvent(SchedulerTask task)
        {
            lock (EventLock)
            {
                bool needSignal;
                if (State == DispatcherState.Running)
                {
                    needSignal = EventList.Count == 0;

                    // check if the event has a valid id
                    if (task.TaskId == 0)
                    {
                        // if not generate one
                        if (++LastEventId == 0)
                            LastEventId = 1;

                        task.TaskId = LastEventId;
                    }

                    // insert the event id in the list of active events
                    ActiveEventIds.Add(LastEventId);

                    // add the event to the queue
                    EventList.Add(task);
                }
                else
                {
                    return 0;
                }

                if (needSignal)
                    Monitor.Pulse(EventLock);
            }

            return task.TaskId;
        }

        public bool StopEvent(uint eventId)
        {
            if (eventId == 0)
                return false;

            lock (EventLock)
            {
                return ActiveEventIds.Remove(eventId);
            }
        }

        public void Start()
        {
            State = DispatcherState.Running;
            Thread = new Thread(SchedulerThread);
            Thread.Start();
        }

        public void Stop()
        {
            lock (EventLock)
            {
                State = DispatcherState.Closing;
            }
        }

        public void Shutdown()
        {
            lock (EventLock)
            {
                State = DispatcherState.Terminated;

                EventList.Clear();
                ActiveEventIds.Clear();

                Monitor.Pulse(EventLock);
            }
        }

        public void Join()
        {
            Thread.Join();
        }

        protected void SchedulerThread()
        {
            while (State != DispatcherState.Terminated)
            {
                bool isEarlyWakening = true;
                Monitor.Enter(EventLock);

                if (EventList.Count == 0)
                    Monitor.Wait(EventLock);
                else
                {
                    int waitTime = EventList.Min.GetCycle();

                    if (waitTime > 0)
                        isEarlyWakening = Monitor.Wait(EventLock, waitTime);
                    else
                        isEarlyWakening = false;
                }

                if (!isEarlyWakening && State != DispatcherState.Terminated)
                {
                    SchedulerTask task = EventList.Min; // Get task with lowest expiration value
                    EventList.Remove(task);

                    if (!ActiveEventIds.Remove(task.TaskId)) // Is task still active
                    {
                        Monitor.Exit(EventLock);
                        continue;
                    }

                    Monitor.Exit(EventLock);
                    task.SetNotToExpire();
                    DispatcherManager.GameDispatcher.AddTask(task, true);
                }
                else
                {
                    Monitor.Exit(EventLock);
                }
            }
        }
    }
}
