using System.Collections.Generic;
using System.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ServerLibrary.Networking
{
    public class OutputMessagePool
    {
        private static object _emptyQueueLock;
        private static object _waitingQueueLock;

        private static Queue<OutputMessage> _emptyMessageQueue;
        private static Deque<OutputMessage> _waitingMessageQueue;

        protected static Thread Thread;
        protected static AutoResetEvent Signaler;

        public static void Initialize()
        {
            _emptyMessageQueue = new Queue<OutputMessage>();
            for (int i = 0; i < Constants.OutputMessagePoolSize; i++)
            {
                _emptyMessageQueue.Enqueue(new OutputMessage());
            }

            _waitingMessageQueue = new Deque<OutputMessage>();
            _emptyQueueLock = new object();
            _waitingQueueLock = new object();

            Signaler = new AutoResetEvent(false);
            Thread = new Thread(new ThreadStart(ProcessWaitingQueue));
            Thread.Start();
        }

        public static OutputMessage GetOutputMessage(Connection connection, bool autosend = true)
        {
            OutputMessage message;

            lock (_emptyQueueLock)
            {
                if (_emptyMessageQueue.Count == 0)
                {
                    for (int i = 0; i < Constants.OutputMessagePoolExpansionSize; i++)
                    {
                        _emptyMessageQueue.Enqueue(new OutputMessage());
                    }
                }

                message = _emptyMessageQueue.Dequeue();
            }

            message.MessageTarget = connection;
            return message;
        }

        public static void ReleaseMessage(OutputMessage msg)
        {
            lock (_emptyQueueLock)
            {
                _emptyMessageQueue.Enqueue(msg);
            }
        }

        public static void SendImmediately(OutputMessage msg)
        {
            msg.MessageTarget.Send(msg);

            ReleaseMessage(msg);
        }

        public static void AddToQueue(OutputMessage msg, bool pushFront = false)
        {
            bool sendSignal;

            lock (_waitingQueueLock)
            {
                sendSignal = (_waitingMessageQueue.Count == 0);

                if(pushFront)
                    _waitingMessageQueue.AddToFront(msg);
                else
                    _waitingMessageQueue.AddToBack(msg);
            }

            if (sendSignal)
                Signaler.Set();
        }

        private static void ProcessWaitingQueue()
        {
            while (true)
            {
                OutputMessage msg = null;
                if (_waitingMessageQueue.Count == 0)
                {
                    Signaler.WaitOne();
                }

                if (_waitingMessageQueue.Count != 0)
                {
                    msg = _waitingMessageQueue.RemoveFromFront();
                }
                else continue;

                //if (!msg.IsExpired())
                {
                    //outputPool->startExecutionFrame();
                    msg.MessageTarget.Send(msg);

                    //MESSAGE RELEASE AND CLIENT DISCONNECT MOVED TO STREAM END
                    //if (msg.DisconnectAfterMessage) msg.MessageTarget.Disconnect();
                    //ReleaseMessage(msg);

                    //outputPool->sendAll();

                    //g_game.clearSpectatorCache();
                }
            }
        }
    }

    //!!
    // USING CIRCULAR BUFFER MIGHT BE USEFUL
    //!!
}
