using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HerhangiOT.ServerLibrary.Networking
{
    public class OutputMessagePool
    {
        private static Queue<OutputMessage> _emptyMessageQueue;
        private static Queue<OutputMessage> _waitingMessageQueue;

        public static void Initialize()
        {
            _emptyMessageQueue = new Queue<OutputMessage>();
            for (int i = 0; i < Constants.OutputMessagePoolSize; i++)
            {
                _emptyMessageQueue.Enqueue(new OutputMessage());
            }

            _waitingMessageQueue = new Queue<OutputMessage>();
        }

        public static OutputMessage GetOutputMessage(bool autoSend = false)
        {
            if (_emptyMessageQueue.Count == 0)
            {
                for (int i = 0; i < Constants.OutputMessagePoolExpansionSize; i++)
                {
                    _emptyMessageQueue.Enqueue(new OutputMessage());
                }
            }

            return _emptyMessageQueue.Dequeue();
        }

        public static void ReleaseMessage(OutputMessage msg)
        {
            _emptyMessageQueue.Enqueue(msg);
        }
    }

    //!!
    // USING CIRCULAR BUFFER MIGHT BE USEFUL
    //!!


    // ADD OutputMessagePoolSize = 100 to Constants.cs
    // ADD OutPutMessagePoolExpansionSize = 10 to Constants.cs

}
