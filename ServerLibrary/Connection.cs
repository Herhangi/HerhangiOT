using System;
using System.Net.Sockets;
using HerhangiOT.ServerLibrary.Networking;
using HerhangiOT.ServerLibrary.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ServerLibrary
{
    public abstract class Connection
    {
        public Socket Socket { get; set; }
        public NetworkStream Stream { get; set; }
        public NetworkMessage InMessage { get; set; }

        protected uint[] XteaKey { get; set; }
        protected bool IsChecksumEnabled { get; set; }
        protected bool IsEncryptionEnabled { get; set; }

        public virtual void HandleFirstConnection(IAsyncResult ar)
        {
            TcpListener clientListener = (TcpListener)ar.AsyncState;
            Socket = clientListener.EndAcceptSocket(ar);
            Stream = new NetworkStream(Socket);
            InMessage = new NetworkMessage();
            IsChecksumEnabled = true;
            XteaKey = new uint[4];

            Stream.BeginRead(InMessage.Buffer, 0, 2, ParseHeader, null);
        }

        private void ParseHeader(IAsyncResult ar)
        {
            int currentlyRead = Stream.EndRead(ar);
            if (currentlyRead == 0)
                Disconnect();

            int size = BitConverter.ToUInt16(InMessage.Buffer, 0) + 2;
            if(size <= 0 || size >= Constants.NETWORKMESSAGE_ERRORMAXSIZE)
                Disconnect();

            //TODO: MAX PACKETS PER SECOND CHECK

            while (currentlyRead < size)
            {
                if (Stream.CanRead)
                    currentlyRead += Stream.Read(InMessage.Buffer, currentlyRead, size - currentlyRead);
                else
                {
                    Disconnect();
                    return;
                }
            }
            InMessage.Reset(2, size);

            //I won't do checksum control as other servers do not care about it
            uint recvChecksum = InMessage.GetUInt32(); //Adler Checksum
            uint checksum = Tools.AdlerChecksum(InMessage.Buffer, InMessage.Position, InMessage.Length - 6);
            if (checksum != recvChecksum)
                InMessage.SkipBytes(-4);
            
            ProcessMessage();
        }

        protected void Disconnect()
        {
            Stream.Close();
            Socket.Close();
        }

        protected void Disconnect(string reason)
        {
            OutputMessage message = new OutputMessage();
            message.AddByte((byte)ServerPacketType.Disconnect);
            message.AddString(reason);
            
            Send(message);

            Disconnect();
        }

        protected void DispatchDisconnect(string reason)
        {
            OutputMessage message = new OutputMessage();
            message.AddByte((byte)ServerPacketType.ErrorMessage);
            message.AddString(reason);

            DispatcherManager.NetworkDispatcher.AddTask(new Task(() => Send(message)));
            DispatcherManager.NetworkDispatcher.AddTask(new Task(Disconnect));
        }

        public bool Send(OutputMessage message)
        {
            PrepareMessageToSend(message);
            InternalSend(message);

            return true;
        }

        private void PrepareMessageToSend(OutputMessage message)
        {
            message.WriteMessageLength();

            if (IsEncryptionEnabled)
            {
                Tools.EncryptXtea(message, XteaKey);
                message.AddCryptoHeader(IsChecksumEnabled); 
            }
        }

        private void InternalSend(OutputMessage message)
        {
            Stream.BeginWrite(message.Buffer, message.HeaderPosition, message.Length, null, null);
        }


        protected abstract void ProcessMessage();
    }
}
