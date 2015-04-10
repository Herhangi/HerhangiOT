using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model.Items
{
    public class DepotChest : Container
    {
        public uint MaxDepotItems { get; set; }

        public DepotChest(ushort id) : base(id)
        {
        }

        public DepotChest(ushort id, ushort size) : base(id, size)
        {
        }

        #region Thing Implementations
        public override ReturnTypes QueryAdd(int index, Thing thing, uint count, CylinderFlags cFlags, Creature actor = null)
        {
	        Item item = thing as Item;
	        if (item == null)
		        return ReturnTypes.NotPossible;

	        bool skipLimit = cFlags.HasFlag(CylinderFlags.Nolimit);
	        if (!skipLimit)
            {
		        int addCount = 0;

		        if ((item.IsStackable && item.Count != count))
			        addCount = 1;

		        if (item.GetTopParent() != this)
		        {
		            Container container = item as Container;
			        if (container != null)
                    {
				        addCount = container.ItemList.Count + 1;
			        }
                    else
                    {
				        addCount = 1;
			        }
		        }

		        if (ItemList.Count + addCount > MaxDepotItems)
			        return ReturnTypes.DepotIsFull;
	        }

	        return base.QueryAdd(index, thing, count, cFlags, actor);
        }

        public override void PostAddNotification(Thing thing, Thing oldParent, int index, CylinderLinks link = CylinderLinks.Owner)
        {
            Thing parent = GetParent();
            if (parent != null)
                parent.PostAddNotification(thing, oldParent, index, CylinderLinks.Parent);
        }

        public override void PostRemoveNotification(Thing thing, Thing newParent, int index, CylinderLinks link = CylinderLinks.Owner)
        {
            Thing parent = GetParent();
            if (parent != null)
            {
                parent.PostRemoveNotification(thing, newParent, index, CylinderLinks.Parent);
            }
        }
        #endregion

        #region Overrides
        public override bool CanRemove()
        {
            return false;
        }

        public override Thing GetParent()
        {
	        if (Parent != null)
		        return Parent.GetParent();
	        return null;
        }
        public override Thing GetRealParent()
        {
            return Parent;
        }
        #endregion
    }
}
