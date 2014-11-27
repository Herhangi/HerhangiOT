namespace HerhangiOT.ServerLibrary
{
    public static class Constants
    {
        public const string STATUS_SERVER_NAME = "HerhangiOT";
        public const string STATUS_SERVER_VERSION = "1.0";
        public const string STATUS_SERVER_DEVELOPERS = "Turhan Yagiz 'Herhangi' Merpez";

        public const ushort CLIENT_VERSION_MIN = 1041;
        public const ushort CLIENT_VERSION_MAX = 1041;
        public const string CLIENT_VERSION_STR = "10.41";

        public const ushort NETWORKMESSAGE_MAXSIZE = 24590;
        public const ushort NETWORKMESSAGE_ERRORMAXSIZE = NETWORKMESSAGE_MAXSIZE - 16;

        public const int OutputMessagePoolSize = 100;
        public const int OutputMessagePoolExpansionSize = 10;
    }
}
