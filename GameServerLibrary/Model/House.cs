using System.Collections.Generic;

namespace HerhangiOT.GameServerLibrary.Model
{
    public class House
    {
        public uint HouseId { get; set; }
        public List<HouseTile> Tiles { get; private set; }

        public House()
        {
            Tiles = new List<HouseTile>();
        }

        public void AddTile(HouseTile tile)
        {
            tile.SetFlag(TileFlags.ProtectionZone);
            Tiles.Add(tile);
        }
    }
}
