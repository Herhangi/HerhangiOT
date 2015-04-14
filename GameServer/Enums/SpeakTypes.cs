namespace HerhangiOT.GameServer.Enums
{
    public enum SpeakTypes : byte
    {
	    Say = 1,
	    Whisper = 2,
	    Yell = 3,
	    PrivateFrom = 4,
	    PrivateTo = 5,
	    ChannelY = 7,
	    ChannelO = 8,
	    PrivateNp = 10,
	    PrivatePn = 12,
	    Broadcast = 13,
	    ChannelR1 = 14, //red - #c text
	    PrivateRedFrom = 15, //@name@text
	    PrivateRedTo = 16, //@name@text
	    MonsterSay = 36,
	    MonsterYell = 37,

	    ChannelR2 = 0xFF, //#d
    }
}
