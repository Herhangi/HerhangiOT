using System.Runtime.InteropServices;

namespace HerhangiOT.GameServer.IO
{
    enum OTBM_AttrTypes_t
    {
        OTBM_ATTR_DESCRIPTION = 1,
        OTBM_ATTR_EXT_FILE = 2,
        OTBM_ATTR_TILE_FLAGS = 3,
        OTBM_ATTR_ACTION_ID = 4,
        OTBM_ATTR_UNIQUE_ID = 5,
        OTBM_ATTR_TEXT = 6,
        OTBM_ATTR_DESC = 7,
        OTBM_ATTR_TELE_DEST = 8,
        OTBM_ATTR_ITEM = 9,
        OTBM_ATTR_DEPOT_ID = 10,
        OTBM_ATTR_EXT_SPAWN_FILE = 11,
        OTBM_ATTR_RUNE_CHARGES = 12,
        OTBM_ATTR_EXT_HOUSE_FILE = 13,
        OTBM_ATTR_HOUSEDOORID = 14,
        OTBM_ATTR_COUNT = 15,
        OTBM_ATTR_DURATION = 16,
        OTBM_ATTR_DECAYING_STATE = 17,
        OTBM_ATTR_WRITTENDATE = 18,
        OTBM_ATTR_WRITTENBY = 19,
        OTBM_ATTR_SLEEPERGUID = 20,
        OTBM_ATTR_SLEEPSTART = 21,
        OTBM_ATTR_CHARGES = 22
    };

    enum OTBM_NodeTypes_t
    {
        OTBM_ROOTV1 = 1,
        OTBM_MAP_DATA = 2,
        OTBM_ITEM_DEF = 3,
        OTBM_TILE_AREA = 4,
        OTBM_TILE = 5,
        OTBM_ITEM = 6,
        OTBM_TILE_SQUARE = 7,
        OTBM_TILE_REF = 8,
        OTBM_SPAWNS = 9,
        OTBM_SPAWN_AREA = 10,
        OTBM_MONSTER = 11,
        OTBM_TOWNS = 12,
        OTBM_TOWN = 13,
        OTBM_HOUSETILE = 14,
        OTBM_WAYPOINTS = 15,
        OTBM_WAYPOINT = 16
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct OTBM_root_header
    {
        uint version;
        ushort width;
        ushort height;
        uint majorVersionItems;
        uint minorVersionItems;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct OTBM_Destination_coords
    {
        ushort _x;
        ushort _y;
        byte _z;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct OTBM_Tile_coords
    {
        byte _x;
        byte _y;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct OTBM_HouseTile_coords
    {
        byte _x;
        byte _y;
        uint _houseid;
    };

    class IOMap
    {
    }
}
