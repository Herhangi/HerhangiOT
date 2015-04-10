namespace HerhangiOT.GameServer.Enums
{
    public enum ReturnTypes : byte
    {
        NoError = 0,
        NotPossible = 1,
        NotEnoughRoom = 2,
        PlayerIsPZLocked = 3,
        PlayerIsNotInvited = 4,
        CannotThrow = 5,
        ThereIsNoWay = 6,
        DestinationOutOfReach = 7,
        CreatureBlock = 8,
        NotMoveable = 9,
        DropTwoHandedItem = 10,
        BothHandsNeedToBeFree = 11,
        CanOnlyUseOneWeapon = 12,
        NeedExchange = 13,
        CannotBeDressed = 14,
        PutThisObjectInYourHand = 15,
        PutThisObjectInBothHands = 16,
        TooFarAway = 17,
        FirstGoDownstairs = 18,
        FirstGoUpstairs = 19,
        ContainerNotEnoughRoom = 20,
        NotEnoughCapacity = 21,
        CannotPickup = 22,
        ThisIsImpossible = 23,
        DepotIsFull = 24,
        CreatureDoesNotExist = 25,
        CannotUseThisObject = 26,
        PlayerWithThisNameIsNotOnline = 27,
        NotRequiredLevelToUseRune = 28,
        YouAreAlreadyTrading = 29,
        ThisPlayerIsAlreadyTrading = 30,
        YouMayNotLogoutDuringAFight = 31,
        DirectPlayerShoot = 32,
        NotEnoughLevel = 33,
        NotEnoughMagicLevel = 34,
        NotEnoughMana = 35,
        NotEnoughSoul = 36,
        YouAreExhausted = 37,
        PlayerIsNotReachable = 38,
        CanOnlyUseThisRuneOnCreatures = 39,
        ActionNotPermittedInProtectionZone = 40,
        YouMayNotAttackThisPlayer = 41,
        YouMayNotAttackAPersonInProtectionZone = 42,
        YouMayNotAttackAPersonWhileInProtectionZone = 43,
        YouMayNotAttackThisCreature = 44,
        YouCanOnlyUseItOnCreatures = 45,
        CreatureIsNotReachable = 46,
        TurnSecureModeToAttackUnmarkedPlayers = 47,
        YouNeedPremiumAccount = 48,
        YouNeedToLearnThisSpell = 49,
        YourVocationCannotUseThisSpell = 50,
        YouNeedAWeaponToUseThisSpell = 51,
        PlayerIsPZLockedLeavePvpZone = 52,
        PlayerIsPZLockedEnterPvpZone = 53,
        ActionNotPermittedInANoPvpZone = 54,
        YouCannotLogoutHere = 55,
        YouNeedAMagicItemToCastSpell = 56,
        CannotConjureItemHere = 57,
        YouNeedToSplitYourSpears = 58,
        NameIsTooAmbigious = 59,
        CanOnlyUseOneShield = 60,
        NoPartyMembersInRange = 61,
        YouAreNotTheOwner = 62,
    }

    public static class ReturnTypeStringifier
    {
        public static string Stringify(ReturnTypes value)
        {
	        switch (value)
            {
		        case ReturnTypes.DestinationOutOfReach:
			        return "Destination is out of reach.";

		        case ReturnTypes.NotMoveable:
			        return "You cannot move this object.";

		        case ReturnTypes.DropTwoHandedItem:
			        return "Drop the double-handed object first.";

		        case ReturnTypes.BothHandsNeedToBeFree:
			        return "Both hands need to be free.";

		        case ReturnTypes.CannotBeDressed:
			        return "You cannot dress this object there.";

		        case ReturnTypes.PutThisObjectInYourHand:
			        return "Put this object in your hand.";

		        case ReturnTypes.PutThisObjectInBothHands:
			        return "Put this object in both hands.";

		        case ReturnTypes.CanOnlyUseOneWeapon:
			        return "You may only use one weapon.";

		        case ReturnTypes.TooFarAway:
			        return "Too far away.";

		        case ReturnTypes.FirstGoDownstairs:
			        return "First go downstairs.";

		        case ReturnTypes.FirstGoUpstairs:
			        return "First go upstairs.";

		        case ReturnTypes.NotEnoughCapacity:
			        return "This object is too heavy for you to carry.";

		        case ReturnTypes.ContainerNotEnoughRoom:
			        return "You cannot put more objects in this container.";

		        case ReturnTypes.NeedExchange:
		        case ReturnTypes.NotEnoughRoom:
			        return "There is not enough room.";

		        case ReturnTypes.CannotPickup:
			        return "You cannot take this object.";

		        case ReturnTypes.CannotThrow:
			        return "You cannot throw there.";

		        case ReturnTypes.ThereIsNoWay:
			        return "There is no way.";

		        case ReturnTypes.ThisIsImpossible:
			        return "This is impossible.";

		        case ReturnTypes.PlayerIsPZLocked:
			        return "You can not enter a protection zone after attacking another player.";

		        case ReturnTypes.PlayerIsNotInvited:
			        return "You are not invited.";

		        case ReturnTypes.CreatureDoesNotExist:
			        return "Creature does not exist.";

		        case ReturnTypes.DepotIsFull:
			        return "You cannot put more items in this depot.";

		        case ReturnTypes.CannotUseThisObject:
			        return "You cannot use this object.";

		        case ReturnTypes.PlayerWithThisNameIsNotOnline:
			        return "A player with this name is not online.";

		        case ReturnTypes.NotRequiredLevelToUseRune:
			        return "You do not have the required magic level to use this rune.";

		        case ReturnTypes.YouAreAlreadyTrading:
			        return "You are already trading.";

		        case ReturnTypes.ThisPlayerIsAlreadyTrading:
			        return "This player is already trading.";

		        case ReturnTypes.YouMayNotLogoutDuringAFight:
			        return "You may not logout during or immediately after a fight!";

		        case ReturnTypes.DirectPlayerShoot:
			        return "You are not allowed to shoot directly on players.";

		        case ReturnTypes.NotEnoughLevel:
			        return "You do not have enough level.";

		        case ReturnTypes.NotEnoughMagicLevel:
			        return "You do not have enough magic level.";

		        case ReturnTypes.NotEnoughMana:
			        return "You do not have enough mana.";

		        case ReturnTypes.NotEnoughSoul:
			        return "You do not have enough soul.";

		        case ReturnTypes.YouAreExhausted:
			        return "You are exhausted.";

		        case ReturnTypes.CanOnlyUseThisRuneOnCreatures:
			        return "You can only use this rune on creatures.";

		        case ReturnTypes.PlayerIsNotReachable:
			        return "Player is not reachable.";

		        case ReturnTypes.CreatureIsNotReachable:
			        return "Creature is not reachable.";

		        case ReturnTypes.ActionNotPermittedInProtectionZone:
			        return "This action is not permitted in a protection zone.";

		        case ReturnTypes.YouMayNotAttackThisPlayer:
			        return "You may not attack this player.";

		        case ReturnTypes.YouMayNotAttackThisCreature:
			        return "You may not attack this creature.";

		        case ReturnTypes.YouMayNotAttackAPersonInProtectionZone:
			        return "You may not attack a person in a protection zone.";

		        case ReturnTypes.YouMayNotAttackAPersonWhileInProtectionZone:
			        return "You may not attack a person while you are in a protection zone.";

		        case ReturnTypes.YouCanOnlyUseItOnCreatures:
			        return "You can only use it on creatures.";

		        case ReturnTypes.TurnSecureModeToAttackUnmarkedPlayers:
			        return "Turn secure mode off if you really want to attack unmarked players.";

		        case ReturnTypes.YouNeedPremiumAccount:
			        return "You need a premium account.";

		        case ReturnTypes.YouNeedToLearnThisSpell:
			        return "You need to learn this spell first.";

		        case ReturnTypes.YourVocationCannotUseThisSpell:
			        return "Your vocation cannot use this spell.";

		        case ReturnTypes.YouNeedAWeaponToUseThisSpell:
			        return "You need to equip a weapon to use this spell.";

		        case ReturnTypes.PlayerIsPZLockedLeavePvpZone:
			        return "You can not leave a pvp zone after attacking another player.";

		        case ReturnTypes.PlayerIsPZLockedEnterPvpZone:
			        return "You can not enter a pvp zone after attacking another player.";

		        case ReturnTypes.ActionNotPermittedInANoPvpZone:
			        return "This action is not permitted in a non pvp zone.";

		        case ReturnTypes.YouCannotLogoutHere:
			        return "You can not logout here.";

		        case ReturnTypes.YouNeedAMagicItemToCastSpell:
			        return "You need a magic item to cast this spell.";

		        case ReturnTypes.CannotConjureItemHere:
			        return "You cannot conjure items here.";

		        case ReturnTypes.YouNeedToSplitYourSpears:
			        return "You need to split your spears first.";

		        case ReturnTypes.NameIsTooAmbigious:
			        return "Name is too ambigious.";

		        case ReturnTypes.CanOnlyUseOneShield:
			        return "You may use only one shield.";

		        case ReturnTypes.NoPartyMembersInRange:
			        return "No party members in range.";

		        case ReturnTypes.YouAreNotTheOwner:
			        return "You are not the owner.";

		        default: // RETURNVALUE_NOTPOSSIBLE, etc
			        return "Sorry, not possible.";
	        }
        }
    }
}
