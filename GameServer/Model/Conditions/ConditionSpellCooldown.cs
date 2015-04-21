using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model.Conditions
{
    public class ConditionSpellCooldown : ConditionGeneric
    {
        public ConditionSpellCooldown(ConditionIds id, ConditionFlags type, int ticks, bool buff = false, uint subId = 0)
            : base(id, type, ticks, buff, subId)
        {
        }

        public override bool StartCondition(Creature creature)
        {
            if (!base.StartCondition(creature))
                return false;

            if (SubId != 0 && Ticks > 0)
            {
                var player = creature as Player;
                if (player != null)
                {
                    player.SendSpellCooldown((byte) SubId, (uint) Ticks);
                }
            }
            return true;
        }

        public override void AddCondition(Creature creature, Condition addCondition)
        {
            if (UpdateCondition(addCondition))
            {
                SetTicks(addCondition.Ticks);

                if (SubId != 0 && Ticks > 0)
                {
                    var player = creature as Player;
                    if (player != null)
                    {
                        player.SendSpellCooldown((byte) SubId, (uint) Ticks);
                    }
                }
            }
        }

        public override Condition Clone()
        {
            return (ConditionSpellCooldown) MemberwiseClone();
        }
    }
}