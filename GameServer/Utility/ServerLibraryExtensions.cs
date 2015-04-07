using HerhangiOT.GameServer.Model;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.GameServer.Utility
{
    public static class ServerLibraryExtensions
    {
        public static void AddPosition(this NetworkMessage message, Position position)
        {
            message.AddUInt16(position.X);
            message.AddUInt16(position.Y);
            message.AddByte(position.Z);
        }

        public static void AddItemId(this NetworkMessage message, ushort itemId)
        {
            message.AddUInt16(ItemManager.Templates[itemId].ClientId);
        }

        public static Position GetPosition(this NetworkMessage message)
        {
            return new Position(message.GetUInt16(), message.GetUInt16(), message.GetByte());
        }
    }
}
