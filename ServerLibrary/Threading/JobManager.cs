using System.Collections.Generic;
using System.Threading;

namespace HerhangiOT.ServerLibrary.Threading
{
    public class JobManager
    {
        protected DispatcherState State { get; set; }
        protected HashSet<string> ActiveJobs { get; set; }
        protected SortedSet<JobTask> JobList { get; set; }

        protected Thread Thread;
        protected object JobLock;

        public JobManager()
        {
            JobLock = new object();
            ActiveJobs = new HashSet<string>();
            JobList = new SortedSet<JobTask>(new JobTask.JobTaskComparer());
            State = DispatcherState.Terminated;
        }

        public void AddJob(string jobName, JobTask job)
        {
            lock (JobLock)
            {
                bool needSignal = false;

                if (State == DispatcherState.Running)
                {
                    needSignal = JobList.Count == 0;

                    job.JobName = jobName;
                    ActiveJobs.Add(jobName);
                    JobList.Add(job);
                }

                if (needSignal)
                    Monitor.Pulse(JobLock);
            }
        }

        public bool StopJob(string jobName)
        {
            lock (JobLock)
            {
                return ActiveJobs.Remove(jobName);
            }
        }

        public void Start()
        {
            State = DispatcherState.Running;
            Thread = new Thread(JobManagerThread);
            Thread.Start();
        }

        public void Stop()
        {
            lock (JobLock)
            {
                State = DispatcherState.Closing;
            }
        }

        public void Shutdown()
        {
            lock (JobLock)
            {
                State = DispatcherState.Terminated;

                JobList.Clear();
                ActiveJobs.Clear();

                Monitor.Pulse(JobLock);
            }
        }

        public void Join()
        {
            Thread.Join();
        }

        protected void JobManagerThread()
        {
            while (State != DispatcherState.Terminated)
            {
                bool isEarlyWakening = true;
                Monitor.Enter(JobLock);

                if (JobList.Count == 0)
                    Monitor.Wait(JobLock);
                else
                {
                    int waitTime = JobList.Min.GetCycle();

                    if (waitTime > 0)
                        isEarlyWakening = Monitor.Wait(JobLock, waitTime);
                    else
                        isEarlyWakening = false;
                }

                if (!isEarlyWakening && State != DispatcherState.Terminated)
                {
                    JobTask task = JobList.Min; // Get job with lowest expiration value

                    if (!ActiveJobs.Contains(task.JobName)) // Is job still active
                    {
                        JobList.Remove(task);
                        Monitor.Exit(JobLock);
                        continue;
                    }

                    Monitor.Exit(JobLock);
                    task.CalculateNextInvocation();
                    DispatcherManager.GameDispatcher.AddTask(task, true);
                }
                else
                {
                    Monitor.Exit(JobLock);
                }
            }
        }
    }
}
