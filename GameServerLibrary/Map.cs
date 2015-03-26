using System.Collections.Generic;
using System.IO;
using HerhangiOT.GameServerLibrary.Model;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServerLibrary
{
    public class Map
    {
        public static Map Instance { get; private set; }

        public const int MaxViewportX = 11;
        public const int MaxViewportY = 11;
        public const int MaxClientViewportX = 8;
        public const int MaxClientViewportY = 6;
        public const int MapMaxLayers = 16;

        protected Dictionary<uint, Town> Towns;
        protected Dictionary<uint, House> Houses; 
        protected Dictionary<string, Position> Waypoints;
 
        protected Floor[] Floors;
        protected string SpawnFile;
        protected string HouseFile;
        protected string Description;

        public ushort Width { get; protected set; }
        public ushort Height { get; protected set; }

        public static bool Load()
        {
            Logger.LogOperationStart("Loading Map");
            Instance = new Map();
            Instance.Floors = new Floor[MapMaxLayers];
            Instance.Towns = new Dictionary<uint, Town>();
            Instance.Houses = new Dictionary<uint, House>();
            Instance.Waypoints = new Dictionary<string, Position>();

            if (!Instance.LoadOtbm())
                return false;

            //LOAD SPAWNS
            //LOAD HOUSES

            Logger.LogOperationDone();
            return true;
        }

        public static bool Save()
        {
            return true;
        }

        public Tile GetTile(ushort x, ushort y, byte z)
        {
            return null;
        }

        public void SetTile(Tile tile)
        {
            Position pos = tile.Position;
            if (pos.Z > MapMaxLayers)
            {
                Logger.Log(LogLevels.Error, "Attempt to set tile on invalid coordinate! X:"+pos.X+", Y:"+pos.Y+", Z:"+pos.Z);
                return;
            }

            if(Floors[pos.Z] == null)
                Floors[pos.Z] = new Floor();
            Floor floor = Floors[pos.Z];

            floor.Tiles[pos.X][pos.Y] = tile;
        }

        protected class Floor
        {
            public Tile[][] Tiles;

            public Floor()
            {
                Tiles = new Tile[Instance.Width][];
                for(int i = 0; i < Instance.Width; i++)
                    Tiles[i] = new Tile[Instance.Height];
            }
        }

        private bool LoadOtbm()
        {
            FileLoader fileLoader = new FileLoader();
            if (!fileLoader.OpenFile("Data/World/"+ConfigManager.Instance[ConfigStr.MAP_NAME]+".otbm", "OTBM"))
            {
                Logger.LogOperationFailed("Could not open map file: " + ConfigManager.Instance[ConfigStr.MAP_NAME]);
                return false;
            }

            byte type;
            NodeStruct root = fileLoader.GetChildNode(null, out type);
            
            BinaryReader props;
            if (!fileLoader.GetProps(root, out props))
            {
                Logger.LogOperationFailed("Could not read root property!");
                return false;
            }
            props.ReadByte();
            uint version = props.ReadUInt32();
            Width = props.ReadUInt16();
            Height = props.ReadUInt16();
            uint majorVersionItems = props.ReadUInt32();
            uint minorVersionItems = props.ReadUInt32();

            if (version <= 0)
            {
                //In otbm version 1 the count variable after splashes/fluidcontainers and stackables
                //are saved as attributes instead, this solves alot of problems with items
                //that is changed (stackable/charges/fluidcontainer/splash) during an update.
                Logger.LogOperationFailed("This map need to be upgraded by using the latest map editor version to be able to load correctly.");
                return false;
            }

            if (version > 2)
            {
                Logger.LogOperationFailed("Unknown OTBM version detected.");
                return false;
            }

            if (majorVersionItems < 3)
            {
                Logger.LogOperationFailed("This map need to be upgraded by using the latest map editor version to be able to load correctly.");
                return false;
            }
            
	        if (majorVersionItems > ItemManager.DwMajorVersion)
            {
		        Logger.LogOperationFailed("The map was saved with a different items.otb version, an upgraded items.otb is required.");
		        return false;
	        }

	        if (minorVersionItems < (uint)OtbClientVersion.V810)
            {
		        Logger.LogOperationFailed("This map needs to be updated.");
		        return false;
	        }

	        if (minorVersionItems > ItemManager.DwMinorVersion)
		        Logger.Log(LogLevels.Warning, "This map needs an updated items.otb.");

            NodeStruct nodeMap = fileLoader.GetChildNode(root, out type);
            if (type != (byte)OtbmNodeTypes.MapData)
            {
                Logger.LogOperationFailed("Could not read data node.");
                return false;
            }

            if (!fileLoader.GetProps(nodeMap, out props))
            {
                Logger.LogOperationFailed("Could not read map data attributes.");
                return false;
            }

            while (props.PeekChar() != -1)
            {
                byte attribute = props.ReadByte();
                switch ((OtbmAttributes)attribute)
                {
                    case OtbmAttributes.Description:
                        Description = props.GetString();
                        break;
                    case OtbmAttributes.ExtSpawnFile:
                        SpawnFile = string.Format("Data/World/{0}", props.GetString());
                        break;
                    case OtbmAttributes.ExtHouseFile:
                        HouseFile = string.Format("Data/World/{0}", props.GetString());
                        break;
                    default:
                        Logger.LogOperationFailed("Map file unknown header node!");
                        return false;
                }
            }

            NodeStruct nodeMapData = fileLoader.GetChildNode(nodeMap, out type);
            while (nodeMapData != null)
            {
                if (type == (byte)OtbmNodeTypes.TileArea)
                {
                    if (!ParseTileArea(nodeMapData, fileLoader))
                    {
                        Logger.LogOperationFailed("Could not parse tile area!");
                        return false;
                    }
                }
                else if (type == (byte) OtbmNodeTypes.Towns)
                {
                    if (!ParseTowns(nodeMapData, fileLoader))
                    {
                        Logger.LogOperationFailed("Could not parse towns!");
                        return false;
                    }
                }
                else if (type == (byte) OtbmNodeTypes.Waypoints && version > 1)
                {
                    if (!ParseWaypoints(nodeMapData, fileLoader))
                    {
                        Logger.LogOperationFailed("Could not parse waypoints!");
                        return false;
                    }
                }
                else
                {
                    Logger.LogOperationFailed("Unknown map node!");
                    return false;
                }

                nodeMapData = fileLoader.GetNextNode(nodeMapData, out type);
            }

            fileLoader.Close();
            return true;
        }

        private bool ParseTileArea(NodeStruct node, FileLoader fileLoader)
        {
            byte type;

            BinaryReader props;
            if (!fileLoader.GetProps(node, out props))
                return false;

            int baseX = props.ReadUInt16();
            int baseY = props.ReadUInt16();
            int baseZ = props.ReadByte();

            NodeStruct nodeTile = fileLoader.GetChildNode(node, out type);
            while (nodeTile != null)
            {
                if (type != (byte) OtbmNodeTypes.Tile && type != (byte) OtbmNodeTypes.HouseTile)
                {
                    Logger.Log(LogLevels.Error, "NOT TILE OR HOUSETILE");
                    return false;
                }

                if (!fileLoader.GetProps(nodeTile, out props))
                    return false;

                int pX = baseX + props.ReadByte();
                int pY = baseY + props.ReadByte();
                int pZ = baseZ;

                TileFlags tileFlags = TileFlags.None;
                bool isHouseTile = false;
                Item groundItem = null;
                House house = null;
                Tile tile = null;

                if (type == (byte) OtbmNodeTypes.HouseTile)
                {
                    //TODO: HOUSES
                    uint houseId = props.ReadUInt32();

                    if (!Houses.ContainsKey(houseId))
                        Houses.Add(houseId, new House {HouseId = houseId});

                    house = Houses[houseId];
                    tile = new HouseTile(pX, pY, pZ, house);
                    house.AddTile((HouseTile)tile);
                    isHouseTile = true;
                }

                while (props.PeekChar() != -1)
                {
                    byte attribute = props.ReadByte();

                    switch ((OtbmAttributes)attribute)
                    {
                        case OtbmAttributes.TileFlags:
                            TileFlags flags = (TileFlags)props.ReadUInt32();

                            if (flags.HasFlag(TileFlags.ProtectionZone))
                                tileFlags |= TileFlags.ProtectionZone;
                            else if (flags.HasFlag(TileFlags.NoPvpZone))
                                tileFlags |= TileFlags.NoPvpZone;
                            else if (flags.HasFlag(TileFlags.PvpZone))
                                tileFlags |= TileFlags.PvpZone;

                            if (flags.HasFlag(TileFlags.NoLogout))
                                tileFlags |= TileFlags.NoLogout;
                            break;

                        case OtbmAttributes.Item:
                            Item item = Item.CreateItem(props);
                            if (item == null) return false;

                            if (isHouseTile && item.IsMovable)
                            {
                                Logger.Log(LogLevels.Warning, "Moveable item found in house: "+house.HouseId);
                                item.DecrementReferenceCounter();
                            }
                            else
                            {
                                if (item.Count <= 0)
                                    item.Count = 1;

                                if (tile != null)
                                {
                                    tile.InternalAddThing(item);
                                    item.StartDecaying();
                                    item.IsLoadedFromMap = true;
                                }
                                else if (item.IsGroundTile)
                                {
                                    groundItem = item;
                                }
                                else
                                {
                                    tile = CreateTile(groundItem, item, pX, pY, pZ);
                                    tile.InternalAddThing(item);
                                    item.StartDecaying();
                                    item.IsLoadedFromMap = true;
                                }
                            }
                            break;

                        default:
                            Logger.Log(LogLevels.Warning, "Unknown tile attribute on; X:"+pX+", Y:"+pY+", Z:"+pZ+"!");
                            return false;
                    }
                }

                NodeStruct nodeItem = fileLoader.GetChildNode(nodeTile, out type);
                while (nodeItem != null)
                {
                    if (type != (byte) OtbmNodeTypes.Item)
                        return false;

                    BinaryReader stream;
                    if (!fileLoader.GetProps(nodeItem, out stream))
                        return false;

                    Item item = Item.CreateItem(stream);
                    if (item == null)
                        return false;

                    //TODO: UNSERIALIZE

                    if (isHouseTile && item.IsMovable)
                    {
                        Logger.Log(LogLevels.Warning, "Moveable item found in house: " + house.HouseId);
                        item.DecrementReferenceCounter();
                    }
                    else
                    {
                        if (item.Count <= 0)
                            item.Count = 1;

                        if (tile != null)
                        {
                            tile.InternalAddThing(item);
                            item.StartDecaying();
                            item.IsLoadedFromMap = true;
                        }
                        else if (item.IsGroundTile)
                        {
                            groundItem = item;
                        }
                        else
                        {
                            tile = CreateTile(groundItem, item, pX, pY, pZ);
                            tile.InternalAddThing(item);
                            item.StartDecaying();
                            item.IsLoadedFromMap = true;
                        }
                    }

                    nodeItem = nodeItem.Next;
                }

                if (tile == null)
                    tile = CreateTile(groundItem, null, pX, pY, pZ);

                tile.SetFlag(tileFlags);
                SetTile(tile);

                nodeTile = fileLoader.GetNextNode(nodeTile, out type);
            }

            return true;
        }

        private bool ParseTowns(NodeStruct node, FileLoader fileLoader)
        {
            byte type;
            NodeStruct nodeTown = fileLoader.GetChildNode(node, out type);

            while (nodeTown != null)
            {
                if (type != (byte)OtbmNodeTypes.Town)
                    return false;

                BinaryReader props;
                if (!fileLoader.GetProps(nodeTown, out props))
                    return false;

                uint townId = props.ReadUInt32();
                if(!Towns.ContainsKey(townId))
                    Towns.Add(townId, new Town {TownId = townId});
                Town town = Towns[townId];

                town.TownName = props.GetString();
                town.TemplePosition = new Position(props.ReadUInt16(), props.ReadUInt16(), props.ReadByte());

                nodeTown = fileLoader.GetNextNode(nodeTown, out type);
            }

            return true;
        }

        private bool ParseWaypoints(NodeStruct node, FileLoader fileLoader)
        {
            byte type;
            NodeStruct nodeWaypoint = fileLoader.GetChildNode(node, out type);

			while (nodeWaypoint != null)
            {
				if (type != (byte)OtbmNodeTypes.Waypoint) 
					return false;

                BinaryReader props;
				if (!fileLoader.GetProps(nodeWaypoint, out props))
					return false;

                //(name, new Position(x, y, z)
                Waypoints.Add(props.GetString(), new Position(props.ReadUInt16(), props.ReadUInt16(), props.ReadByte()));

				nodeWaypoint = fileLoader.GetNextNode(nodeWaypoint, out type);
			}
            return true;
        }

        //We will see if we need StaticTile, DynamicTile system in the future, keeping this method for this reason
        private static Tile CreateTile(Item ground, Item item, int pX, int pY, int pZ)
        {
            if (ground == null)
            {
                return new Tile(pX, pY, pZ);
            }

            Tile tile = new Tile(pX, pY, pZ);
            tile.InternalAddThing(ground);
            ground.StartDecaying();
            return tile;
        }
    }

    #region OTBM ENUMS
    public enum OtbmNodeTypes : byte
    {
        RootV1 = 1,
        MapData = 2,
        ItemDef = 3,
        TileArea = 4,
        Tile = 5,
        Item = 6,
        TileSquare = 7,
        TileRef = 8,
        Spawns = 9,
        SpawnArea = 10,
        Monster = 11,
        Towns = 12,
        Town = 13,
        HouseTile = 14,
        Waypoints = 15,
        Waypoint = 16
    }

    public enum OtbmAttributes : byte
    {
        Description = 1,
        ExtFile = 2,
        TileFlags = 3,
        ActionId = 4,
        UniqueId = 5,
        Text = 6,
        Desc = 7,
        TeleDest = 8,
        Item = 9,
        DepotId = 10,
        ExtSpawnFile = 11,
        RuneCharges = 12,
        ExtHouseFile = 13,
        HouseDoorId = 14,
        Count = 15,
        Duration = 16,
        DecayingState = 17,
        WrittenDate = 18,
        WrittenBy = 19,
        SleeperGuid = 20,
        SleepStart = 21,
        Charges = 22
    }
    #endregion
}
