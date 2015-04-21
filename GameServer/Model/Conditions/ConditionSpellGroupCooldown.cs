using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionSpellGroupCooldown : ConditionGeneric
    {
        public ConditionSpellGroupCooldown(ConditionIds id, ConditionFlags type, int ticks, bool buff = false, uint subId = 0) : base(id, type, ticks, buff, subId)
        {
        }

        public sealed override bool StartCondition(Creature creature)
        {
	        if (!base.StartCondition(creature))
		        return false;

	        if (SubId != 0 && Ticks > 0)
            {
		        Player player = creature as Player;
		        if (player != null)
                {
			        player.SendSpellGroupCooldown((SpellGroups)SubId, (uint)Ticks);
		        }
	        }
	        return true;
        }

        public sealed override void AddCondition(Creature creature, Condition addCondition)
        {
            if (UpdateCondition(addCondition))
            {
                SetTicks(addCondition.Ticks);

                if (SubId != 0 && Ticks > 0)
                {
                    Player player = creature as Player;
                    if (player != null)
                    {
                        player.SendSpellGroupCooldown((SpellGroups)SubId, (uint)Ticks);
                    }
                }
            }
        }

        public sealed override Condition Clone()
        {
            return (ConditionSpellGroupCooldown) MemberwiseClone();
        }
    }
}
