﻿using System;
using System.Runtime.CompilerServices;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ServerLibrary.Threading
{
    public class Task
    {
        public long Expiration { get; protected set; }
        public Action Action { get; protected set; }

        public Task(uint time, Action action)
        {
            Expiration = Tools.GetSystemMilliseconds() + time;
            Action = action;
        }

        public Task(Action action)
        {
            Expiration = -1;
            Action = action;
        }

        public void SetNotToExpire()
        {
            Expiration = -1;
        }

        public bool IsExpired()
        {
            if (Expiration == -1) return false;
            return (Expiration < Tools.GetSystemMilliseconds());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task CreateTask(uint expiration, Action action)
        {
            return new Task(expiration, action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task CreateTask(Action action)
        {
            return new Task(action);
        }
    }
}
