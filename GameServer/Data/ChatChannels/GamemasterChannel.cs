using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.GameServer.Scriptability;

namespace HerhangiOT.GameServer.Data.ChatChannels
{
    public class GamemasterChannel : ChatChannel
    {
        public override void Setup()
        {
            Id = 8;
            Name = "Gamemaster";
        }

        public override bool CanJoin(Player player)
        {
            return player.AccountType >= AccountTypes.GameMaster;
        }

        public override bool OnSpeak(Player player, ref SpeakTypes type, string message)
        {
            AccountTypes playerAccountType = player.AccountType;

            if (type == SpeakTypes.ChannelY)
            {
                if(playerAccountType >= AccountTypes.God)
                    type = SpeakTypes.ChannelO;
            }
            else if (type == SpeakTypes.ChannelO)
            {
                if(playerAccountType < AccountTypes.God)
                    type = SpeakTypes.ChannelY;
            }
            else if (type == SpeakTypes.ChannelR1)
            {
                if (playerAccountType < AccountTypes.God && !player.HasFlag(PlayerFlags.CanTalkRedChannel))
                {
                    type = SpeakTypes.ChannelY;
                }
            }
            return true;
        }
    }
}
