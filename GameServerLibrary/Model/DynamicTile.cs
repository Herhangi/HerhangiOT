namespace HerhangiOT.GameServerLibrary.Model
{
    public class DynamicTile : Tile
    {
        public DynamicTile(int x, int y, int z) : base(x, y, z)
        {
            SetFlag(TileFlags.DynamicTile);
        }
    }
}
