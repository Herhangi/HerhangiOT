using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class Monster : Creature
    {
        public sealed override CreatureTypes GetCreatureType()
        {
            return CreatureTypes.Monster;
        }
    }
}
