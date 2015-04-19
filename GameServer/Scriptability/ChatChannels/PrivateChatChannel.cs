using System.Collections.Generic;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;

namespace HerhangiOT.GameServer.Scriptability.ChatChannels
{
    public class PrivateChatChannel : ChatChannel
    {
        public PrivateChatChannel(ushort channelId, string channelName) : base(channelId, channelName)
        {
            Invites = new Dictionary<uint, Player>();
        }

        public bool IsInvited(uint id)
        {
            if (id == Owner) return true;
            return Invites.ContainsKey(id);
        }

        public void InvitePlayer(Player invitor, Player invitedPlayer)
        {
            if(Invites.ContainsKey(invitedPlayer.CharacterId))
                return;

            Invites[invitedPlayer.CharacterId] = invitedPlayer;

            invitedPlayer.SendTextMessage(MessageTypes.InfoDescription, string.Format("{0} invites you to {1} private chat channel.", invitor.CharacterName, GenderStringifier.Stringify(invitor)));
            
            invitor.SendTextMessage(MessageTypes.InfoDescription, string.Format("{0} has been invited.", invitedPlayer.CharacterName));

            foreach (Player user in Users.Values)
            {
                user.SendChannelEvent(Id, invitedPlayer.CharacterName, ChatChannelEvents.Invite);
            }
        }
        public void ExcludePlayer(Player player, Player excludedPlayer)
        {
            if(!RemoveInvite(excludedPlayer.CharacterId))
                return;

            RemoveUser(excludedPlayer);

            player.SendTextMessage(MessageTypes.InfoDescription, string.Format("{0} has been excluded.", excludedPlayer.CharacterName));
            excludedPlayer.SendClosePrivate(Id);

            foreach (Player user in Users.Values)
            {
                user.SendChannelEvent(Id, excludedPlayer.CharacterName, ChatChannelEvents.Exclude);
            }
        }

        public bool RemoveInvite(uint id)
        {
            return Invites.Remove(id);
        }

        public void CloseChannel()
        {
	        foreach (Player user in Users.Values)
            {
                user.SendClosePrivate(Id);
	        }
        }
    }
}
