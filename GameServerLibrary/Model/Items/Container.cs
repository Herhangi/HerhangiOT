using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServerLibrary.Model.Items
{
    public class Container : Item
    {
        protected uint MaxSize;
        protected uint TotalWeight;
        protected Deque<Item> ItemList;
        protected uint SerializationCount;

        protected bool Unlocked;
        protected bool Pagination;

        public Container(ushort id) : base(id)
        {
            MaxSize = ItemManager.Templates[id].MaxItems;
            TotalWeight = 0;
            SerializationCount = 0;
            Unlocked = true;
            Pagination = false;
        }

        public Container(ushort id, ushort size) : base(id)
        {
            MaxSize = size;
            TotalWeight = 0;
            SerializationCount = 0;
            Unlocked = true;
            Pagination = false;
        }

        //TODO: Container from tile
    }
}
