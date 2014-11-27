using System;

namespace HerhangiOT.ServerLibrary.Threading
{
    public class Task
    {
        private int _expiration;
        public Action _action;

        public Task(int time, Action action)
        {
            _expiration = Environment.TickCount + time;
            _action = action;
        }

        public Task(Action action)
        {
            _expiration = -1;
            _action = action;
        }

        public bool IsExpired()
        {
            if (_expiration == -1) return false;
            return (_expiration > Environment.TickCount);
        }
    }
}
