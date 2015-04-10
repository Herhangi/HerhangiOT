using System;
using HerhangiOT.GameServer.Enums;
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

        public static void AddItem(this NetworkMessage message, Item item)
        {
	        ItemTemplate it = ItemManager.Templates[item.Id];

            message.AddUInt16(it.ClientId);
            message.AddByte(0xFF); // MARK_UNMARKED

	        if (it.IsStackable)
            {
		        message.AddByte(Math.Min(byte.MaxValue, item.Count));
	        }
            else if (it.Group.HasFlag(ItemGroups.Splash) || it.Group.HasFlag(ItemGroups.Fluid))
            {
		        message.AddByte((byte)ItemManager.FluidMap[(byte)item.FluidType & 7]);
	        }

	        if (it.IsAnimation)
            {
		        message.AddByte(0xFE);    // random phase (0xFF for async)
	        }
        }
        
        public static void AddItem(this NetworkMessage message, ushort id, byte count)
        {
	        ItemTemplate it = ItemManager.Templates[id];

            message.AddUInt16(it.ClientId);
	        message.AddByte(0xFF);    // MARK_UNMARKED

	        if (it.IsStackable)
            {
                message.AddByte(count);
            }
            else if (it.Group.HasFlag(ItemGroups.Splash) || it.Group.HasFlag(ItemGroups.Fluid))
            {
                message.AddByte((byte)ItemManager.FluidMap[count & 7]);
	        }

	        if (it.IsAnimation) {
		        message.AddByte(0xFE);    // random phase (0xFF for async)
	        }
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
