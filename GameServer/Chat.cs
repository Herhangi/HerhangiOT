using System;
using System.Collections.Generic;
using System.Reflection;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.GameServer.Model;
using HerhangiOT.GameServer.Scriptability;
using HerhangiOT.GameServer.Scriptability.ChatChannels;
using HerhangiOT.ScriptLibrary;
using HerhangiOT.ServerLibrary;

namespace HerhangiOT.GameServer
{
    public static class Chat
    {
        private static readonly PrivateChatChannel DummyPrivate = new PrivateChatChannel(Constants.ChatChannelPrivate, "Private Chat Channel");

        public static Dictionary<ushort, ChatChannel> NormalChannels { get; private set; }
        public static Dictionary<ushort, PrivateChatChannel> PrivateChannels { get; private set; }
        public static Dictionary<ushort, ChatChannel> PartyChannels { get; private set; }
        public static Dictionary<uint, ChatChannel> GuildChannels { get; private set; }

        public static bool Load(bool forceCompilation = false)
        {
            Logger.LogOperationStart("Loading chat channels");

            NormalChannels = new Dictionary<ushort, ChatChannel>();
            PrivateChannels = new Dictionary<ushort, PrivateChatChannel>();
            PartyChannels = new Dictionary<ushort, ChatChannel>();
            GuildChannels = new Dictionary<uint, ChatChannel>();

            Assembly chatAssembly;
            List<string> externalAssemblies = new List<string>();
            externalAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            bool redFromCache;

            if (!ScriptManager.CompileCsScripts("Data/ChatChannels", "ChatChannels.*.dll", externalAssemblies, out chatAssembly, out redFromCache, forceCompilation))
                return false;

            try
            {
                Dictionary<ushort, ChatChannel> channels = new Dictionary<ushort, ChatChannel>();
                foreach (Type chatChannel in chatAssembly.GetTypes())
                {
                    if (chatChannel.BaseType == typeof(ChatChannel))
                    {
                        ChatChannel channel = (ChatChannel)Activator.CreateInstance(chatChannel);
                        channel.Setup();
                        channels.Add(channel.Id, channel);
                    }
                }
                NormalChannels.Clear();
                NormalChannels = channels;
            }
            catch (Exception e)
            {
                if (!forceCompilation)
                {
                    return Load(true);
                }

                Logger.LogOperationFailed(e.ToString());
                return false;
            }

            if (redFromCache)
                Logger.LogOperationCached();
            else
                Logger.LogOperationDone();
            return true;
        }

        public static ChatChannel CreateChannel(Player player, ushort channelId)
        {
            if (GetChannel(player, channelId) != null)
                return null;

            switch (channelId)
            {
                case Constants.ChatChannelGuild:
                    //Guild guild = player.getGuild(); //TODO: GUILD
                    //if (guild)
                    //{
                    //    ChatChannel* newChannel = new ChatChannel(channelId, guild->getName());
                    //    guildChannels[guild->getId()] = newChannel;
                    //    return newChannel;
                    //}
                    break;

                case Constants.ChatChannelParty:
                    //Party* party = player.getParty(); //TODO: PARTY
                    //if (party)
                    //{
                    //    ChatChannel* newChannel = new ChatChannel(channelId, "Party");
                    //    partyChannels[party] = newChannel;
                    //    return newChannel;
                    //}
                    break;

                case Constants.ChatChannelPrivate:
                    //only 1 private channel for each premium player
                    if (!player.IsPremium() || GetPrivateChannel(player) != null)
                        return null;

                    //find a free private channel slot
                    for (ushort i = 100; i < 10000; ++i)
                    {
                        if (!PrivateChannels.ContainsKey(i))
                        {
                            PrivateChatChannel newChannel = new PrivateChatChannel(i, string.Format("{0}'s Channel", player.CharacterName)) { Owner = player.CharacterId };
                            PrivateChannels[i] = newChannel;
                            return newChannel;
                        }
                    }
                    break;
            }
            return null;
        }

        public static bool DeleteChannel(Player player, ushort channelId)
        {
            switch (channelId)
            {
                case Constants.ChatChannelGuild:
                    //Guild* guild = player.getGuild(); //TODO: GUILD
                    //if (!guild) {
                    //    return false;
                    //}

                    //auto it = guildChannels.find(guild->getId());
                    //if (it == guildChannels.end()) {
                    //    return false;
                    //}

                    //delete it->second;
                    //guildChannels.erase(it);
                    break;

                case Constants.ChatChannelParty:
                    //Party* party = player.getParty(); //TODO: PARTY
                    //if (!party) {
                    //    return false;
                    //}

                    //auto it = partyChannels.find(party);
                    //if (it == partyChannels.end()) {
                    //    return false;
                    //}

                    //delete it->second;
                    //partyChannels.erase(it);
                    break;

                default:
                    PrivateChatChannel channel;
                    if (!PrivateChannels.TryGetValue(channelId, out channel))
                        return false;

                    channel.CloseChannel();
                    return PrivateChannels.Remove(channelId);
            }
            return true;
        }

        public static ChatChannel AddUserToChannel(Player player, ushort channelId)
        {
            ChatChannel channel = GetChannel(player, channelId);
            if (channel != null && channel.AddUser(player))
            {
                return channel;
            }
            return null;
        }
        public static bool RemoveUserFromChannel(Player player, ushort channelId)
        {
            ChatChannel channel = GetChannel(player, channelId);
            if (channel == null || !channel.RemoveUser(player))
                return false;

            if (channel.Owner == player.CharacterId)
                DeleteChannel(player, channelId);

            return true;
        }
        public static void RemoveUserFromAllChannels(Player player)
        {
            foreach (ChatChannel channel in NormalChannels.Values)
            {
                channel.RemoveUser(player);
            }

            //for (const auto& it : partyChannels) { //TODO: PARTY
            //    it.second->removeUser(player);
            //}

            //for (const auto& it : guildChannels) { //TODO: GUILD
            //    it.second->removeUser(player);
            //}

            foreach (PrivateChatChannel pChannel in PrivateChannels.Values) //TODO: I THINK THIS MUST BE IMPROVED WITH PLAYER PARTY CACHING
            {
                pChannel.RemoveInvite(player.CharacterId);
                pChannel.RemoveUser(player);

                if (pChannel.Owner == player.CharacterId)
                {
                    DeleteChannel(player, pChannel.Id);
                }
            }
        }

        public static bool TalkToChannel(Player player, SpeakTypes type, string text, ushort channelId)
        {
            ChatChannel channel = GetChannel(player, channelId);
            if (channel == null)
                return false;

            if (channelId == Constants.ChatChannelGuild)
            {
                //if (player.getGuildLevel() > 1) { //TODO: GUILD
                //    type = TALKTYPE_CHANNEL_O;
                //} else if (type != TALKTYPE_CHANNEL_Y) {
                //    type = TALKTYPE_CHANNEL_Y;
                //}
            }
            else if (type != SpeakTypes.ChannelY && (channelId == Constants.ChatChannelPrivate || channelId == Constants.ChatChannelParty))
            {
                type = SpeakTypes.ChannelY;
            }

            if (!channel.OnSpeak(player, ref type, text))
            {
                return false;
            }

            return channel.Talk(player, type, text);
        }

        public static List<ChatChannel> GetChannelList(Player player)
        {
            List<ChatChannel> list = new List<ChatChannel>();

            //if (player.getGuild()) //TODO: GUILD
            //{
            //    ChatChannel* channel = getChannel(player, CHANNEL_GUILD);
            //    if (channel) {
            //        list.push_back(channel);
            //    } else {
            //        channel = createChannel(player, CHANNEL_GUILD);
            //        if (channel) {
            //            list.push_back(channel);
            //        }
            //    }
            //}

            //if (player.getParty()) { //TODO: PARTY
            //    ChatChannel* channel = getChannel(player, CHANNEL_PARTY);
            //    if (channel) {
            //        list.push_back(channel);
            //    } else {
            //        channel = createChannel(player, CHANNEL_PARTY);
            //        if (channel) {
            //            list.push_back(channel);
            //        }
            //    }
            //}

            foreach (ChatChannel nChannel in NormalChannels.Values)
            {
                if (nChannel.CanJoin(player))
                    list.Add(nChannel);
            }

            bool hasPrivate = false;
            foreach (PrivateChatChannel pChannel in PrivateChannels.Values) //TODO: THIS CAN BE IMPROVED WITH CHAT CHANNEL CACHING
            {
                uint guid = player.CharacterId;
                if (pChannel.IsInvited(guid))
                    list.Add(pChannel);

                if (pChannel.Owner == guid)
                    hasPrivate = true;
            }

            if (!hasPrivate && player.IsPremium())
            {
                list.Add(DummyPrivate);
            }
            return list;
        }

        public static ChatChannel GetChannel(Player player, ushort channelId)
        {
            switch (channelId)
            {
                case Constants.ChatChannelGuild:
                    //Guild* guild = player.getGuild(); //TODO: GUILD
                    //if (guild) {
                    //    auto it = guildChannels.find(guild->getId());
                    //    if (it != guildChannels.end()) {
                    //        return it->second;
                    //    }
                    //}
                    break;

                case Constants.ChatChannelParty:
                    //Party* party = player.getParty(); //TODO: PARTY
                    //if (party) {
                    //    auto it = partyChannels.find(party);
                    //    if (it != partyChannels.end()) {
                    //        return it->second;
                    //    }
                    //}
                    break;

                default:
                    ChatChannel channel;
                    if (NormalChannels.TryGetValue(channelId, out channel))
                    {
                        if (!channel.CanJoin(player))
                            return null;
                        return channel;
                    }

                    PrivateChatChannel pChannel;
                    if (PrivateChannels.TryGetValue(channelId, out pChannel))
                    {
                        if (pChannel.IsInvited(player.CharacterId))
                            return pChannel;
                    }
                    break;
            }
            return null;
        }
        public static ChatChannel GetChannelById(ushort channelId)
        {
            ChatChannel channel;
            NormalChannels.TryGetValue(channelId, out channel);
            return channel;
        }
        public static ChatChannel GetGuildChannelById(uint guildId)
        {
            ChatChannel channel;
            GuildChannels.TryGetValue(guildId, out channel);
            return channel;
        }
        public static PrivateChatChannel GetPrivateChannel(Player player)
        {
            foreach (PrivateChatChannel pChannel in PrivateChannels.Values) //TODO: THIS CAN BE IMPROVED WITH CHAT CHANNEL CACHING
            {
                if (pChannel.Owner == player.CharacterId)
                    return pChannel;
            }
            return null;
        }

        public static bool SendChannelMessage(ushort channelId, SpeakTypes type, string message)
        {
            ChatChannel channel = GetChannelById(channelId);
            if (channel == null)
                return false;

            channel.SendToAll(message, type);
            return true;
        }
    }
}
