using System;
using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class Npc : Creature
    {
        public static uint NpcAutoID = 0x80000000;

        public override CreatureTypes GetCreatureType()
        {
            throw new NotImplementedException();
        }

        public override string GetName()
        {
            throw new NotImplementedException();
        }

        public override void AddList()
        {
            throw new NotImplementedException();
        }

        public override void SetID()
        {
            throw new NotImplementedException();
        }
    }
}
