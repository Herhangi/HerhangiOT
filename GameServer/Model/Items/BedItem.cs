using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary.Threading;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Items
{
    public class BedItem : Item
    {
        public House House { get; set; }
        public long SleepStart { get; protected set; }
        public uint SleeperGuid { get; protected set; }

        public BedItem(ushort id)
            : base(id)
        {
            House = null;
            InternalRemoveSleeper();
        }

        public sealed override bool CanRemove()
        {
            return House == null;
        }

        public bool CanUse(Player player)
        {
            if (player == null || House == null || !player.IsPremium())
                return false;

            if (SleeperGuid == 0)
                return true;

            //if (House.GetHouseAccessLevel(player) == HOUSE_OWNER) { //TODO: HOUSE SYSTEM
            //    return true;
            //}

            //Player sleeper;
            //if (!IOLoginData::loadPlayerById(&sleeper, sleeperGUID)) {
            //    return false;
            //}

            //if (house->getHouseAccessLevel(&sleeper) > house->getHouseAccessLevel(player)) {
            //    return false;
            //}
            return true;
        }

        public bool TrySleep(Player player)
        {
            if (House == null || player.IsRemoved())
                return false;

            if (SleeperGuid != 0)
            {
                if (ItemManager.Templates[Id].TransformToFree != 0)// && House->getOwner() == player->getGUID()) TODO: House
                {
                    WakeUp(null);
                }

                Game.AddMagicEffect(player.GetPosition(), MagicEffects.Poff);
                return false;
            }
            return true;
        }

        public bool Sleep(Player player)
        {
            if (House == null)
                return false;

            if (SleeperGuid != 0)
                return false;

            BedItem nextBedItem = GetNextBedItem();

            InternalSetSleeper(player);

            if (nextBedItem != null)
                nextBedItem.InternalSetSleeper(player);

            // update the bedSleepersMap
            Game.SetBedSleeper(this, player.CharacterId);

            // make the player walk onto the bed
            Map.MoveCreature(player, Parent as Tile);

            // display 'Zzzz'/sleep effect
            Game.AddMagicEffect(player.GetPosition(), MagicEffects.Sleep);

            // kick player after he sees himself walk onto the bed and it change id
            uint playerId = player.Id;
            DispatcherManager.Scheduler.AddEvent(SchedulerTask.CreateSchedulerTask(SchedulerTask.SchedulerMinTicks, () => Game.KickPlayer(playerId, false)));

            // change self and partner's appearance
            UpdateAppearance(player);

            if (nextBedItem != null)
            {
                nextBedItem.UpdateAppearance(player);
            }

            return true;
        }

        void WakeUp(Player player)
        {
            if (House == null)
                return;

            if (SleeperGuid != 0)
            {
                if (player == null)
                {
                    //Player _player; //TODO: Player data save
                    //if (IOLoginData::loadPlayerById(&_player, sleeperGUID)) {
                    //    regeneratePlayer(&_player);
                    //    IOLoginData::savePlayer(&_player);
                    //}
                }
                else
                {
                    RegeneratePlayer(player);
                    Game.AddCreatureHealth(player);
                }
            }

            // update the bedSleepersMap
            Game.RemoveBedSleeper(SleeperGuid);

            BedItem nextBedItem = GetNextBedItem();

            // unset sleep info
            InternalRemoveSleeper();

            if (nextBedItem != null)
                nextBedItem.InternalRemoveSleeper();

            // change self and partner's appearance
            UpdateAppearance(null);

            if (nextBedItem != null)
                nextBedItem.UpdateAppearance(null);
        }

        public BedItem GetNextBedItem()
        {
            Directions dir = ItemManager.Templates[Id].BedPartnerDirection;
            Position targetPos = Position.GetNextPosition(dir, GetPosition());

            Tile tile = Map.GetTile(targetPos.X, targetPos.Y, targetPos.Z);
            if (tile == null)
            {
                return null;
            }
            return tile.GetBedItem();
        }

        protected void UpdateAppearance(Player player)
        {
            ItemTemplate it = ItemManager.Templates[Id];
            if (it.Type == ItemTypes.Bed)
            {
                if (player != null && it.TransformToOnUse[player.CharacterData.Gender] != 0)
                {
                    ItemTemplate newType = ItemManager.Templates[it.TransformToOnUse[player.CharacterData.Gender]];
                    if (newType.Type == ItemTypes.Bed)
                    {
                        Game.TransformItem(this, it.TransformToOnUse[player.CharacterData.Gender]);
                    }
                }
                else if (it.TransformToFree != 0)
                {
                    ItemTemplate newType = ItemManager.Templates[it.TransformToFree];
                    if (newType.Type == ItemTypes.Bed)
                    {
                        Game.TransformItem(this, it.TransformToFree);
                    }
                }
            }
        }

        protected void RegeneratePlayer(Player player)
        {
            long sleptTime = Tools.GetSystemMilliseconds() - SleepStart;

            //Condition* condition = player->getCondition(CONDITION_REGENERATION, CONDITIONID_DEFAULT); TODO: Conditions
            //if (condition)
            //{
            //    uint32_t regen;
            //    if (condition->getTicks() != -1) {
            //        regen = std::min<int32_t>((condition->getTicks() / 1000), sleptTime) / 30;
            //        const int32_t newRegenTicks = condition->getTicks() - (regen * 30000);
            //        if (newRegenTicks <= 0) {
            //            player->removeCondition(condition);
            //        } else {
            //            condition->setTicks(newRegenTicks);
            //        }
            //    } else {
            //        regen = sleptTime / 30;
            //    }

            //    player->changeHealth(regen, false);
            //    player->changeMana(regen);
            //}

            int soulRegen = (int)(sleptTime / (60 * 15));
            player.ChangeSoul(soulRegen);
        }

        protected void InternalSetSleeper(Player player)
        {
            string description = player.GetName() + " is sleeping there.";

            SleeperGuid = player.CharacterId;
            SleepStart = Tools.GetSystemMilliseconds();
            SetSpecialDescription(description);
        }
        protected void InternalRemoveSleeper()
        {
            SleepStart = 0;
            SleeperGuid = 0;
            SetSpecialDescription("Nobody is sleeping there.");
        }
    }
}
