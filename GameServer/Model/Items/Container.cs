using System;
using System.Collections.Generic;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Items
{
    public class Container : Item
    {
        public ushort MaxSize { get; protected set; }
        public uint TotalWeight { get; protected set; }
        public Deque<Item> ItemList { get; protected set; }
        public uint SerializationCount { get; protected set; }

        public bool IsUnlocked { get; protected set; }
        public bool HasPagination { get; protected set; }

        public Container(ushort id) : base(id)
        {
            MaxSize = ItemManager.Templates[id].MaxItems;
            TotalWeight = 0;
            SerializationCount = 0;
            IsUnlocked = true;
            HasPagination = false;
        }

        public Container(ushort id, ushort size) : base(id)
        {
            MaxSize = size;
            TotalWeight = 0;
            SerializationCount = 0;
            IsUnlocked = true;
            HasPagination = false;
        }

        //TODO: Container from tile
        
        public void AddItemBack(Item item)
        {
	        AddItem(item);
	        UpdateItemWeight(item.GetWeight());

	        //send change to client
	        if (Parent != null)// && (getParent() != VirtualCylinder::virtualCylinder)) TODO: Virtual Cylinder
            {
		        OnAddContainerItem(item);
	        }
        }
        
        private void AddItem(Item item)
        {
	        ItemList.AddToBack(item);
	        item.SetParent(this);
        }

        public void ReplaceThing(int index, Thing thing)
        {
	        Item item = thing as Item;
	        if (item == null)
		        return;

	        Item replacedItem = GetItemByIndex(index);
	        if (replacedItem == null)
		        return;

	        ItemList[index] = item;
	        item.SetParent(this);
	        UpdateItemWeight(-(replacedItem.GetWeight()) + item.GetWeight());

	        //send change to client
	        if (Parent != null)
		        OnUpdateContainerItem((ushort)index, replacedItem, item);

	        replacedItem.SetParent(null);
        }

        public void RemoveThing(Thing thing, uint count)
        {
	        Item item = thing as Item;
	        if (item == null)
		        return;

            int index = ItemList.IndexOf(item);
	        if (index == -1)
		        return;

	        if (item.IsStackable && count != item.Count)
            {
		        byte newCount = (byte)Math.Max(0, item.Count - count); 
		        uint oldWeight = item.GetWeight();
		        item.Count = newCount;
		        UpdateItemWeight(-oldWeight + item.GetWeight());

		        //send change to client
		        if (Parent != null)
			        OnUpdateContainerItem((ushort)index, item, item);
	        }
            else
            {
		        UpdateItemWeight(-item.GetWeight());

		        //send change to client
		        if (Parent != null)
			        OnRemoveContainerItem((ushort)index, item);

		        item.SetParent(null);
		        ItemList.RemoveAt(index);
	        }
        }

        private void UpdateItemWeight(long diff)
        {
	        TotalWeight = (uint)(TotalWeight + diff);

            Container parentContainer = GetParentContainer();
	        if (parentContainer != null)
            {
		        parentContainer.UpdateItemWeight(diff);
	        }
        }
        
        public Item GetItemByIndex(int index)
        {
	        if (index >= MaxSize)
		        return null;
	        
            return ItemList[index];
        }
        
        public int GetThingIndex(Thing thing)
        {
            Item item = thing as Item;
            if (item == null) return -1;
            return ItemList.IndexOf(item);
        }

        private Container GetParentContainer()
        {
	        if (Parent == null)
		        return null;
	        
            return Parent as Container;
        }

        #region On Container Change Operations
        private void OnAddContainerItem(Item item)
        {
	        HashSet<Creature> spectators = new HashSet<Creature>();
	        Map.GetSpectators(ref spectators, GetPosition(), false, true, 2, 2, 2, 2);

	        //send to client
	        foreach (Player spectator in spectators)
	        {
	            spectator.SendAddContainerItem(this, item);
	        }

	        //event methods
	        foreach (Player spectator in spectators)
            {
		        spectator.OnAddContainerItem(item);
	        }
        }

        private void OnUpdateContainerItem(ushort index, Item oldItem, Item newItem)
        {
	        HashSet<Creature> spectators = new HashSet<Creature>();
	        Map.GetSpectators(ref spectators, GetPosition(), false, true, 2, 2, 2, 2);

	        //send to client
	        foreach (Player spectator in spectators)
            {
		        spectator.SendUpdateContainerItem(this, index, newItem);
	        }

	        //event methods
	        foreach (Player spectator in spectators)
            {
		        spectator.OnUpdateContainerItem(this, oldItem, newItem);
	        }
        }

        private void OnRemoveContainerItem(ushort index, Item item)
        {
	        HashSet<Creature> list = new HashSet<Creature>();
	        Map.GetSpectators(ref list, GetPosition(), false, true, 2, 2, 2, 2);

	        //send change to client
	        foreach (Player spectator in list)
            {
		        spectator.SendRemoveContainerItem(this, index);
	        }

	        //event methods
	        foreach (Player spectator in list)
            {
		        spectator.OnRemoveContainerItem(this, item);
	        }
        }
        #endregion
        
        public bool IsHoldingItem(Item item)
        {
            return ItemList.IndexOf(item) != -1;
        }

        public bool HasParent()
        {
            return Id != (short) FixedItems.BrowseField && !(GetParent() is Player);
        }
    }
}
