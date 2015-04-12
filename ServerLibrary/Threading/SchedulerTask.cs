using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ServerLibrary.Threading
{
    public class SchedulerTask : Task
    {
        public const int SchedulerMinTicks = 50;

        public uint TaskId { get; set; }

        public SchedulerTask(uint time, Action action) : base(time, action) { }

        public int GetCycle()
        {
            return (int)(Expiration - Tools.GetSystemMilliseconds());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SchedulerTask CreateSchedulerTask(uint delay, Action action)
        {
            return new SchedulerTask(Math.Max(delay, SchedulerMinTicks), action);
        }

        public class SchedulerTaskComparer : IComparer<SchedulerTask>
        {
            public int Compare(SchedulerTask x, SchedulerTask y)
            {
                if (x.TaskId == y.TaskId) return 0;
                return (x.Expiration > y.Expiration) ? -1 : 1;
            }
        }
    }
}
