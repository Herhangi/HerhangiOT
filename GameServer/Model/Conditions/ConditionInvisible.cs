using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionInvisible : ConditionGeneric
    {
        public ConditionInvisible(ConditionIds id, ConditionFlags type, int ticks, bool buff = false, uint subId = 0)
            : base(id, type, ticks, buff, subId)
        {
        }

        public override sealed bool StartCondition(Creature creature)
        {
            if (!base.StartCondition(creature))
                return false;

            Game.InternalCreatureChangeVisible(creature, false);
            return true;
        }

        public override sealed void EndCondition(Creature creature)
        {
            if (!creature.IsInvisible())
                Game.InternalCreatureChangeVisible(creature, true);
        }

        public override sealed Condition Clone()
        {
            return (ConditionInvisible) MemberwiseClone();
        }
    }
}