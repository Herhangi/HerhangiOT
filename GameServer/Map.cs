using System;
using System.Collections.Generic;
using System.IO;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer
{
    public class Map
    {
        public static Map Instance { get; private set; }

        public const int MaxViewportX = 11;
        public const int MaxViewportY = 11;
        public const int MaxClientViewportX = 8;
        public const int MaxClientViewportY = 6;
        public const int MapMaxLayers = 16;

        public const int FloorBits = 3;
        public const int FloorSize = 1 << FloorBits;
        public const int FloorMask = FloorSize - 1;

        protected Dictionary<uint, Town> Towns;
        protected Dictionary<uint, House> Houses; 
        protected Dictionary<string, Position> Waypoints;

        protected Dictionary<Position, HashSet<Creature>> SpectatorCache = new Dictionary<Position, HashSet<Creature>>(); 
        protected Dictionary<Position, HashSet<Creature>> PlayersSpectatorCache = new Dictionary<Position, HashSet<Creature>>(); 
 
        protected Floor[] Floors;
        protected string SpawnFile;
        protected string HouseFile;
        protected string Description;

        public ushort Width { get; protected set; }
        public ushort Height { get; protected set; }

        public static readonly List<Tuple<int, int>> ExtendedRelList = new List<Tuple<int, int>>
        {
                                                                     new Tuple<int, int>(0, -2),
                                        new Tuple<int, int>(-1, -1), new Tuple<int, int>(0, -1), new Tuple<int, int>(1, -1),
            new Tuple<int, int>(-2, 0), new Tuple<int, int>(-1,  0),                             new Tuple<int, int>(1,  0), new Tuple<int, int>(2, 0),
                                        new Tuple<int, int>(-1,  1), new Tuple<int, int>(0,  1), new Tuple<int, int>(1,  1),
                                                                     new Tuple<int, int>(0,  2)
        };
        public static readonly List<Tuple<int, int>> NormalRelList = new List<Tuple<int, int>>
        {
            new Tuple<int, int>(-1, -1), new Tuple<int, int>(0, -1), new Tuple<int, int>(1, -1),
            new Tuple<int, int>(-1,  0),                             new Tuple<int, int>(1,  0),
            new Tuple<int, int>(-1,  1), new Tuple<int, int>(0,  1), new Tuple<int, int>(1,  1),
        };

        public static bool Load()
        {
            Logger.LogOperationStart("Loading Map<"+ConfigManager.Instance[ConfigStr.MapName]+">");
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
            if(Floors[z] == null)
                return null;
            Floor floor = Floors[z];
            return floor.Tiles[x][y];
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

        #region OTBM Operations
        private bool LoadOtbm()
        {
            FileLoader fileLoader = new FileLoader();
            if (!fileLoader.OpenFile("Data/World/"+ConfigManager.Instance[ConfigStr.MapName]+".otbm", "OTBM"))
            {
                Logger.LogOperationFailed("Could not open map file: " + ConfigManager.Instance[ConfigStr.MapName]);
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
        #endregion

        public Town GetTown(ushort townId)
        {
            if (Towns.ContainsKey(townId))
                return Towns[townId];
            return null;
        }

        //TODO: CHANGE FLOORING AND MAP STORAGE SYSTEM
        private void GetSpectatorsInternal(ref HashSet<Creature> list, Position centerPos, int minRangeX, int maxRangeX, int minRangeY, int maxRangeY, int minRangeZ, int maxRangeZ, bool onlyPlayers)
        {
	        int minY = centerPos.Y + minRangeY;
	        int minX = centerPos.Y + minRangeX;
	        int maxY = centerPos.Y + maxRangeY;
	        int maxX = centerPos.Y + maxRangeX;

	        int minoffset = centerPos.Z - maxRangeZ;
            ushort x1 = (ushort)Math.Min(ushort.MaxValue, Math.Max(0, (minX + minoffset)));
	        ushort y1 = (ushort)Math.Min(ushort.MaxValue, Math.Max(0, (minY + minoffset)));
            
	        int maxoffset = centerPos.Z - minRangeZ;
	        ushort x2 = (ushort)Math.Min(ushort.MaxValue, Math.Max(0, (maxX + maxoffset)));
	        ushort y2 = (ushort)Math.Min(ushort.MaxValue, Math.Max(0, (maxY + maxoffset)));

	        int startx1 = x1 - (x1 % FloorSize);
	        int starty1 = y1 - (y1 % FloorSize);
	        int endx2 = x2 - (x2 % FloorSize);
	        int endy2 = y2 - (y2 % FloorSize);

            for (int nz = minRangeZ; nz <= maxRangeZ; nz++)
            {
                for (int ny = starty1; ny <= endy2; ny++)
                {
                    for (int nx = startx1; nx <= endx2; nx++)
                    {
                        Floor floor = Floors[nz];
                        Tile tile = floor.Tiles[ny][nx];
                        if (tile.Creatures != null && tile.Creatures.Count > 0)
                        {
                            foreach (Creature creature in tile.Creatures)
                            {
                                if (onlyPlayers)
                                {
                                    if (creature is Player)
                                        list.Add(creature);
                                }
                                else
                                    list.Add(creature);
                            }
                        }
                    }    
                }    
            }
        }


        public void GetSpectators(ref HashSet<Creature> list, Position position, bool multifloor = false, bool onlyPlayers = false,
            int minRangeX = 0, int maxRangeX = 0, int minRangeY = 0, int maxRangeY = 0)
        {
            if (position.Z >= MapMaxLayers)
                return;

            bool foundCache = false;
            bool cacheResult = false;
             
            minRangeX = (minRangeX == 0 ? -MaxViewportX : -minRangeX);
            maxRangeX = (maxRangeX == 0 ? MaxViewportX : maxRangeX);
            minRangeY = (minRangeY == 0 ? -MaxViewportY : -minRangeY);
            maxRangeY = (maxRangeY == 0 ? MaxViewportY : maxRangeY);

	        if (minRangeX == -MaxViewportX && maxRangeX == MaxViewportX && minRangeY == -MaxViewportY && maxRangeY == MaxViewportY && multifloor)
            {
		        if (onlyPlayers)
                {
		            if (PlayersSpectatorCache.ContainsKey(position))
		            {
		                if (list == null || list.Count == 0)
		                    list = PlayersSpectatorCache[position];
		                else
		                {
		                    list.UnionWith(PlayersSpectatorCache[position]);
		                }

		                foundCache = true;
		            }
		        }

		        if (!foundCache)
                {
                    if (SpectatorCache.ContainsKey(position))
                    {
                        if (!onlyPlayers)
                        {
                             if (list == null || list.Count == 0)
		                        list = SpectatorCache[position];
		                    else
		                    {
		                        list.UnionWith(SpectatorCache[position]);
		                    }
                        }
                        else
                        {
                            foreach (Creature spectator in SpectatorCache[position])
                            {
                                if (spectator is Player)
                                {
                                    list.Add(spectator);
                                }
                            }
                        }

				        foundCache = true;
			        }
                    else
                    {
				        cacheResult = true;
			        }
		        }
	        }

	        if (!foundCache) {
		        int minRangeZ;
		        int maxRangeZ;

		        if (multifloor)
                {
			        if (position.Z > 7) {
				        //underground
				        //8->15
			            minRangeZ = Math.Max(position.Z - 2, 0);
			            maxRangeZ = Math.Min(position.Z + 2, MapMaxLayers - 1);
			        } else if (position.Z == 6) {
				        minRangeZ = 0;
				        maxRangeZ = 8;
			        } else if (position.Z == 7) {
				        minRangeZ = 0;
				        maxRangeZ = 9;
			        } else {
				        minRangeZ = 0;
				        maxRangeZ = 7;
			        }
		        } else {
			        minRangeZ = position.Z;
			        maxRangeZ = position.Z;
		        }

		        GetSpectatorsInternal(ref list, position, minRangeX, maxRangeX, minRangeY, maxRangeY, minRangeZ, maxRangeZ, onlyPlayers);

		        if (cacheResult) {
			        if (onlyPlayers) {
				        PlayersSpectatorCache[position] = list;
			        } else {
				        SpectatorCache[position] = list;
			        }
		        }
	        }
        }

        public bool PlaceCreature(Position centerPosition, Creature creature, bool extendedPos = false, bool forceLogin = false)
        {
	        bool foundTile;
	        bool placeInPZ;

	        Tile tile = GetTile(centerPosition.X, centerPosition.Y, centerPosition.Z);
	        if (tile != null)
	        {
	            placeInPZ = tile.Flags.HasFlag(TileFlags.ProtectionZone);
		        ReturnTypes ret = tile.QueryAdd(creature, CylinderFlags.IgnoreBlockItem);
		        foundTile = forceLogin || ret == ReturnTypes.NoError;
	        }
            else
            {
		        placeInPZ = false;
		        foundTile = false;
	        }

	        if (!foundTile)
            {
		        List<Tuple<int, int>> relList = (extendedPos ? ExtendedRelList : NormalRelList);

		        if (extendedPos)
                {
                    relList.Shuffle(0, 5);
                    relList.Shuffle(5, 6);
		        }
                else
		        {
		            relList.Shuffle();
		        }

		        foreach (Tuple<int, int> pos in relList) {
                    Position tryPos = new Position((ushort)(centerPosition.X + pos.Item1), (ushort)(centerPosition.Y + pos.Item2), centerPosition.Z);

			        tile = GetTile(tryPos.X, tryPos.Y, tryPos.Z);
			        if (tile == null || (placeInPZ && !tile.Flags.HasFlag(TileFlags.ProtectionZone)))
				        continue;

			        if (tile.QueryAdd(creature, CylinderFlags.None) == ReturnTypes.NoError) {
				        if (!extendedPos || IsSightClear(centerPosition, tryPos, false)) {
					        foundTile = true;
					        break;
				        }
			        }
		        }

		        if (!foundTile) {
			        return false;
		        }
	        }

	        int index = 0;
	        uint flags = 0;
	        Item toItem = null;

	        //Cylinder* toCylinder = tile->queryDestination(index, *creature, &toItem, flags); //TODO
	        //toCylinder->internalAddThing(creature);

	        tile.AddCreature(creature);
	        return true;
        }

        private bool IsSightClear(Position fromPos, Position toPos, bool floorCheck)
        {
	        if (floorCheck && fromPos.Z != toPos.Z)
		        return false;

	        // Cast two converging rays and see if either yields a result.
	        return CheckSightLine(fromPos, toPos) || CheckSightLine(toPos, fromPos);
        }

        private bool CheckSightLine(Position fromPos, Position toPos)
        {
	        if (fromPos == toPos)
		        return true;

	        Position start = new Position(fromPos.Z > toPos.Z ? toPos : fromPos);
	        Position destination = new Position(fromPos.Z > toPos.Z ? fromPos : toPos);

	        sbyte mx = (sbyte)(start.X < destination.X ? 1 : start.X == destination.X ? 0 : -1);
	        sbyte my = (sbyte)(start.Y < destination.Y ? 1 : start.Y == destination.Y ? 0 : -1);

	        int A = Position.GetOffsetY(destination, start);
	        int B = Position.GetOffsetX(start, destination);
	        int C = -(A * destination.X + B * destination.Y);

	        while (start.X != destination.X || start.Y != destination.Y)
            {
		        int moveHor = Math.Abs(A * (start.X + mx) + B * (start.Y) + C);
		        int moveVer = Math.Abs(A * (start.X) + B * (start.Y + my) + C);
                int moveCross = Math.Abs(A * (start.X + mx) + B * (start.Y + my) + C);

		        if (start.Y != destination.Y && (start.X == destination.X || moveHor > moveVer || moveHor > moveCross)) {
			        start.Y += (ushort)my;
		        }

		        if (start.X != destination.X && (start.Y == destination.Y || moveVer > moveHor || moveVer > moveCross)) {
			        start.X += (ushort)mx;
		        }

		        Tile tile = GetTile(start.X, start.Y, start.Z);
		        if (tile != null && tile.HasProperty(ItemProperties.BlockProjectile))
			        return false;
	        }

	        // now we need to perform a jump between floors to see if everything is clear (literally)
	        while (start.Z != destination.Z) {
		        Tile tile = GetTile(start.X, start.Y, start.Z);
		        if (tile != null && tile.GetThingCount() > 0) {
			        return false;
		        }

		        start.Z++;
	        }

	        return true;
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
        Waypoint = 16,
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
        Charges = 22,
    }
    #endregion
}
