using System;
using System.Collections.Generic;
using System.Xml;
using HerhangiOT.GameServerLibrary.Model;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServerLibrary
{
    public class OutfitManager
    {
        private static List<Outfit>[] Outfits { get; set; }

        public static bool Load()
        {
            Logger.LogOperationStart("Loading Outfits");

            Outfits = new List<Outfit>[(int)Genders.Last+1];
            for(Genders i = Genders.First; i <= Genders.Last; i++)
                Outfits[(int)i] = new List<Outfit>();

            if (LoadFromXml())
            {
                Logger.LogOperationDone();
                return true;
            }
            return false;
        }

        private static bool LoadFromXml()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("Data/XML/outfits.xml");

                foreach (XmlNode outfitNode in doc.GetElementsByTagName("outfit"))
                {
                    if(outfitNode.Attributes == null) continue;

                    XmlAttribute enabledAttribute = outfitNode.Attributes["enabled"];
                    if(enabledAttribute == null || !Tools.ConvertLuaBoolean(enabledAttribute.InnerText)) continue;

                    XmlAttribute typeAttribute = outfitNode.Attributes["type"];
                    if (typeAttribute == null)
                    {
                        Logger.Log(LogLevels.Warning, "OutfitManager: Missing outfit type!");
                        continue;
                    }

                    ushort type = ushort.Parse(typeAttribute.InnerText);
                    if (type > (ushort) Genders.Last)
                    {
                        Logger.Log(LogLevels.Warning, "OutfitManager: Invalid outfit type: " + type);
                        continue;
                    }

                    XmlAttribute lookTypeAttribute = outfitNode.Attributes["looktype"];
                    if (lookTypeAttribute == null)
                    {
                        Logger.Log(LogLevels.Warning, "OutfitManager: Missing looktype on outfit!");
                        continue;
                    }

                    Outfits[type].Add(new Outfit
                    {
                        Name = outfitNode.Attributes["name"].InnerText,
                        LookType = ushort.Parse(lookTypeAttribute.InnerText),
                        IsPremiumOnly = Tools.ConvertLuaBoolean(outfitNode.Attributes["premium"].InnerText),
                        IsUnlockedAtBeginning = Tools.ConvertLuaBoolean(outfitNode.Attributes["unlocked"].InnerText)
                    });
                }
            }
            catch (Exception)
            {
                Logger.LogOperationFailed("OutfitManager: XML could not be loaded!");
                return false;
            }

            return true;
        }
    }
}
