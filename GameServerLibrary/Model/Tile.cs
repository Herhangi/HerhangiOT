using System.Collections.Generic;

namespace HerhangiOT.GameServerLibrary.Model
{
    public class Tile : Thing
    {
        public Position Position { get; set; }
        public TileFlags Flags { get; private set; }

        public List<Item> Items { get; private set; }
        public List<Creature> Creatures { get; private set; }

        public Item Ground { get; private set; }

        private int _downItemCount;

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
                SetFlag(TileFlags.FloorChange);

                switch (item.FloorChangeDirection)
                {
                    case FloorChangeDirection.Down:
                        SetFlag(TileFlags.FloorChangeDown);
                        break;
                    case FloorChangeDirection.North:
                        SetFlag(TileFlags.FloorchangeNorth);
                        break;
                    case FloorChangeDirection.South:
                        SetFlag(TileFlags.FloorChangeSouth);
                        break;
                    case FloorChangeDirection.East:
                        SetFlag(TileFlags.FloorChangeEast);
                        break;
                    case FloorChangeDirection.West:
                        SetFlag(TileFlags.FloorChangeWest);
                        break;
                    case FloorChangeDirection.SouthAlt:
                        SetFlag(TileFlags.FloorChangeSouthAlt);
                        break;
                    case FloorChangeDirection.EastAlt:
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

                        if(!isInserted)
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
        }

        public ReturnTypes QueryAdd(Thing thing, CylinderFlags cFlags)
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

        public void AddCreature(Creature creature)
        {
            if(Creatures == null)
                Creatures = new List<Creature>();
            Creatures.Add(creature);
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
