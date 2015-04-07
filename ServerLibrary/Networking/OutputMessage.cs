using System;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ServerLibrary.Networking
{
    public class OutputMessage : NetworkMessage
    {
        #region Constants
        public const int HeaderLength = 2;
        public const int CryptoLength = 4;
        public const int XteaMultiple = 8;
        public const int MaxBodyLength = Constants.NETWORKMESSAGE_MAXSIZE - HeaderLength - CryptoLength - XteaMultiple;
        public const int MaxProtocolBodyLength = MaxBodyLength - 10;
        #endregion

        private int _headerPosition;
        /// <summary>
        /// Current position of header cursor 
        /// </summary>
        public int HeaderPosition
        {
            get { return _headerPosition; }
            protected set { _headerPosition = value; }
        }
        public Connection MessageTarget;
        public bool DisconnectAfterMessage;
        public bool IsRecycledMessage = true;

        public OutputMessage()
        {
            FreeMessage();
        }
        
        public void FreeMessage()
        {
            //allocate enough size for headers
            //2 bytes for unencrypted message size
            //4 bytes for checksum
            //2 bytes for encrypted message size
            _headerPosition = 8;
            MessageTarget = null;
            DisconnectAfterMessage = false;

            Reset(8);
        }

        public void WriteMessageLength()
        {
            AddHeaderUInt16((ushort)Length);
        }

        public void AddCryptoHeader(bool addChecksum)
        {
            if (addChecksum)
            {
                AddHeaderUInt32(Tools.AdlerChecksum(Buffer, 6, Length));
            }

            AddHeaderUInt16((ushort)Length);
        }

        #region Add To Header
        protected void AddHeaderBytes(byte[] value)
        {
            if (value.Length > _headerPosition)
            {
                Logger.Log(LogLevels.Error, "OutputMessage AddHeader buffer is full!");
                return;
            }

            _headerPosition -= value.Length;
            Array.Copy(value, 0, Buffer, _headerPosition, value.Length);
            Length += value.Length;
        }

        protected void AddHeaderUInt32(uint value)
        {
            AddHeaderBytes(BitConverter.GetBytes(value));
        }

        protected void AddHeaderUInt16(ushort value)
        {
            AddHeaderBytes(BitConverter.GetBytes(value));
        }
        #endregion

        public void Append(NetworkMessage msg) {
			int msgLen = msg.Length;
            Buffer.MemCpy(Position, msg.Buffer, msgLen);
			Length += msgLen;
			Position += msgLen;
		}

        public void AddPaddingBytes(int count)
        {
            Buffer.MemSet(Position, (byte)ServerPacketType.PaddingByte, count);
            Length += count;
            Position += count;
        }
    }
}