using System.Collections.Generic;
using System.Xml;
using HerhangiOT.GameServer.Enums;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model
{
    public class Group
    {
        public ushort Id { get; set; }
        public string Name { get; set; }
        public PlayerFlags Flags { get; set; }
        public uint MaxDepotItems { get; set; }
        public uint MaxVipEntries { get; set; }
        public bool Access { get; set; }
    }

    public static class Groups
    {
        private static Dictionary<ushort, Group> _groups { get; set; }

        public static Group GetGroup(ushort id)
        {
            Group group;
            _groups.TryGetValue(id, out group);
            return group;
        }

        public static bool Load()
        {
            Logger.LogOperationStart("Loading Groups");
            _groups = new Dictionary<ushort, Group>();

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("Data/XML/groups.xml");
            }
            catch
            {
                Logger.LogOperationFailed("LoadFromXml: Xml file could not be loaded!");
                return false;
            }

            foreach (XmlNode itemNode in doc.GetElementsByTagName("group"))
            {
                if (itemNode.Attributes == null) continue;

                Group group = new Group()
                {
                    Id = ushort.Parse(itemNode.Attributes["id"].InnerText),
                    Name = itemNode.Attributes["name"].InnerText,
                    Flags = (PlayerFlags)ulong.Parse(itemNode.Attributes["flags"].InnerText),
                    MaxDepotItems = uint.Parse(itemNode.Attributes["maxdepotitems"].InnerText),
                    MaxVipEntries = uint.Parse(itemNode.Attributes["maxvipentries"].InnerText),
                    Access = Tools.ConvertLuaBoolean(itemNode.Attributes["access"].InnerText),
                };
                _groups.Add(group.Id, group);
            }
            Logger.LogOperationDone();
            return true;
        }
    }
}
