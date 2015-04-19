using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.GameServer.Scriptability;

namespace HerhangiOT.GameServer.Data.ChatChannels
{
    public class WorldChat : ChatChannel
    {
        public override void Setup()
        {
            Id = 3;
            Name = "World Chat";
            IsPublicChannel = true;
        }

        public override bool OnSpeak(Player player, ref SpeakTypes type, string message)
        {
            AccountTypes playerAccountType = player.AccountType;

            if (player.CharacterData.Level == 1 && playerAccountType < AccountTypes.GameMaster)
            {
                player.SendCancelMessage("You may not speak into channels as long as you are on level 1.");
                return false;
            }

            if (type == SpeakTypes.ChannelY)
            {
                if(playerAccountType >= AccountTypes.GameMaster)
                    type = SpeakTypes.ChannelO;
            }
            else if (type == SpeakTypes.ChannelO)
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
