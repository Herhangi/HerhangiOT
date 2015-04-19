namespace HerhangiOT.ServerLibrary
{
    public static class Constants
    {
        public const string STATUS_SERVER_NAME = "HerhangiOT";
        public const string STATUS_SERVER_VERSION = "0.1";
        public const string STATUS_SERVER_DEVELOPERS = "Turhan Yagiz 'Herhangi' Merpez";

        public const ushort CLIENT_VERSION_MIN = 1076;
        public const ushort CLIENT_VERSION_MAX = 1076;
        public const string CLIENT_VERSION_STR = "10.76";

        public const ushort NETWORKMESSAGE_MAXSIZE = 24590;
        public const ushort NETWORKMESSAGE_ERRORMAXSIZE = NETWORKMESSAGE_MAXSIZE - 16;

        public const int OutputMessagePoolSize = 100;
        public const int OutputMessagePoolExpansionSize = 10;

        public const int NetworkMessagePoolSize = 100;
        public const int NetworkMessagePoolExpansionSize = 10;

        public const byte LightLevelDay = 250;
        public const byte LightLevelNight = 40;

        public const int JobCheckCreatureBucketCount = 10;
        public const int JobCheckCreatureCompletionInterval = 1000;
        public const int JobCheckCreatureInterval = JobCheckCreatureCompletionInterval / JobCheckCreatureBucketCount;

        public const int DispatcherTaskExpiration = 2000;

        public const ushort ChatChannelGuild = 0x00;
        public const ushort ChatChannelParty = 0x01;
        public const ushort ChatChannelPrivate = 0xFFFF;
    }
}
