namespace HerhangiOT.GameServerLibrary.Model
{
    public class Position
    {
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public byte Z { get; set; }

        public Position(ushort x, ushort y, byte z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Position(Position pos)
        {
            X = pos.X;
            Y = pos.Y;
            Z = pos.Z;
        }

        public LocationType Type
        {
            get
            {
                if (X == 0xFFFF)
                {
                    if ((Y & 0x40) != 0)
                    {
                        return LocationType.Container;
                    }
                    else
                    {
                        return LocationType.Slot;
                    }
                }
                else
                {
                    return LocationType.Ground;
                }
            }
        }
    }
}
