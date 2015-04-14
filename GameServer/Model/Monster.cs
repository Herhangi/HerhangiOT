using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class Monster : Creature
    {
        public static uint MonsterAutoID = 0x40000000;

        public int StepDuration { get; set; }

        public bool IsTargetNearby { get { return StepDuration >= 1; } }
        public bool IsFleeing { get { return Health <= 0; } } //TODO: MONSTER TYPE

        public sealed override CreatureTypes GetCreatureType()
        {
            return CreatureTypes.Monster;
        }

        public override string GetName()
        {
            throw new System.NotImplementedException();
        }

        public override void AddList()
        {
            throw new System.NotImplementedException();
        }

        public sealed override void SetID()
        {
            if (Id == 0)
                Id = MonsterAutoID++;
        }

        protected override bool UseCacheMap()
        {
            return true;
        }
    }
}
