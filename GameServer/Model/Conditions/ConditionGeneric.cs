using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionGeneric : Condition
    {
        public ConditionGeneric(ConditionIds id, ConditionFlags type, int ticks, bool buff = false, uint subId = 0)
            : base(id, type, ticks, buff, subId)
        {
        }

        public override void EndCondition(Creature creature)
        {
        }

        public override void AddCondition(Creature creature, Condition addCondition)
        {
            if (UpdateCondition(addCondition))
                SetTicks(addCondition.Ticks);
        }

        public override IconFlags GetIcons()
        {
            IconFlags icons = base.GetIcons();

            switch (ConditionType)
            {
                case ConditionFlags.ManaShield:
                    icons |= IconFlags.Manashield;
                    break;

                case ConditionFlags.InFight:
                    icons |= IconFlags.Swords;
                    break;

                case ConditionFlags.Drunk:
                    icons |= IconFlags.Drunk;
                    break;
            }

            return icons;
        }

        public override Condition Clone()
        {
            return (Condition) MemberwiseClone();
        }
    }
}