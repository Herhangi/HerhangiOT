using System;
using System.Collections.Generic;
using System.Linq;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model.Items;

namespace HerhangiOT.GameServer.Model
{
    public class Tile : Thing
    {
        public Position Position { get; set; }
        public TileFlags Flags { get; private set; }

        public List<Item> Items { get; private set; }
        public List<Creature> Creatures { get; private set; }

        public Item Ground { get; private set; }

        private int _downItemCount;
        public int TopItemsIndex { get { return _downItemCount; } }

        public Tile(int x, int y, int z)
        {
            Position = new Position((ushort)x,(ushort)y,(byte)z);
            Flags = TileFlags.None;
        }

        #region Flag Operations
        public void SetFlag(TileFlags flag)
        {
            Flags |= flag;
        }
        public void SetFlags(Item item)
        {
            if (!Flags.HasFlag(TileFlags.FloorChange))
            {
                if(item.FloorChangeDirection != FloorChangeDirections.None)
                    SetFlag(TileFlags.FloorChange);

                switch (item.FloorChangeDirection)
                {
                    case FloorChangeDirections.Down:
                        SetFlag(TileFlags.FloorChangeDown);
                        break;
                    case FloorChangeDirections.North:
                        SetFlag(TileFlags.FloorChangeNorth);
                        break;
                    case FloorChangeDirections.South:
                        SetFlag(TileFlags.FloorChangeSouth);
                        break;
                    case FloorChangeDirections.East:
                        SetFlag(TileFlags.FloorChangeEast);
                        break;
                    case FloorChangeDirections.West:
                        SetFlag(TileFlags.FloorChangeWest);
                        break;
                    case FloorChangeDirections.SouthAlt:
                        SetFlag(TileFlags.FloorChangeSouthAlt);
                        break;
                    case FloorChangeDirections.EastAlt:
                        SetFlag(TileFlags.FloorChangeEastAlt);
                        break;
                }
            }

            if (item.ImmovableBlockSolid())
                SetFlag(TileFlags.ImmovableBlockSolid);

            //TODO: WAITING ITEM ATTRIBUTES
        }

        public void ResetFlag(TileFlags flag)
        {
            Flags &= ~flag;
        }
        public void ResetFlags(Item item)
        {
            //TODO: WAITING ITEM ATTRIBUTES
        }
        #endregion

        #region Add Methods
        public void InternalAddThing(Thing thing)
        {
            thing.SetParent(this);

            Creature creature = thing as Creature;

            if (creature != null)
            {
                //TODO: PROGRAM HERE
                //g_game.map.clearSpectatorCache();
                //CreatureVector* creatures = makeCreatures();
                //creatures->insert(creatures->begin(), creature);
            }
            else
            {
                Item item = thing as Item;
                if (item == null) return;

                List<Item> items = MakeItemList();

                if (item.IsGroundTile)
                {
                    if (Ground == null)
                        Ground = item;
                }
                else if (item.IsAlwaysOnTop)
                {
                    bool isInserted = false;

                    for (int i = _downItemCount; i < items.Count; i++)
                    {
                        if (items[i].AlwaysOnTopOrder > item.AlwaysOnTopOrder)
                        {
                            items.Insert(i, item);
                            isInserted = true;
                            break;
                        }
                    }

                    if (!isInserted)
                        items.Add(item);
                }
                else
                {
                    items.Insert(0, item);
                    _downItemCount++;
                }

                SetFlags(item);
            }
        }

        public override ReturnTypes QueryAdd(int index, Thing thing, uint count, CylinderFlags cFlags, Creature actor = null)
        {
            Item item;
            Creature creature;

	        if ((creature = thing as Creature) != null)
            {
		        if (cFlags.HasFlag(CylinderFlags.Nolimit))
			        return ReturnTypes.NoError;

		        if (cFlags.HasFlag(CylinderFlags.Pathfinding))
                {
			        if (Flags.HasFlag(TileFlags.FloorChange) || Flags.HasFlag(TileFlags.Teleport))
				        return ReturnTypes.NotPossible;
		        }

                if (Ground == null)
                    return ReturnTypes.NotPossible;

                Monster monster = creature as Monster;
		        if (monster != null)
                {
			        if (Flags.HasFlag(TileFlags.ProtectionZone))
				        return ReturnTypes.NotPossible;
                    
			        if (Flags.HasFlag(TileFlags.FloorChange) || Flags.HasFlag(TileFlags.Teleport))
				        return ReturnTypes.NotPossible;

                    //TODO: After monsters
                    //const CreatureVector* creatures = getCreatures();
                    //if (monster->canPushCreatures() && !monster->isSummon()) {
                    //    if (creatures) {
                    //        for (Creature* tileCreature : *creatures) {
                    //            if (tileCreature->getPlayer() && tileCreature->getPlayer()->isInGhostMode()) {
                    //                continue;
                    //            }

                    //            const Monster* creatureMonster = tileCreature->getMonster();
                    //            if (!creatureMonster || !tileCreature->isPushable() ||
                    //                    (creatureMonster->isSummon() && creatureMonster->getMaster()->getPlayer())) {
                    //                return RETURNVALUE_NOTPOSSIBLE;
                    //            }
                    //        }
                    //    }
                    //} else if (creatures && !creatures->empty()) {
                    //    for (const Creature* tileCreature : *creatures) {
                    //        if (!tileCreature->isInGhostMode()) {
                    //            return RETURNVALUE_NOTENOUGHROOM;
                    //        }
                    //    }
                    //}

                    //if (hasFlag(TILESTATE_IMMOVABLEBLOCKSOLID)) {
                    //    return RETURNVALUE_NOTPOSSIBLE;
                    //}

                    //if (hasBitSet(FLAG_PATHFINDING, flags) && hasFlag(TILESTATE_IMMOVABLENOFIELDBLOCKPATH)) {
                    //    return RETURNVALUE_NOTPOSSIBLE;
                    //}

                    //if (hasFlag(TILESTATE_BLOCKSOLID) || (hasBitSet(FLAG_PATHFINDING, flags) && hasFlag(TILESTATE_NOFIELDBLOCKPATH))) {
                    //    if (!(monster->canPushItems() || hasBitSet(FLAG_IGNOREBLOCKITEM, flags))) {
                    //        return RETURNVALUE_NOTPOSSIBLE;
                    //    }
                    //}

                    //MagicField* field = getFieldItem();
                    //if (field && !field->isBlocking()) {
                    //    CombatType_t combatType = field->getCombatType();

                    //    //There is 3 options for a monster to enter a magic field
                    //    //1) Monster is immune
                    //    if (!monster->isImmune(combatType)) {
                    //        //1) Monster is "strong" enough to handle the damage
                    //        //2) Monster is already afflicated by this type of condition
                    //        if (hasBitSet(FLAG_IGNOREFIELDDAMAGE, flags)) {
                    //            if (!(monster->canPushItems() || monster->hasCondition(Combat::DamageToConditionType(combatType)))) {
                    //                return RETURNVALUE_NOTPOSSIBLE;
                    //            }
                    //        } else {
                    //            return RETURNVALUE_NOTPOSSIBLE;
                    //        }
                    //    }
                    //}

                    //return RETURNVALUE_NOERROR;
		        }

                Player player = creature as Player;
		        if (player != null)
                {
                    //TODO: Player Group
                    if(Creatures != null && Creatures.Count != 0 && !cFlags.HasFlag(CylinderFlags.IgnoreBlockCreature))// && !player->isAccessPlayer())
                    {
                        foreach (Creature tileCreature in Creatures)
                        {
                            if(!player.CanWalkthrough(tileCreature))
                                return ReturnTypes.NotPossible;
                        }
			        }

			        if (player.Parent == null && Flags.HasFlag(TileFlags.NoLogout))
				        return ReturnTypes.NotPossible; //player is trying to login to a "no logout" tile

			        Tile playerTile = player.Parent;
			        if (playerTile != null && player.IsPzLocked)
                    {
				        if (!playerTile.Flags.HasFlag(TileFlags.PvpZone))
                        {
					        //player is trying to enter a pvp zone while being pz-locked
					        if (Flags.HasFlag(TileFlags.PvpZone))
						        return ReturnTypes.PlayerIsPZLockedEnterPvpZone;
				        }
                        else if (!Flags.HasFlag(TileFlags.PvpZone))
                        {
					        // player is trying to leave a pvp zone while being pz-locked
					        return ReturnTypes.PlayerIsPZLockedLeavePvpZone;
				        }

				        if ((!playerTile.Flags.HasFlag(TileFlags.NoPvpZone) && Flags.HasFlag(TileFlags.NoPvpZone)) ||
					        (!playerTile.Flags.HasFlag(TileFlags.ProtectionZone) && Flags.HasFlag(TileFlags.ProtectionZone))) {
					        // player is trying to enter a non-pvp/protection zone while being pz-locked
					        return ReturnTypes.PlayerIsPZLocked;
				        }
			        }
		        }
                else if (Creatures != null && Creatures.Count != 0 && cFlags.HasFlag(CylinderFlags.IgnoreBlockCreature))
                {
                    foreach (Creature tileCreature in Creatures)
                    {
                        if(!tileCreature.IsInGhostMode())
                            return ReturnTypes.NotEnoughRoom;
                    }
		        }

		        if (Items != null)
                {
			        if (!cFlags.HasFlag(CylinderFlags.IgnoreBlockItem))
                    {
				        //If the CylinderFlags.IgnoreBlockItem bit isn't set we dont have to iterate every single item
				        if (Flags.HasFlag(TileFlags.BlockSolid))
					        return ReturnTypes.NotEnoughRoom;
			        }
                    else
                    {
				        //CylinderFlags.IgnoreBlockItem is set
				        if (Ground != null)
				        {
				            ItemTemplate it = ItemManager.Templates[Ground.Id];
					        if (it.DoesBlockSolid && (!it.IsMoveable))// || Ground->hasAttribute(ITEM_ATTRIBUTE_UNIQUEID))) { TODO: Attributes
						        return ReturnTypes.NotPossible;
				        }

				        foreach(Item stackItem in Items)
                        {
				            ItemTemplate it = ItemManager.Templates[stackItem.Id];
					        if (it.DoesBlockSolid && (!it.IsMoveable))// || item->hasAttribute(ITEM_ATTRIBUTE_UNIQUEID))) {
						        return ReturnTypes.NotPossible;
				        }
			        }
		        }
	        }
            else if ((item = thing as Item) != null)
            {
		        if (Items != null && Items.Count >= 0xFFFF)
			        return ReturnTypes.NotPossible;

		        if (cFlags.HasFlag(CylinderFlags.Nolimit))
			        return ReturnTypes.NoError;

		        bool itemIsHangable = item.IsHangable;
		        if (Ground == null && !itemIsHangable) {
			        return ReturnTypes.NotPossible;
		        }

		        if (Creatures != null && Creatures.Count != 0 && item.IsBlocking && !cFlags.HasFlag(CylinderFlags.IgnoreBlockCreature))
                {
                    foreach (Creature tileCreature in Creatures)
                    {
                        if(!tileCreature.IsInGhostMode())
                            return ReturnTypes.NotEnoughRoom;
                    }
		        }

		        if (itemIsHangable && Flags.HasFlag(TileFlags.SupportsHangable))
                {
			        if (Items != null)
                    {
                        foreach (Item tileItem in Items)
                        {
                            if(tileItem.IsHangable)
                                return ReturnTypes.NeedExchange;
                        }
			        }
		        }
                else
                {
			        if (Ground != null)
                    {
				        ItemTemplate itemTemplate = ItemManager.Templates[Ground.Id];
				        if (itemTemplate.DoesBlockSolid)
                        {
					        if (!itemTemplate.DoesAllowPickupable || item.IsMagicField || item.IsBlocking)
                            {
						        if (!item.IsPickupable)
							        return ReturnTypes.NotEnoughRoom;

						        if (!itemTemplate.HasHeight || itemTemplate.IsPickupable || itemTemplate.Type == ItemTypes.Bed)
							        return ReturnTypes.NotEnoughRoom;
					        }
				        }
			        }

			        if (Items != null)
                    {
                        foreach (Item tileItem in Items)
                        {
				            ItemTemplate itemTemplate = ItemManager.Templates[tileItem.Id];

                            if (!itemTemplate.DoesBlockSolid)
                                continue;

                            if (itemTemplate.DoesAllowPickupable && !item.IsMagicField && !item.IsBlocking)
                                continue;

                            if (!item.IsPickupable)
                                return ReturnTypes.NotEnoughRoom;

                            if (!itemTemplate.HasHeight || itemTemplate.IsPickupable || itemTemplate.Type == ItemTypes.Bed)
                                return ReturnTypes.NotEnoughRoom;
                        }
			        }
		        }
	        }

	        return ReturnTypes.NoError;
        }

        public override Thing QueryDestination(ref int index, Thing thing, ref Item destItem, ref CylinderFlags flags)
        {
            Tile destTile = null;
            destItem = null;

            if (Flags.HasFlag(TileFlags.FloorChangeDown))
            {
                ushort dx = Position.X;
                ushort dy = Position.Y;
                byte dz = (byte)(Position.Z + 1);

                Tile southDownTile = Map.GetTile(dx, (ushort)(dy - 1), dz);
                if (southDownTile != null && southDownTile.FloorChange(Directions.SouthAlt))
                {
                    dy -= 2;
                    destTile = Map.GetTile(dx, dy, dz);
                }
                else
                {
                    Tile eastDownTile = Map.GetTile((ushort)(dx - 1), dy, dz);
                    if (eastDownTile != null && eastDownTile.FloorChange(Directions.EastAlt))
                    {
                        dx -= 2;
                        destTile = Map.GetTile(dx, dy, dz);
                    }
                    else
                    {
                        Tile downTile = Map.GetTile(dx, dy, dz);
                        if (downTile != null)
                        {
                            if (downTile.FloorChange(Directions.North))
                            {
                                ++dy;
                            }

                            if (downTile.FloorChange(Directions.South))
                            {
                                --dy;
                            }

                            if (downTile.FloorChange(Directions.SouthAlt))
                            {
                                dy -= 2;
                            }

                            if (downTile.FloorChange(Directions.East))
                            {
                                --dx;
                            }

                            if (downTile.FloorChange(Directions.EastAlt))
                            {
                                dx -= 2;
                            }

                            if (downTile.FloorChange(Directions.West))
                            {
                                ++dx;
                            }

                            destTile = Map.GetTile(dx, dy, dz);
                        }
                    }
                }
            }
            else if (Flags.HasFlag(TileFlags.FloorChange))
            {
                ushort dx = Position.X;
                ushort dy = Position.Y;
                byte dz = (byte)(Position.Z - 1);

                if (FloorChange(Directions.North))
                {
                    --dy;
                }

                if (FloorChange(Directions.South))
                {
                    ++dy;
                }

                if (FloorChange(Directions.East))
                {
                    ++dx;
                }

                if (FloorChange(Directions.West))
                {
                    --dx;
                }

                if (FloorChange(Directions.SouthAlt))
                {
                    dy += 2;
                }

                if (FloorChange(Directions.EastAlt))
                {
                    dx += 2;
                }

                destTile = Map.GetTile(dx, dy, dz);
            }

            if (destTile == null)
            {
                destTile = this;
            }
            else
            {
                flags |= CylinderFlags.Nolimit;    //Will ignore that there is blocking items/creatures
            }

            Thing destThing = destTile.GetTopDownItem();
            if (destThing != null)
            {
                destItem = destThing as Item;
            }

            return destTile;
        }

        public void AddCreature(Creature creature)
        {
            if (Creatures == null)
                Creatures = new List<Creature>();
            Creatures.Add(creature);
        }

        public void AddThing(Thing thing)
        {
	        AddThing(0, thing);
        }

        public virtual void AddThing(int index, Thing thing)
        {
            Creature creature = thing as Creature;
	        if (creature != null)
            {
		        Map.ClearSpectatorCache();
		        creature.SetParent(this);
                MakeCreatureList();
		        Creatures.Insert(0, creature);
	        }
            else
            {
		        Item item = thing as Item;
		        if (item == null)
                {
			        return;
		        }

		        if (Items != null && Items.Count >= 0xFFFF)
			        return;

		        item.SetParent(this);

		        if (item.IsGroundTile)
                {
			        if (Ground == null)
                    {
				        Ground = item;
				        OnAddTileItem(item);
			        }
                    else
                    {
                        ItemTemplate oldType = ItemManager.Templates[Ground.Id];
                        ItemTemplate newType = ItemManager.Templates[item.Id];

				        Item oldGround = Ground;
                        Ground.SetParent(null);
                        Game.ReleaseItem(Ground);
				        Ground = item;
				        ResetFlags(oldGround);
				        SetFlags(item);
				        OnUpdateTileItem(oldGround, oldType, item, newType);
				        PostRemoveNotification(oldGround, null, 0);
			        }
		        }
                else if (item.IsAlwaysOnTop)
                {
			        if (item.IsSplash)
                    {
				        //remove old splash if exists
				        if (Items != null)
                        {
                            for(int i = TopItemsIndex; i < Items.Count; i++)
                            {
						        Item oldSplash = Items[i];
						        if (oldSplash.IsSplash)
                                {
							        RemoveThing(oldSplash, 1);
							        oldSplash.SetParent(null);
							        Game.ReleaseItem(oldSplash);
							        PostRemoveNotification(oldSplash, null, 0);
							        break;
						        }
					        }
				        }
			        }

			        bool isInserted = false;

			        if (Items != null)
                    {
                        for(int i = TopItemsIndex; i < Items.Count; i++)
                        {
					        //Note: this is different from internalAddThing
					        if (ItemManager.Templates[item.Id].AlwaysOnTopOrder <= ItemManager.Templates[Items[i].Id].AlwaysOnTopOrder)
					        {
					            Items.Insert(i, item);
						        isInserted = true;
						        break;
					        }
				        }
			        }
                    else
                    {
				        MakeItemList();
			        }

			        if (!isInserted)
                    {
				        Items.Add(item);
			        }

			        OnAddTileItem(item);
		        }
                else
                {
			        if (item.IsMagicField)
                    {
				        //remove old field item if exists
				        if (Items != null)
                        {
                            for(int i = 0; i < TopItemsIndex; i++)
                            {
                                MagicField oldField = Items[i] as MagicField;
						        if (oldField != null)
                                {
							        if (oldField.IsReplaceable)
                                    {
								        RemoveThing(oldField, 1);

								        oldField.SetParent(null);
								        Game.ReleaseItem(oldField);
								        PostRemoveNotification(oldField, null, 0);
								        break;
							        }
                                    else
                                    {
								        //This magic field cannot be replaced.
								        item.SetParent(null);
								        Game.ReleaseItem(item);
								        return;
							        }
						        }
					        }
				        }
			        }

			        MakeItemList();
                    Items.Insert(0, item);
			        ++_downItemCount;
			        OnAddTileItem(item);
		        }
	        }
        }
        #endregion

        #region Remove Methods
        public void RemoveThing(Thing thing, uint count)
        {
	        Creature creature = thing as Creature;
	        if (creature != null)
	        {
	            if (Creatures.Remove(creature))
	            {
                    //TODO: CLEAR SPECTATOR CACHE
	            }
		        return;
	        }

	        Item item = thing as Item;
	        if (item == null)
		        return;

	        int index = GetThingIndex(item);
	        if (index == -1)
		        return;

	        if (item == Ground)
            {
		        Ground.SetParent(null);
		        Ground = null;

		        HashSet<Creature> spectators = new HashSet<Creature>();
		        Map.GetSpectators(ref spectators, Position, true);
		        OnRemoveTileItem(spectators, new List<int>(spectators.Count), item);
		        return;
	        }

	        if (Items == null)
		        return;

	        if (item.IsAlwaysOnTop)
	        {
	            int pos = Items.FindIndex(TopItemsIndex, i => i == item);
                if(pos == -1)
                    return;

	            List<int> oldStackPosVector = new List<int>();

		        HashSet<Creature> spectators = new HashSet<Creature>();
		        Map.GetSpectators(ref spectators, Position, true);
		        foreach (Creature spectator in spectators)
		        {
		            Player tmpPlayer = spectator as Player;
                    if(tmpPlayer != null)
				        oldStackPosVector.Add(GetStackposOfItem(tmpPlayer, item));
		        }

		        item.SetParent(null);
	            Items.RemoveAt(pos);
		        OnRemoveTileItem(spectators, oldStackPosVector, item);
	        }
            else
	        {
	            int pos = Items.FindIndex(0, TopItemsIndex, i => i == item);
		        if (pos == -1) 
			        return;

		        if (item.IsStackable && count != item.Count)
		        {
		            byte newCount = (byte)Math.Max(0, item.Count - count); 

			        item.Count = newCount;

		            ItemTemplate itemTemplate = ItemManager.Templates[item.Id];
                    OnUpdateTileItem(item, itemTemplate, item, itemTemplate);
		        }
                else
                {
                    List<int> oldStackPosVector = new List<int>();

                    HashSet<Creature> spectators = new HashSet<Creature>();
			        Map.GetSpectators(ref spectators, Position, true);
                    foreach (Creature spectator in spectators)
                    {
                        Player tmpPlayer = spectator as Player;
                        if(tmpPlayer != null)
                            oldStackPosVector.Add(GetStackposOfItem(tmpPlayer, item));
                    }

			        item.SetParent(null);
                    Items.RemoveAt(pos);
			        --_downItemCount;
			        OnRemoveTileItem(spectators, oldStackPosVector, item);
		        }
	        }
        }
        #endregion
        
        private void OnAddTileItem(Item item)
        {
	        if (item.HasProperty(ItemProperties.Moveable) || item is Container)
            {
	            Container fieldContainer;
                if (Game.BrowseFields.TryGetValue(this, out fieldContainer))
                {
                    fieldContainer.AddItemBack(item);
                    item.SetParent(this);
                }
	        }

	        SetFlags(item);

	        Position cylinderMapPos = GetPosition();

            HashSet<Creature> spectators = new HashSet<Creature>();
	        Map.GetSpectators(ref spectators, cylinderMapPos, true);

	        //send to client
	        foreach (Creature spectator in spectators)
	        {
	            Player tmpPlayer = (Player)spectator;
	            if (tmpPlayer != null)
			        tmpPlayer.SendAddTileItem(this, cylinderMapPos, item);
	        }

            //event methods
	        foreach (Creature spectator in spectators)
		        spectator.OnAddTileItem(this, cylinderMapPos);
        }
        private void OnUpdateTileItem(Item oldItem, ItemTemplate oldType, Item newItem, ItemTemplate newType)
        {
	        if (newItem.HasProperty(ItemProperties.Moveable) || newItem is Container)
            {
	            Container fieldContainer;
                if (Game.BrowseFields.TryGetValue(this, out fieldContainer))
                {
                    int index = fieldContainer.GetThingIndex(oldItem);
                    if (index != -1)
                    {
                        fieldContainer.ReplaceThing(index, newItem);
                        newItem.SetParent(this);
                    }
                }
	        }
            else if (oldItem.HasProperty(ItemProperties.Moveable) || oldItem is Container)
            {
	            Container fieldContainer;
                if (Game.BrowseFields.TryGetValue(this, out fieldContainer))
                {
                    Thing oldParent = oldItem.Parent;
                    fieldContainer.RemoveThing(oldItem, oldItem.Count);
			        oldItem.SetParent(oldParent);
		        }
	        }

	        Position cylinderMapPos = GetPosition();

	        HashSet<Creature> list = new HashSet<Creature>();
	        Map.GetSpectators(ref list, cylinderMapPos, true);

	        //send to client
            foreach (Creature spectator in list)
            {
                Player tmpPlayer = spectator as Player;
		        if (tmpPlayer != null)
                {
			        tmpPlayer.SendUpdateTileItem(this, cylinderMapPos, newItem);
		        }
	        }

            //event methods
            foreach (Creature spectator in list)
            {
		        spectator.OnUpdateTileItem(this, cylinderMapPos, oldItem, oldType, newItem, newType);
	        }
        }
        private void OnRemoveTileItem(HashSet<Creature> list, List<int> oldStackPosVector, Item item)
        {
	        if (item.HasProperty(ItemProperties.Moveable) || item is Container)
	        {
	            Container fieldContainer;
                if(Game.BrowseFields.TryGetValue(this, out fieldContainer))
                    fieldContainer.RemoveThing(item, item.Count);
	        }

	        ResetFlags(item);

	        Position cylinderMapPos = GetPosition();
            ItemTemplate iType = ItemManager.Templates[item.Id];

	        //send to client
	        int i = 0;
	        foreach (Creature spectator in list)
	        {
	            Player tmpPlayer = spectator as Player;
		        if (tmpPlayer != null)
                {
			        tmpPlayer.SendRemoveTileThing(cylinderMapPos, oldStackPosVector[i++]);
		        }
	        }

            //event methods
            foreach (Creature spectator in list)
            {
		        spectator.OnRemoveTileItem(this, cylinderMapPos, iType, item);
	        }
        }

        private void OnUpdateTile(HashSet<Creature> list)
        {
	        Position cylinderMapPos = GetPosition();

	        foreach (Player spectator in list)
            {
		        spectator.SendUpdateTile(this, cylinderMapPos);
	        }
        }

        public sealed override void PostAddNotification(Thing thing, Thing oldParent, int index, CylinderLinks link = CylinderLinks.Owner)
        {
	        HashSet<Creature> list = new HashSet<Creature>();
	        Map.GetSpectators(ref list, GetPosition(), true, true);
	        foreach (Player spectator in list)
            {
		        spectator.PostAddNotification(thing, oldParent, index, CylinderLinks.Near);
	        }

	        //add a reference to this item, it may be deleted after being added (mailbox for example)
            Creature creature = thing as Creature;
	        Item item;
	        if (creature != null)
            {
		        creature.IncrementReferenceCounter();
		        item = null;
	        }
            else
            {
		        item = thing as Item;
		        if (item != null)
                {
			        item.IncrementReferenceCounter();
		        }
	        }

	        if (link == CylinderLinks.Owner)
            {
		        if (Flags.HasFlag(TileFlags.Teleport))
                {
			        Teleport teleport = GetTeleportItem();
			        if (teleport != null)
                    {
				        teleport.AddThing(thing);
			        }
		        }
                else if (Flags.HasFlag(TileFlags.TrashHolder))
                {
			        TrashHolder trashholder = GetTrashHolder();
			        if (trashholder != null)
                    {
				        trashholder.AddThing(thing);
			        }
		        }
                else if (Flags.HasFlag(TileFlags.MailBox))
                {
			        Mailbox mailbox = GetMailbox();
			        if (mailbox != null)
                    {
				        mailbox.AddThing(thing);
			        }
		        }

		        //calling movement scripts TODO: Scripting
                //if (creature != null)
                //{
                //    g_moveEvents->onCreatureMove(creature, this, oldParent ? oldParent->getPosition() : getPosition(), MOVE_EVENT_STEP_IN);
                //}
                //else if (item)
                //{
                //    g_moveEvents->onItemMove(item, this, true);
                //}
	        }

	        //release the reference to this item onces we are finished
	        if (creature != null)
		        Game.ReleaseCreature(creature);
	        else if (item != null)
		        Game.ReleaseItem(item);
        }

        public sealed override void PostRemoveNotification(Thing thing, Thing newParent, int index, CylinderLinks link = CylinderLinks.Owner)
        {
            HashSet<Creature> list = new HashSet<Creature>();
	        Map.GetSpectators(ref list, GetPosition(), true, true);

	        if (GetThingCount() > 8)
            {
		        OnUpdateTile(list);
	        }

	        foreach (Player spectator in list)
            {
		        spectator.PostRemoveNotification(thing, newParent, index, CylinderLinks.Near);
	        }

	        //calling movement scripts
	        Creature creature = thing as Creature;
	        if (creature != null)
            {
		        //g_moveEvents->onCreatureMove(creature, this, GetPosition(), MOVE_EVENT_STEP_OUT); //TODO: SCRIPTING
	        }
            else
            {
		        Item item = thing as Item;
		        if (item != null)
                {
			        //g_moveEvents->onItemMove(item, this, false); //TODO: SCRIPTING
		        }
	        }
        }

        public bool HasProperty(ItemProperties property)
        {
            if (Ground != null && Ground.HasProperty(property))
                return true;

            if (Items != null)
            {
                foreach (Item item in Items)
                {
                    if (item.HasProperty(property))
                        return true;
                }
            }
            return false;
        }

        public bool HasHeight(uint n)
        {
	        uint height = 0;

	        if (Ground != null)
            {
		        if (Ground.HasProperty(ItemProperties.HasHeight))
			        ++height;

		        if (n == height)
			        return true;
	        }

            if (Items != null)
            {
                foreach (Item item in Items)
                {
                    if (item.HasProperty(ItemProperties.HasHeight))
                    {
                        if (n == ++height)
                            return true;
                    }
                }
            }
	        
	        return false;
        }

        public ZoneTypes GetZone()
        {
            if (Flags.HasFlag(TileFlags.ProtectionZone))
                return ZoneTypes.Protection;
            if (Flags.HasFlag(TileFlags.NoPvpZone))
                return ZoneTypes.NoPvp;
            if (Flags.HasFlag(TileFlags.PvpZone))
                return ZoneTypes.Pvp;
            return ZoneTypes.Normal;
        }

        
		private bool FloorChange(Directions direction)
        {
			switch (direction)
            {
				case Directions.North:
					return Flags.HasFlag(TileFlags.FloorChangeNorth);

                case Directions.South:
                    return Flags.HasFlag(TileFlags.FloorChangeSouth);

                case Directions.East:
                    return Flags.HasFlag(TileFlags.FloorChangeEast);

                case Directions.West:
                    return Flags.HasFlag(TileFlags.FloorChangeWest);

                case Directions.SouthAlt:
                    return Flags.HasFlag(TileFlags.FloorChangeSouthAlt);

                case Directions.EastAlt:
                    return Flags.HasFlag(TileFlags.FloorChangeEastAlt);

				default:
					return false;
			}
		}

        private Item GetTopDownItem()
        {
            if (Items != null)
            {
                if (_downItemCount == 0) return null;
                return Items[_downItemCount - 1];
            }
            return null;
        }

        public int GetThingCount()
        {
            int thingCount = 0;
            if (Creatures != null)
                thingCount += Creatures.Count;

            if (Items != null)
                thingCount += Items.Count;

            if (Ground != null)
                thingCount++;

            return thingCount;
        }
        public int GetThingIndex(Thing thing)
        {
	        int n = -1;
	        if (Ground != null)
            {
		        if (Ground == thing)
			        return 0;
		        ++n;
	        }

            Item item = null;
	        if (Items != null)
	        {
	            item = thing as Item;

		        if (item != null && item.IsAlwaysOnTop)
                {
                    for (int i = TopItemsIndex; i < Items.Count; i++)
                    {
				        ++n;
				        if (Items[i] == item)
					        return n;
			        }
		        }
                else
                {
	                n += Items.Count - _downItemCount;
		        }
	        }

	        if (Creatures != null)
            {
		        if (thing is Creature)
                {
			        foreach (Creature creature in Creatures)
                    {
				        ++n;
				        if (creature == thing)
					        return n;
			        }
		        }
                else
                {
			        n += Creatures.Count;
		        }
	        }

	        if (Items != null)
            {
		        if (item != null && !item.IsAlwaysOnTop)
                {
                    for(int i = 0; i < TopItemsIndex; i++)
                    {
				        ++n;
				        if (Items[i] == item)
					        return n;
			        }
		        }
	        }
	        return -1;
        }

        public int GetStackposOfCreature(Player player, Creature creature)
        {
            int n = 0;
            if (Ground != null)
                n++;

	        if (Items != null)
	        {
	            n += Items.Count - _downItemCount;
		        if (n >= 10)
			        return -1;
	        }

            if (Creatures != null)
            {
                foreach (Creature c in Creatures)
                {
                    if (c == creature)
                        return n;
                    
                    if (player.CanSeeCreature(c))
                    {
                        if (++n >= 10) return -1;
                    }
                }
            }
	        return -1;
        }
        public int GetStackposOfItem(Player player, Item item)
        {
	        int n = 0;
	        if (Ground != null)
            {
		        if (Ground == item)
			        return n;
		        ++n;
	        }

	        if (Items != null)
            {
		        if (item.IsAlwaysOnTop)
                {
                    for (int i = TopItemsIndex; i < Items.Count; i++)
                    {
                        if (Items[i] == item)
                            return n;
                        if (++n == 10)
                            return -1;
                    }
		        }
                else
                {
	                n += Items.Count - _downItemCount;
			        if (n >= 10)
				        return -1;
		        }
	        }

	        if (Creatures != null)
            {
                foreach (Creature creature in Creatures)
                {
                    if (player.CanSeeCreature(creature))
                    {
                        if (++n >= 10)
                            return -1;
                    }
                }
	        }

	        if (Items != null)
            {
		        if (!item.IsAlwaysOnTop)
                {
                    for(int i = 0; i < TopItemsIndex; i++)
                    {
				        if (Items[i] == item)
					        return n;
                        if (++n >= 10)
					        return -1;
			        }
		        }
	        }
	        return -1;
        }

        public int GetClientIndexOfCreature(Player player, Creature creature)
        {
            int n = 0;
            if (Ground != null)
                n++;

            if (Items != null)
            {
                n += Items.Count - _downItemCount;
            }

            if (Creatures != null)
            {
                foreach (Creature c in Creatures)
                {
                    if (c == creature)
                        return n;

                    if (player.CanSeeCreature(c))
                    {
                        ++n;
                    }
                }
            }
            return -1;
        }
        
        public Teleport GetTeleportItem()
        {
	        if (Flags.HasFlag(TileFlags.Teleport))
		        return null;

            if (Items != null)
                return Items.OfType<Teleport>().LastOrDefault();
	        return null;
        }
        public TrashHolder GetTrashHolder()
        {
	        if (!Flags.HasFlag(TileFlags.TrashHolder))
		        return null;

	        if (Ground is TrashHolder)
		        return Ground as TrashHolder;

            if (Items != null)
                return Items.OfType<TrashHolder>().LastOrDefault();
	        return null;
        }
        public Mailbox GetMailbox()
        {
	        if (!Flags.HasFlag(TileFlags.MailBox))
		        return null;

	        if (Ground is Mailbox)
                return Ground as Mailbox;

            if (Items != null)
                return Items.OfType<Mailbox>().LastOrDefault();
	        return null;
        }

        public List<Item> MakeItemList()
        {
            return Items ?? (Items = new List<Item>());
        }
        public List<Creature> MakeCreatureList()
        {
            return Creatures ?? (Creatures = new List<Creature>());
        }
    }
}
