using System.Collections.Generic;

namespace HerhangiOT.ServerLibrary.Networking
{
    public class NetworkMessagePool
    {
        private static object _emptyQueueLock;
        private static Queue<NetworkMessage> _emptyMessageQueue;

        public static void Initialize()
        {
            _emptyQueueLock = new object();
            _emptyMessageQueue = new Queue<NetworkMessage>();
            for (int i = 0; i < Constants.NetworkMessagePoolSize; i++)
            {
                _emptyMessageQueue.Enqueue(new NetworkMessage());
            }
        }

        public static NetworkMessage GetEmptyMessage()
        {
            NetworkMessage message;

            lock (_emptyQueueLock)
            {
                if (_emptyMessageQueue.Count == 0)
                {
                    for (int i = 0; i < Constants.NetworkMessagePoolExpansionSize; i++)
                    {
                        _emptyMessageQueue.Enqueue(new NetworkMessage());
                    }
                }

                message = _emptyMessageQueue.Dequeue();
            }

            return message;
        }

        public static void ReleaseMessage(NetworkMessage msg)
        {
            msg.Reset();

            lock (_emptyQueueLock)
            {
                _emptyMessageQueue.Enqueue(msg);
            }
        }
    }
}
