using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        protected int PendingWrites { get; set; }
        protected bool IsChecksumEnabled { get; set; }
        protected bool IsSecretConnection { get; set; }
        protected bool IsEncryptionEnabled { get; set; }
        protected bool IsFirstMessageReceived { get; set; }
        protected OutputMessage OutputBuffer { get; private set; }
        protected ConcurrentQueue<OutputMessage> PendingMessages { get; private set; }

        public virtual void HandleFirstConnection(IAsyncResult ar)
        {
            try
            {
                TcpListener clientListener = (TcpListener)ar.AsyncState;
                Socket = clientListener.EndAcceptSocket(ar);
                Stream = new NetworkStream(Socket);
                InMessage = new NetworkMessage();
                IsChecksumEnabled = true;
                XteaKey = new uint[4];

                PendingMessages = new ConcurrentQueue<OutputMessage>();
                Stream.BeginRead(InMessage.Buffer, 0, 2, ParseHeader, null);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected virtual void ParseHeader(IAsyncResult ar)
        {
            if (!EndRead(ar))
            {
                return;
            }

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
            {
                if(IsEncryptionEnabled && !InMessage.XteaDecrypt(XteaKey))
                    return;

                ProcessMessage();
            }
        }

        protected bool EndRead(IAsyncResult ar)
        {
            try
            {
                int currentlyRead = Stream.EndRead(ar);
                if (currentlyRead == 0)
                {
                    Disconnect();
                    return false;
                }

                int size = BitConverter.ToUInt16(InMessage.Buffer, 0) + 2;
                if (size <= 0 || size >= Constants.NETWORKMESSAGE_ERRORMAXSIZE)
                {
                    Disconnect();
                    return false;
                }

                //TODO: MAX PACKETS PER SECOND CHECK

                while (currentlyRead < size)
                {
                    if (Stream.CanRead)
                        currentlyRead += Stream.Read(InMessage.Buffer, currentlyRead, size - currentlyRead);
                    else
                    {
                        Disconnect();
                        return false;
                    }
                }

                InMessage.Reset(2, size);
                return true;
            }
            catch (Exception)
            {
                Disconnect();
                return false;
            }
        }

        public virtual void Disconnect()
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
        protected virtual void DispatchDisconnect(string reason)
        {
            OutputMessage message = OutputMessagePool.GetOutputMessage(this, false);
            message.AddByte((byte)ServerPacketType.ErrorMessage);
            message.AddString(reason);

            message.DisconnectAfterMessage = true;
            OutputMessagePool.AddToQueue(message);
        }

        public bool Send(OutputMessage message)
        {
            if (message == OutputBuffer)
                OutputBuffer = null;

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

        private void InternalSend(OutputMessage message, bool wasPending = false)
        {
            if(wasPending || PendingWrites++ == 0)
                Stream.BeginWrite(message.Buffer, message.HeaderPosition, message.Length, OnStreamWriteCompleted, message);
            else
                PendingMessages.Enqueue(message);
        }

        private void OnStreamWriteCompleted(IAsyncResult result)
        {
            OutputMessage message = (OutputMessage)result.AsyncState;

            try
            {
                Stream.EndWrite(result);

                if (--PendingWrites > 0)
                {
                    OutputMessage pendingMessage;
                    if (PendingMessages.TryDequeue(out pendingMessage))
                    {
                        InternalSend(pendingMessage, true); 
                        return;
                    }
                }

                if (message.DisconnectAfterMessage)
                    Disconnect();
            }
            finally
            {
                if (message.IsRecycledMessage)
                    OutputMessagePool.ReleaseMessage(message);
            }
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
                if(OutputBuffer != null)
                    OutputMessagePool.AddToQueue(OutputBuffer);
		        OutputBuffer = OutputMessagePool.GetOutputMessage(this);
		        return OutputBuffer;
	        }

            return null;
        }

        protected abstract void ProcessFirstMessage(bool isChecksummed);
        protected virtual void ProcessMessage() { }
    }
}
