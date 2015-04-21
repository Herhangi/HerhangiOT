using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.ServerLibrary;

namespace HerhangiOT.GameServer
{
    public class Combat
    {
        public static CombatTypeFlags ConditionToDamageType(ConditionFlags condition)
        {
            switch (condition)
            {
                case ConditionFlags.Fire:
                    return CombatTypeFlags.FireDamage;

                case ConditionFlags.Energy:
                    return CombatTypeFlags.EnergyDamage;

                case ConditionFlags.Bleeding:
                    return CombatTypeFlags.PhysicalDamage;

                case ConditionFlags.Drown:
                    return CombatTypeFlags.DrownDamage;

                case ConditionFlags.Poison:
                    return CombatTypeFlags.EarthDamage;

                case ConditionFlags.Freezing:
                    return CombatTypeFlags.IceDamage;

                case ConditionFlags.Dazzled:
                    return CombatTypeFlags.HolyDamage;

                case ConditionFlags.Cursed:
                    return CombatTypeFlags.DeathDamage;
            }

            return CombatTypeFlags.None;
        }

        public static ReturnTypes CanDoCombat(Creature attacker, Creature target)
        {
	        if (attacker != null)
	        {
	            Player targetPlayer = target as Player;
		        if (targetPlayer != null)
                {
			        if (targetPlayer.HasFlag(PlayerFlags.CannotBeAttacked))
				        return ReturnTypes.YouMayNotAttackThisPlayer;

                    Player attackerPlayer = attacker as Player;
			        if (attackerPlayer != null)
                    {
				        if (attackerPlayer.HasFlag(PlayerFlags.CannotAttackPlayer))
					        return ReturnTypes.YouMayNotAttackThisPlayer;

				        if (IsProtected(attackerPlayer, targetPlayer))
					        return ReturnTypes.YouMayNotAttackThisPlayer;

				        //nopvp-zone
				        Tile targetPlayerTile = targetPlayer.Parent;
				        if (targetPlayerTile.Flags.HasFlag(TileFlags.NoPvpZone))
					        return ReturnTypes.ActionNotPermittedInANoPvpZone;
				        if (attackerPlayer.Parent.Flags.HasFlag(TileFlags.NoPvpZone) && !targetPlayerTile.Flags.HasFlag(TileFlags.NoPvpZone) && !targetPlayerTile.Flags.HasFlag(TileFlags.ProtectionZone))
					        return ReturnTypes.ActionNotPermittedInANoPvpZone;
			        }

			        if (attacker.Master != null) // IsSummon
			        {
			            Player masterAttackerPlayer = attacker.Master as Player;
				        if (masterAttackerPlayer != null)
                        {
					        if (masterAttackerPlayer.HasFlag(PlayerFlags.CannotAttackPlayer))
						        return ReturnTypes.YouMayNotAttackThisPlayer;

					        if (targetPlayer.Parent.Flags.HasFlag(TileFlags.NoPvpZone))
						        return ReturnTypes.ActionNotPermittedInANoPvpZone;

					        if (IsProtected(masterAttackerPlayer, targetPlayer))
						        return ReturnTypes.YouMayNotAttackThisPlayer;
				        }
			        }
		        }
                else if (target is Monster)
                {
                    Player attackerPlayer = attacker as Player;
			        if (attackerPlayer != null)
                    {
				        if (attackerPlayer.HasFlag(PlayerFlags.CannotAttackMonster))
					        return ReturnTypes.YouMayNotAttackThisCreature;

				        if (target.Master is Player && target.GetZone() == ZoneTypes.NoPvp)
					        return ReturnTypes.ActionNotPermittedInANoPvpZone;
			        }
                    else if (attacker is Monster)
                    {
				        Creature targetMaster = target.Master;

				        if (!(targetMaster is Player))
                        {
					        Creature attackerMaster = attacker.Master;

					        if (!(attackerMaster is Player))
						        return ReturnTypes.YouMayNotAttackThisCreature;
				        }
			        }
		        }

		        if (Game.WorldType == GameWorldTypes.NoPvp)
                {
			        if (attacker is Player || attacker.Master is Player)
                    {
				        if (target is Player)
                        {
					        if (!IsInPvpZone(attacker, target))
						        return ReturnTypes.YouMayNotAttackThisPlayer;
				        }

				        if (target.Master is Player)
                        {
					        if (!IsInPvpZone(attacker, target))
						        return ReturnTypes.YouMayNotAttackThisCreature;
				        }
			        }
		        }
	        }
            return ReturnTypes.NoError; //return g_events->eventCreatureOnTargetCombat(attacker, target); //TODO: Scripting
        }
        
        public static bool IsProtected(Player attacker, Player target)
        {
	        uint protectionLevel = (uint)ConfigManager.Instance[ConfigInt.ProtectionLevel];

	        if (target.CharacterData.Level < protectionLevel || attacker.CharacterData.Level < protectionLevel)
		        return true;

	        if (attacker.VocationData.Id == 0 || target.VocationData.Id == 0)
		        return true;

            //if (attacker.Skull == SkullTypes.Black && attacker.GetSk->getSkullClient(target) == SKULL_NONE) { //TODO: SKULL CLIENT
            //    return true;
            //}

	        return false;
        }
        
        public static bool IsInPvpZone(Creature attacker, Creature target)
        {
	        return attacker.GetZone() == ZoneTypes.Pvp && target.GetZone() == ZoneTypes.Pvp;
        }
    }
}
