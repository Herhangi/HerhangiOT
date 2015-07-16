using System;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;

namespace HerhangiOT.GameServer.Utility
{
    public static class GameTools
    {
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
    }
}
