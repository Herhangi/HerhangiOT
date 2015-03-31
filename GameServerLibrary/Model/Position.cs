using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public static int GetOffsetX(Position p1, Position p2) {
		    return p1.X - p2.X;
	    }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public static int GetOffsetY(Position p1, Position p2) {
		    return p1.Y - p2.Y;
	    }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public static int GetOffsetZ(Position p1, Position p2) {
		    return p1.Z - p2.Z;
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
