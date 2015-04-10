using System;
using System.Runtime.CompilerServices;
using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
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
	    public static int GetOffsetX(Position p1, Position p2)
        {
		    return p1.X - p2.X;
	    }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public static int GetOffsetY(Position p1, Position p2)
        {
		    return p1.Y - p2.Y;
	    }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public static int GetOffsetZ(Position p1, Position p2)
        {
		    return p1.Z - p2.Z;
	    }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    public static int GetDistanceX(Position p1, Position p2)
        {
            return Math.Abs(GetOffsetX(p1, p2));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDistanceY(Position p1, Position p2)
        {
            return Math.Abs(GetOffsetY(p1, p2));
	    }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDistanceZ(Position p1, Position p2)
        {
            return Math.Abs(GetOffsetZ(p1, p2));
	    }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreInRange(Position p1, Position p2, int deltax, int deltay, int deltaz)
        {
		    return GetDistanceX(p1, p2) <= deltax && GetDistanceY(p1, p2) <= deltay && GetDistanceZ(p1, p2) <= deltaz;
	    }

        public static Position GetNextPosition(Directions direction, Position oldPosition)
        {
            Position pos = new Position(oldPosition);
            switch (direction)
            {
                case Directions.North:
                    pos.Y--;
                    break;

                case Directions.South:
                    pos.Y++;
                    break;

                case Directions.West:
                    pos.X--;
                    break;

                case Directions.East:
                    pos.X++;
                    break;

                case Directions.SouthWest:
                    pos.X--;
                    pos.Y++;
                    break;

                case Directions.NorthWest:
                    pos.X--;
                    pos.Y--;
                    break;

                case Directions.NorthEast:
                    pos.X++;
                    pos.Y--;
                    break;

                case Directions.SouthEast:
                    pos.X++;
                    pos.Y++;
                    break;
            }

            return pos;
        }

        public LocationTypes Type
        {
            get
            {
                if (X == 0xFFFF)
                {
                    if ((Y & 0x40) != 0)
                    {
                        return LocationTypes.Container;
                    }
                    else
                    {
                        return LocationTypes.Slot;
                    }
                }
                else
                {
                    return LocationTypes.Ground;
                }
            }
        }
    }
}
