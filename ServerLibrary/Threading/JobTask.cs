using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ServerLibrary.Threading
{
    public class JobTask : Task
    {
        public string JobName { get; set; }
        public uint FirstDelay { get; set; }
        public uint Interval { get; set; }
        public long NextInvocation { get; set; }

        public JobTask(uint interval, uint firstDelay, Action action)
            : base(interval, action)
        {
            FirstDelay = firstDelay;
            Interval = interval;
            Expiration = -1;

            NextInvocation = Tools.GetSystemMilliseconds() + FirstDelay;
        }

        public void CalculateNextInvocation()
        {
            NextInvocation = Tools.GetSystemMilliseconds() + Interval;
        }

        public int GetCycle()
        {
            return (int)(NextInvocation - Tools.GetSystemMilliseconds());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobTask CreateJobTask(uint interval, uint firstDelay, Action action)
        {
            return new JobTask(interval, firstDelay, action);
        }

        public class JobTaskComparer : IComparer<JobTask>
        {
            public int Compare(JobTask x, JobTask y)
            {
                if (string.Equals(x.JobName, y.JobName)) return 0;
                return (x.Expiration > y.Expiration) ? -1 : 1;
            }
        }
    }
}
