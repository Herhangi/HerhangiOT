using System;
using System.IO;
using HerhangiOT.GameServer.Model.Items;
using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class Item : Thing
    {
        public ushort Id { get; protected set; }
        public byte Count { get; set; }
        public uint ReferenceCounter { get; protected set; }
        public bool IsLoadedFromMap { get; set; }

        public FluidTypes FluidType { get { return FluidTypes.None; } } //TODO: Attributes

        public Item(ushort id, byte count = 0)
        {
            Id = id;
            Count = count;
            ReferenceCounter = 0;

            //ATTRIBUTES
            ItemTemplate it = ItemManager.Templates[id];

            Count = 1;

            if (it.Group.HasFlag(ItemGroups.Fluid) || it.Group.HasFlag(ItemGroups.Splash))
            {
                //TODO: SET ATTRIBUTE: FLUID TYPE
            }
            else if (it.IsStackable)
            {
                if (count != 0)
                    Count = count;
                else if (it.Charges != 0)
                    Count = (byte)it.Charges;
            }
            else if (it.Charges != 0)
            {
                if (count != 0)
                    Count = count;
                else
                    Count =(byte)it.Charges;
            }

            IsLoadedFromMap = false;
            //TODO: SET ATTRIBUTE: DURATION
        }

        public override ushort GetThingId()
        {
            throw new System.NotImplementedException();
        }

        public override string GetDescription(int lookDistance = 0)
        {
            throw new System.NotImplementedException();
        }

        public static Item CreateItem(BinaryReader reader)
        {
            ushort id;
            try
            {
                 id = reader.ReadUInt16();
            }
            catch (Exception)
            {
                return null;
            }
            FixItemId(ref id);

            return CreateItem(id);
        }

        public static Item CreateItem(ushort itemId, byte count = 0)
        {
            Item newItem = null;

            ItemTemplate it = ItemManager.Templates[itemId];
       
            if (it.Group == ItemGroups.Deprecated)
                return null;

	        if (it.IsStackable && count == 0) {
		        count = 1;
	        }

	        if (it.Id != 0) {
		        if (it.Type == ItemTypes.Depot) {
			        newItem = new DepotLocker(itemId);
		        } else if (it.Group.HasFlag(ItemGroups.Container)) {
			        newItem = new Container(itemId);
		        } else if (it.Type == ItemTypes.Teleport) {
			        newItem = new Teleport(itemId);
		        } else if (it.Type == ItemTypes.MagicField) {
			        newItem = new MagicField(itemId);
		        } else if (it.Type == ItemTypes.Door) {
			        newItem = new Door(itemId);
		        } else if (it.Type == ItemTypes.TrashHolder) {
			        newItem = new TrashHolder(itemId);
		        } else if (it.Type == ItemTypes.Mailbox) {
			        newItem = new Mailbox(itemId);
		        } else if (it.Type == ItemTypes.Bed) {
			        newItem = new BedItem(itemId);
		        } else if (it.Id >= 2210 && it.Id <= 2212) {
			        newItem = new Item((ushort)(itemId - 3), count);
		        } else if (it.Id == 2215 || it.Id == 2216) {
                    newItem = new Item((ushort)(itemId - 2), count);
		        } else if (it.Id >= 2202 && it.Id <= 2206) {
                    newItem = new Item((ushort)(itemId - 37), count);
		        } else if (it.Id == 2640) {
                    newItem = new Item(6132, count);
		        } else if (it.Id == 6301) {
                    newItem = new Item(6300, count);
		        } else if (it.Id == 18528) {
                    newItem = new Item(18408, count);
		        } else {
			        newItem = new Item(itemId, count);
		        }

		        newItem.IncrementReferenceCounter();
	        }

	        return newItem;
        }

        public static void FixItemId(ref ushort id)
        {
            switch ((FixedItems)id)
            {
                case FixedItems.FireFieldPvpFull:
                    id = (ushort)FixedItems.FireFieldPersistentFull;
                    break;
                case FixedItems.FireFieldPvpMedium:
                    id = (ushort)FixedItems.FireFieldPersistentMedium;
                    break;
                case FixedItems.FireFieldPvpSmall:
                    id = (ushort)FixedItems.FireFieldPersistentSmall;
                    break;
                case FixedItems.EnergyFieldPvp:
                    id = (ushort)FixedItems.EnergyFieldPersistent;
                    break;
                case FixedItems.PoisonFieldPvp:
                    id = (ushort)FixedItems.PoisonFieldPersistent;
                    break;
                case FixedItems.MagicWall:
                    id = (ushort)FixedItems.MagicWallPersistent;
                    break;
                case FixedItems.WildGrowth:
                    id = (ushort)FixedItems.WildGrowthPersistent;
                    break;
            }
        }

        public void SetSpecialDescription(string desc)
        {
            //TODO: SET ATTRIBUTE: DESCRIPTION
        }

        public void IncrementReferenceCounter()
        {
            ReferenceCounter++;
        }

        public void DecrementReferenceCounter()
        {
            if (--ReferenceCounter == 0)
                //TODO: RECYCLE ITEM
                return;
        }

        public void StartDecaying()
        {
            //TODO: GAME DECAYING
        }

        #region Template Attributes
        public bool HasProperty(ItemProperties property)
        {
	        ItemTemplate it = ItemManager.Templates[Id];
	        switch (property) {
		        case ItemProperties.BlockSolid: return it.DoesBlockSolid;
		        case ItemProperties.Moveable: return it.IsMoveable;// && !hasAttribute(ITEM_ATTRIBUTE_UNIQUEID); TODO: Attributes
		        case ItemProperties.HasHeight: return it.HasHeight;
		        case ItemProperties.BlockProjectile: return it.DoesBlockProjectile;
		        case ItemProperties.BlockPath: return it.DoesBlockPathFinding;
		        case ItemProperties.IsVertical: return it.IsVertical;
		        case ItemProperties.IsHorizontal: return it.IsHorizontal;
		        case ItemProperties.ImmovableBlockSolid: return it.DoesBlockSolid && (!it.IsMoveable);// || hasAttribute(ITEM_ATTRIBUTE_UNIQUEID)); TODO: Attributes
		        case ItemProperties.ImmovableBlockPath: return it.DoesBlockPathFinding && (!it.IsMoveable);// || hasAttribute(ITEM_ATTRIBUTE_UNIQUEID)); TODO: Attributes
		        case ItemProperties.ImmovableNoFieldBlockPath: return !IsMagicField && it.DoesBlockPathFinding && (!it.IsMoveable);// || hasAttribute(ITEM_ATTRIBUTE_UNIQUEID)); TODO: Attributes
		        case ItemProperties.NoFieldBlockPath: return !IsMagicField && it.DoesBlockPathFinding;
		        case ItemProperties.SupportHangable: return it.IsHorizontal || it.IsVertical;
		        default: return false;
	        }
        }

        public bool IsMovable { get { return ItemManager.Templates[Id].IsMoveable; } }
        public bool IsGroundTile { get { return ItemManager.Templates[Id].Group == ItemGroups.Ground; } }
        public bool IsAlwaysOnTop { get { return ItemManager.Templates[Id].IsAlwaysOnTop; } }
        public bool IsBlocking { get { return ItemManager.Templates[Id].DoesBlockSolid; } }
        public bool IsHangable { get { return ItemManager.Templates[Id].IsHangable; } }
        public bool IsPickupable { get { return ItemManager.Templates[Id].IsPickupable; } }
        public bool IsMagicField { get { return ItemManager.Templates[Id].Type == ItemTypes.MagicField; } }
        public bool HasWalkStack { get { return ItemManager.Templates[Id].WalkStack; } }
        public byte AlwaysOnTopOrder { get { return ItemManager.Templates[Id].AlwaysOnTopOrder; } }
        public FloorChangeDirections FloorChangeDirection { get { return ItemManager.Templates[Id].FloorChange; } }

        public bool ImmovableBlockSolid()
        {
            ItemTemplate it = ItemManager.Templates[Id];
            return it.DoesBlockSolid && (!it.IsMoveable); //Attribute
        }
        #endregion
    }
}
