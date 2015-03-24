using System;
using System.IO;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.ServerLibrary
{
    public class FileLoader
    {
        private static readonly char[] GloballyAcceptedIdentifier = new[] { '\0', '\0', '\0', '\0' };

        private FileStream _file;
        private BinaryReader _reader;

        public bool OpenFile(string fileName, string acceptedIdentifier)
        {
            try
            {
                _file = File.Open(fileName, FileMode.Open);
                _reader = new BinaryReader(_file);

                char[] identifier = _reader.ReadChars(4);
                if (identifier.MemCmp(GloballyAcceptedIdentifier, 4) != 0 && identifier.MemCmp(identifier, 4) != 0)
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


            uint version = _reader.ReadUInt32();

            root = new Node();
            root.Start = 4;

            if (_reader.ReadByte() == Constants.NodeStart)
            {
                return ParseNode(root);
            }
            else
            {
                return false;
            }
        }
    }

    public class NodeStruct
    {
        public uint Start = 0;
        public uint PropsSize = 0;
        public uint Type = 0;
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
            NodeStruct nextNode;

            while (deleteNode != null)
            {
                if (deleteNode.Child != null)
                    ClearChild(deleteNode.Child);

                node.Child = null; //Clearing all references for GC

                nextNode = deleteNode.Next;
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
