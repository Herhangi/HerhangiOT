using System.Collections.Generic;
using HerhangiOT.ServerLibrary.Model;

namespace HerhangiOT.ServerLibrary
{
    public class ItemManager
    {
        public List<Item> Items;

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
