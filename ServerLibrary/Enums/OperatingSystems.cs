namespace HerhangiOT.ServerLibrary.Enums
{
    public enum OperatingSystems : ushort
    {
	    CLIENTOS_LINUX = 0x01,
	    CLIENTOS_WINDOWS = 0x02,
	    CLIENTOS_FLASH = 0x03,

        // WILL NOT BE SUPPORTED FOR SOME TIME
	    CLIENTOS_OTCLIENT_LINUX = 0x0A,
	    CLIENTOS_OTCLIENT_WINDOWS = 0x0B,
	    CLIENTOS_OTCLIENT_MAC = 0x0C
    }
}
