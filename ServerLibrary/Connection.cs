using System;
using System.Net.Sockets;
using HerhangiOT.ServerLibrary.Networking;
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
        protected bool IsSecretConnection { get; set; }
        protected bool IsEncryptionEnabled { get; set; }
        protected bool IsFirstMessageReceived { get; set; }
        protected OutputMessage OutputBuffer { get; private set; }

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

        protected void ParseHeader(IAsyncResult ar)
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

            uint recvChecksum = InMessage.GetUInt32(); //Adler Checksum
            uint checksum = Tools.AdlerChecksum(InMessage.Buffer, InMessage.Position, InMessage.Length - 6);
            if (checksum != recvChecksum)
                InMessage.SkipBytes(-4);   

            if (!IsFirstMessageReceived)
            {
                IsFirstMessageReceived = true;
                ProcessFirstMessage(checksum == recvChecksum);
            }
            else
                ProcessMessage();
        }

        public void Disconnect()
        {
            Stream.Close();
            Socket.Close();
        }

        public void Disconnect(string reason, uint version)
        {
            OutputMessage message = OutputMessagePool.GetOutputMessage(this, false);

            if(version > 1076)
                message.AddByte((byte)ServerPacketType.Disconnect1076);
            else
                message.AddByte((byte)ServerPacketType.Disconnect);
            message.AddString(reason);

            message.DisconnectAfterMessage = true;
            OutputMessagePool.AddToQueue(message);
        }

        public void DispatchDisconnect(string reason)
        {
            OutputMessage message = OutputMessagePool.GetOutputMessage(this, false);
            message.AddByte((byte)ServerPacketType.ErrorMessage);
            message.AddString(reason);

            message.DisconnectAfterMessage = true;
            OutputMessagePool.AddToQueue(message);
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
            Stream.BeginWrite(message.Buffer, message.HeaderPosition, message.Length, OnStreamWriteCompleted, message);
        }

        private void OnStreamWriteCompleted(IAsyncResult result)
        {
            OutputMessage message = (OutputMessage)result.AsyncState;
            Stream.EndWrite(result);

            if(message.DisconnectAfterMessage)
                Disconnect();

            if(message.IsRecycledMessage)
                OutputMessagePool.ReleaseMessage(message);
        }
        
        protected void WriteToOutputBuffer(NetworkMessage msg)
        {
	        OutputMessage @out = GetOutputBuffer(msg.Length);
	        if (@out != null) {
		        @out.Append(msg);
	        }
            NetworkMessagePool.ReleaseMessage(msg);
        }

        private OutputMessage GetOutputBuffer(int size)
        {
	        if (OutputBuffer != null && OutputMessage.MaxProtocolBodyLength >= OutputBuffer.Length + size)
		        return OutputBuffer;

            if (Socket != null)
            {
		        OutputBuffer = OutputMessagePool.GetOutputMessage(this);
		        return OutputBuffer;
	        }

            return null;
        }

        protected abstract void ProcessFirstMessage(bool isChecksummed);
        protected virtual void ProcessMessage() { }
    }
}
