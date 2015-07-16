namespace HerhangiOT.ServerLibrary
{
    public static class Constants
    {
        public const string ServerName = "HerhangiOT";
        public const string ServerVersion = "0.1";
        public const string ServerDevelopers = "Turhan Yagiz 'Herhangi' Merpez";

        public const ushort ClientVersionMin = 1076;
        public const ushort ClientVersionMax = 1077;
        public const string ClientVersionStr = "10.77";

        public const ushort NetworkMessageSizeMax = 24590;
        public const ushort NetworkMessageErrorSizeMax = NetworkMessageSizeMax - 16;

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
