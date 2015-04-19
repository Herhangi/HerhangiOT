using System.Collections.Generic;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;

namespace HerhangiOT.GameServer.Scriptability
{
    public class ChatChannel
    {
        public ushort Id { get; protected set; }
        public uint Owner { get; set; }
        public string Name { get; protected set; }
        public bool IsPublicChannel { get; protected set; }
        public Dictionary<uint, Player> Users { get; protected set; }
        public Dictionary<uint, Player> Invites { get; protected set; } 

        public ChatChannel()
        {
            Users = new Dictionary<uint, Player>();
        }
        public ChatChannel(ushort channelId, string channelName)
        {
            Id = channelId;
            Name = channelName;
            IsPublicChannel = false;
            Users = new Dictionary<uint, Player>();
        }

        ~ChatChannel()
        {
            Users.Clear();
        }

        public bool AddUser(Player player)
        {
            if (Users.ContainsKey(player.Id))
                return false;

            if (!OnJoin(player))
                return false;

            //TODO: Guild Channel

            if (!IsPublicChannel)
            {
                foreach (Player user in Users.Values)
                {
                    user.SendChannelEvent(Id, player.CharacterName, ChatChannelEvents.Join);
                }
            }

            Users[player.Id] = player;
            return true;
        }
        public bool RemoveUser(Player player)
        {
            if (!Users.Remove(player.Id))
                return false;

            if (!IsPublicChannel)
            {
                foreach (Player user in Users.Values)
                {
                    user.SendChannelEvent(Id, player.CharacterName, ChatChannelEvents.Leave);
                }
            }

            OnLeave(player);
            return true;
        }

        public void SendToAll(string message, SpeakTypes type)
        {
            foreach (Player user in Users.Values)
            {
                user.SendChannelMessage(string.Empty, message, type, Id);
            }
        }
        public bool Talk(Player fromPlayer, SpeakTypes type, string message)
        {
            if (!Users.ContainsKey(fromPlayer.Id))
                return false;

            foreach (Player user in Users.Values)
            {
                user.SendToChannel(fromPlayer, type, message, Id);
            }
            return true;
        }

        public virtual void Setup() { }
        public virtual bool CanJoin(Player player) { return true; }
        public virtual bool OnJoin(Player player) { return true; }
        public virtual bool OnLeave(Player player) { return true; }
        public virtual bool OnSpeak(Player player, ref SpeakTypes type, string message) { return true; }
    }
}
