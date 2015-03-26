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
