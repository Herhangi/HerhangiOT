using System;

namespace HerhangiOT.GameServer.Enums
{
    [Flags]
    public enum CylinderFlags : byte
    {
        None = 0,
        Nolimit = 1 << 0,		    //Bypass limits like capacity/container limits, blocking items/creatures etc.
        IgnoreBlockItem = 1 << 1,	//Bypass movable blocking item checks
        IgnoreBlockCreature = 1 << 2,//Bypass creature checks
        ChildIsOwner = 1 << 3,		//Used by containers to query capacity of the carrier (player)
        Pathfinding = 1 << 4,		//An additional check is done for floor changing/teleport items
        IgnoreFieldDamage = 1 << 5,	//Bypass field damage checks
        IgnoreNotMoveable = 1 << 6,	//Bypass check for mobility
        IgnoreAutostack = 1 << 7    //queryDestination will not try to stack items together
    }
}
