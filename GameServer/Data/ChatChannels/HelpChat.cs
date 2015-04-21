using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.GameServer.Scriptability;

namespace HerhangiOT.GameServer.Data.ChatChannels
{
    public class HelpChat : ChatChannel
    {
        public override void Setup()
        {
            Id = 7;
            Name = "Help";
            IsPublicChannel = true;
        }

        public override bool OnSpeak(Player player, ref SpeakTypes type, string message)
        {
            AccountTypes playerAccountType = player.AccountType;

            if (player.CharacterData.Level == 1 && playerAccountType == AccountTypes.Normal)
            {
                player.SendCancelMessage("You may not speak into channels as long as you are on level 1.");
                return false;
            }

            if (player.GetCondition(ConditionFlags.ChannelMutedTicks, ConditionIds.Default, Id) != null)
            {
                player.SendCancelMessage("You are muted from the Help channel for using it inappropriately.");
                return false;
            }

            if (playerAccountType >= AccountTypes.Tutor)
            {
		        if (message.StartsWith("!mute "))
		        {
		            string targetName = message.Substring(6);
		            Player target = Game.GetPlayerByName(targetName);
			        
                    if (target != null) 
                    {
                        if (playerAccountType > target.AccountType)
                        {
					        if (target.GetCondition(ConditionFlags.ChannelMutedTicks, ConditionIds.Default, Id) != null)
                            {
                                target.AddCondition(Condition.CreateCondition(ConditionIds.Default, ConditionFlags.ChannelMutedTicks, 3600000, 0, false, Id));
                                Chat.SendChannelMessage(Id, SpeakTypes.ChannelR1, string.Format("{0} has been muted by {1} for using Help Channel inappropriately.", target.CharacterName, player.CharacterName));
                            }
					        else
                            {
						        player.SendCancelMessage("That player is already muted.");
                            }
                        }
				        else
                        {
                            player.SendCancelMessage("You are not authorized to mute that player.");
                        }
                    }
			        else
                    {
				        player.SendCancelMessage(ReturnTypes.PlayerWithThisNameIsNotOnline);
			        }
		            return false;
		        }
		        else if (message.StartsWith("!unmute "))
		        {
		            string targetName = message.Substring(8);
		            Player target = Game.GetPlayerByName(targetName);

			        if (target != null)
                    {
                        if (playerAccountType > target.AccountType)
                        {
                            Condition condition = target.GetCondition(ConditionFlags.ChannelMutedTicks, ConditionIds.Default, Id);
					        if (condition != null)
					        {
					            target.RemoveCondition(condition);
						        Chat.SendChannelMessage(Id, SpeakTypes.ChannelR1, string.Format("{0} has been unmuted by {1}.", target.CharacterName, player.CharacterName));
                            }
					        else
						        player.SendCancelMessage("That player is not muted.");
                        }
				        else
                            player.SendCancelMessage("You are not authorized to unmute that player.");
                    }
			        else
                    {
				        player.SendCancelMessage(ReturnTypes.PlayerWithThisNameIsNotOnline);   
                    }
		            return false;
		        }
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
                    if(playerAccountType >= AccountTypes.Tutor || player.HasFlag(PlayerFlags.TalkOrangeHelpChannel))
                        type = SpeakTypes.ChannelO;
                    else
                        type = SpeakTypes.ChannelY;
                }
            }
            return true;
        }
    }
}
