using System.Collections.Generic;
using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public abstract class Thing
    {
        private static readonly Position NullTilePosition = new Position(0xFFFF, 0xFFFF, 0xFF);

        public virtual ushort GetThingId() { return 0; }

        public abstract string GetDescription(int lookDistance);

        public virtual Thing GetParent()
        {
            return null;
        }
        public virtual Thing GetRealParent()
        {
            return GetParent();
        }
        public virtual void SetParent(Thing parent) { }

        public virtual Position GetPosition()
        {
            Tile tile = this as Tile;
            if (tile == null)
                return NullTilePosition;
            return tile.Position;
        }

        public abstract int GetThrowRange();
        public abstract bool IsPushable();
        public virtual bool IsRemoved()
        {
            return true;
        }

        //Cylinder is implemented as Thing as well!
        public virtual ReturnTypes QueryAdd(int index, Thing thing, uint count, CylinderFlags cFlags, Creature actor = null) { return ReturnTypes.NoError; }
        public virtual ReturnTypes QueryMaxCount(int index, Thing thing, uint count, ref uint maxQueryCount, CylinderFlags flags) { return ReturnTypes.NoError; }
        public virtual ReturnTypes QueryRemove(Thing thing, uint count, CylinderFlags flags) { return ReturnTypes.NoError; }
        public virtual Thing QueryDestination(ref int index, Thing thing, ref Item destItem, ref CylinderFlags flags) { return null; }

		public virtual void AddThing(Thing thing) { }
		public virtual void AddThing(uint index, Thing thing) { }
		public virtual void UpdateThing(Thing thing, ushort itemId, uint count) { }
		public virtual void ReplaceThing(uint index, Thing thing) { }
		public virtual void RemoveThing(Thing thing, uint count) { }

        public virtual void PostAddNotification(Thing thing, Thing oldParent, int index, CylinderLinks link = CylinderLinks.Owner) { }
        public virtual void PostRemoveNotification(Thing thing, Thing newParent, int index, CylinderLinks link = CylinderLinks.Owner) { }

        public virtual int GetThingIndex(Thing thing) { return -1; }
        public virtual int GetFirstIndex() { return 0; }
        public virtual int GetLastIndex() { return 0; }
        public virtual Thing GetThing(int index) { return null; }
        public virtual uint GetItemTypeCount(ushort itemId, int subType = -1) { return 0; }
        public virtual Dictionary<uint, uint> GetAllItemTypeCount(Dictionary<uint, uint> countMap) { return countMap; }

        public virtual void InternalAddThing(Thing thing) { }
        public virtual void InternalAddThing(uint index, Thing thing) { }
        public virtual void StartDecaying() { }
    }
}
