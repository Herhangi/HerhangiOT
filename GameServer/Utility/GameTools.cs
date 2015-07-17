using System;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;

namespace HerhangiOT.GameServer.Utility
{
    public static class GameTools
    {
        private static readonly byte[] ClientToServerFluidMap =
        {
            (byte)FluidTypes.None,
            (byte)FluidTypes.Water,
            (byte)FluidTypes.Mana,
            (byte)FluidTypes.Beer,
            (byte)FluidTypes.Mud,
            (byte)FluidTypes.Blood,
            (byte)FluidTypes.Slime,
            (byte)FluidTypes.Rum,
            (byte)FluidTypes.Lemonade,
            (byte)FluidTypes.Milk,
            (byte)FluidTypes.Wine,
            (byte)FluidTypes.Life,
            (byte)FluidTypes.Urine,
            (byte)FluidTypes.Oil,
            (byte)FluidTypes.FruitJuice,
            (byte)FluidTypes.CoconutMilk,
            (byte)FluidTypes.Tea,
            (byte)FluidTypes.Mead
        };

        public static Directions GetDirectionTo(Position from, Position to)
        {
            Directions dir;

            int offsetX = Position.GetOffsetX(from, to);

            if (offsetX < 0)
            {
                dir = Directions.East;
                offsetX = Math.Abs(offsetX);
            }
            else
            {
                dir = Directions.West;
            }

            int offsetY = Position.GetOffsetY(from, to);
            if (offsetY >= 0)
            {
                if (offsetY > offsetX)
                {
                    dir = Directions.North;
                }
                else if (offsetY == offsetX)
                {
                    if (dir == Directions.East)
                    {
                        dir = Directions.NorthEast;
                    }
                    else
                    {
                        dir = Directions.NorthWest;
                    }
                }
            }
            else
            {
                offsetY = Math.Abs(offsetY);
                if (offsetY > offsetX)
                {
                    dir = Directions.South;
                }
                else if (offsetY == offsetX)
                {
                    if (dir == Directions.East)
                    {
                        dir = Directions.SouthEast;
                    }
                    else
                    {
                        dir = Directions.SouthWest;
                    }
                }
            }
            return dir;
        }

        public static byte ServerFluidToClient(byte serverFluid)
        {
            for (byte i = 0; i < ClientToServerFluidMap.Length; ++i)
            {
                if (ClientToServerFluidMap[i] == serverFluid)
                    return i;
            }
            return 0;
        }

        public static byte ClientFluidToServer(byte clientFluid)
        {
            if (clientFluid >= ClientToServerFluidMap.Length)
                return 0;

            return ClientToServerFluidMap[clientFluid];
        }
    }
}
