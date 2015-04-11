using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public abstract class Thing
    {
        private static readonly Position NullTilePosition = new Position(0xFFFF, 0xFFFF, 0xFF);

        public virtual ushort GetThingId() { return 0; }

        public virtual string GetDescription(int lookDistance = 0) { return string.Empty; }

        public virtual void SetParent(Thing parent) { }
        
		public virtual void AddThing(Thing thing) { }
		public virtual void AddThing(uint index, Thing thing) { }
		public virtual void UpdateThing(Thing thing, ushort itemId, uint count) { }
		public virtual void ReplaceThing(uint index, Thing thing) { }
		public virtual void RemoveThing(Thing thing, uint count) { }

        public virtual void PostAddNotification(Thing thing, Thing oldParent, int index, CylinderLinks link = CylinderLinks.Owner) { }
        public virtual void PostRemoveNotification(Thing thing, Thing newParent, int index, CylinderLinks link = CylinderLinks.Owner) { }

        public virtual Position GetPosition()
        {
            Tile tile = this as Tile;
            if (tile == null)
                return NullTilePosition;
            return tile.Position;
        }

        public virtual bool IsRemoved()
        {
            return true;
        }

        public virtual Thing GetParent()
        {
            return null;
        }
		public virtual Thing GetRealParent()
        {
			return GetParent();
		}

		public virtual ReturnTypes QueryAdd(int index, Thing thing, uint count, CylinderFlags cFlags, Creature actor = null) { return ReturnTypes.NoError; }
        public virtual Thing QueryDestination(ref int index, Thing thing, ref Item destItem, ref CylinderFlags flags) { return null; }
    }
}
