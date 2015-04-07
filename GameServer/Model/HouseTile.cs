using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class HouseTile : Tile
    {
        public House House { get; private set; }

        public HouseTile(int x, int y, int z, House house) : base(x,y,z)
        {
            House = house;
            SetFlag(TileFlags.House);
        }
    }
}
