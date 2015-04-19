using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.GameServer.Scriptability;

namespace HerhangiOT.GameServer.Data.ChatChannels
{
    public class AdvertisingChannel : ChatChannel
    {
        public override void Setup()
        {
            Id = 5;
            Name = "Advertising";
            IsPublicChannel = true;
        }

        public override bool CanJoin(Player player)
        {
            return player.VocationData.Id != 0 || player.AccountType >= AccountTypes.SeniorTutor;
        }

        public override bool OnSpeak(Player player, ref SpeakTypes type, string message)
        {
            AccountTypes playerAccountType = player.AccountType;

            if (playerAccountType >= AccountTypes.GameMaster)
            {
                if(type == SpeakTypes.ChannelY)
                    type = SpeakTypes.ChannelO;
                return true;
            }

            //TODO: CONDITIONS
            //if player:getCondition(CONDITION_CHANNELMUTEDTICKS, CONDITIONID_DEFAULT, CHANNEL_ADVERTISING) then
            //    player:sendCancelMessage("You may only place one offer in two minutes.")
            //    return false
            //end
            //player:addCondition(muted)

            if (type == SpeakTypes.ChannelO)
            {
                if(playerAccountType < AccountTypes.GameMaster)
                    type = SpeakTypes.ChannelY;
            }
            else if (type == SpeakTypes.ChannelR1)
            {
                if (playerAccountType < AccountTypes.GameMaster && !player.HasFlag(PlayerFlags.CanTalkRedChannel))
                {
                    type = SpeakTypes.ChannelY;
                }
            }
            return true;
        }
    }
}
