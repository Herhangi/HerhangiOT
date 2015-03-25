﻿using System;
using System.IO;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ServerLibrary
{
    public class FileLoader
    {
        //Special Bytes
        private const byte NodeStart = 0xFE;
        private const byte NodeEnd = 0xFF;
        private const byte EscapeChar = 0xFD;

        private static readonly char[] GloballyAcceptedIdentifier = { '\0', '\0', '\0', '\0' };

        private byte[] _buffer;
        private FileStream _file;
        private BinaryReader _reader;
        private NodeStruct _root;

        public bool OpenFile(string fileName, string acceptedIdentifier)
        {
            try
            {
                _file = File.Open(fileName, FileMode.Open);
                _reader = new BinaryReader(_file);

                char[] identifier = _reader.ReadChars(4);
                if (identifier.MemCmp(GloballyAcceptedIdentifier, 4) != 0 && identifier.MemCmp(acceptedIdentifier.ToCharArray(), 4) != 0)
                {
                    _reader.Close();
                    _file.Close();
                    return false;
                }
            }
            catch (Exception)
            {
                _reader.Close();
                _file.Close();
                return false;
            }

            _root = null;
            _root = new NodeStruct();
            _root.Start = 4;

            if (_reader.ReadByte() == NodeStart)
                return ParseNode(_root);

            return false;
        }

        protected bool ParseNode(NodeStruct node)
        {
            int value;
            long pos;
	        NodeStruct currentNode = node;

            value = _file.ReadByte();
	        while (value != -1)
	        {
	            currentNode.Type = (byte)value;
		        bool setPropsSize = false;

		        while (true)
		        {
		            value = _file.ReadByte();

			        if (value == -1)
				        return false;

			        bool skipNode = false;

			        switch ((byte)value) {
				        case NodeStart:
			                pos = _file.Position;

                            NodeStruct childNode = new NodeStruct();
                            childNode.Start = pos;
			                currentNode.PropsSize = pos - currentNode.Start - 2;
			                currentNode.Child = childNode;

                            setPropsSize = true;
                            
                            if (!ParseNode(childNode))
                                return false;

                            break;

				        case NodeEnd:
					        //current node end
					        if (!setPropsSize)
					        {
					            pos = _file.Position;
						        currentNode.PropsSize = pos - currentNode.Start - 2;
					        }

			                value = _file.ReadByte();
			                if (value == -1)
			                    return true;

					        switch ((byte)value) {
						        case NodeStart:
							        //Starts next node
					                pos = _file.Position;

							        skipNode = true;
							        NodeStruct nextNode = new NodeStruct();
							        nextNode.Start = pos;
							        currentNode.Next = nextNode;
							        currentNode = nextNode;
							        break;

						        case NodeEnd:
							        //return safeTell(pos) && safeSeek(pos);
                                    _file.Seek(-1, SeekOrigin.Current);
					                return true;

						        default:
                                    //Invalid Format
							        return false;
					        }
					        break;

				        case EscapeChar:
			                value = _file.ReadByte();
			                if (value == -1) return false;
					        break;
			        }

			        if (skipNode) {
				        break;
			        }
		        }
	        }
	        return false;
        }

        public NodeStruct GetChildNode(NodeStruct parent, out byte type)
        {
            if (parent != null)
            {
                NodeStruct child = parent.Child;
                if (child != null)
                    type = child.Type;
                else
                    type = 0;

                return child;
            }

            type = _root.Type;
            return _root;
        }
        public NodeStruct GetNextNode(NodeStruct prev, out byte type)
        {
            type = 0;
	        if (prev == null)
		        return null;

            NodeStruct next = prev.Next;
	        if (next != null)
		        type = next.Type;

	        return next;
        }

        public bool GetProps(NodeStruct node, out BinaryReader props)
        {
            long size;
            byte[] buffer = GetProps(node, out size);

            if (buffer != null)
            {
                props = new BinaryReader(new MemoryStream(buffer, 0, (int)size));
                return true;
            }

            props = null;
            return false;
        }
        private byte[] GetProps(NodeStruct node, out long size)
        {
            size = 0;
            if (node == null)return null;

            if (_buffer == null || _buffer.Length < node.PropsSize)
            {
                _buffer = new byte[node.PropsSize];
            }

            _file.Seek(node.Start + 1, SeekOrigin.Begin);
            int readSize = _file.Read(_buffer, 0, (int)node.PropsSize);
            if (readSize < node.PropsSize) return null;

            uint j = 0;
            bool escaped = false;
            for (uint i = 0; i < node.PropsSize; ++i, ++j)
            {
                if (_buffer[i] == EscapeChar)
                {
                    //escape char found, skip it and write next
                    _buffer[j] = _buffer[++i];
                    //is neede a displacement for next bytes
                    escaped = true;
                }
                else if (escaped)
                {
                    _buffer[j] = _buffer[i];
                }
            }
            size = j;
            return _buffer;
        }
    }

    public class NodeStruct
    {
        public long Start = 0;
        public long PropsSize = 0;
        public byte Type = 0;
        public NodeStruct Next = null;
        public NodeStruct Child = null;

        public static void ClearNet(NodeStruct root)
        {
            if (root != null)
                ClearChild(root);
        }

        private static void ClearNext(NodeStruct node)
        {
            NodeStruct deleteNode = node;

            while (deleteNode != null)
            {
                if (deleteNode.Child != null)
                    ClearChild(deleteNode.Child);

                node.Child = null; //Clearing all references for GC

                NodeStruct nextNode = deleteNode.Next;
                deleteNode.Next = null; //Clearing all references for GC
                deleteNode = nextNode;
            }
        }

        private static void ClearChild(NodeStruct node)
        {
            if(node.Child != null)
                ClearChild(node.Child);

            node.Child = null; //Clearing all references for GC

            if(node.Next != null)
                ClearNext(node.Next);

            node.Next = null; //Clearing all references for GC
        }
    }
}