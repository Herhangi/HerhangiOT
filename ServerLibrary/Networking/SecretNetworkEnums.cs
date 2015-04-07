namespace HerhangiOT.ServerLibrary.Networking
{
    public enum SecretNetworkPacketType : byte
    {
        Authentication = 0,

        CheckUserAuthenticity = 10,

    }

    public enum SecretNetworkResponseType : byte
    {
        Success = 0,

        InvalidGameServerId = 10,
        InvalidGameServerSecret = 11,

        InvalidAccountData = 20,
        AnotherCharacterOnline = 21,
        CharacterCouldNotBeFound = 22,
        SessionCouldNotBeFound = 23,
    }
}
