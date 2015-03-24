using System.Collections.Generic;
using HerhangiOT.GameServerLibrary.Model;
using HerhangiOT.ServerLibrary;

namespace HerhangiOT.GameServerLibrary
{
    public class ItemManager
    {
        public static uint DwMajorVersion;
        public static uint DwMinorVersion;
        public static uint DwBuildNumber;

        private static Dictionary<ushort, ItemTemplate> _templates = new Dictionary<ushort, ItemTemplate>();
        public List<ItemTemplate> Items;

        public static bool Load()
        {
            Logger.LogOperationStart("Loading Items");

            if (!LoadFromOtb())
            {
                Logger.LogOperationFailed("Could not load items from OTB!");
                return false;
            }
            if (!LoadFromXml())
            {
                Logger.LogOperationFailed("Could not load items from XML!");
                return false;
            }
            Logger.LogOperationDone();
            return true;
        }

        private static bool LoadFromOtb()
        {

            return true;
        }

        private static bool LoadFromXml()
        {
            return true;
        }
    }
}
