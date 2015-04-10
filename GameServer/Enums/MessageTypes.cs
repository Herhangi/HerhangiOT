namespace HerhangiOT.GameServer.Enums
{
    public enum MessageTypes : byte
    {
        StatusConsoleBlue = 4, /*FIXME Blue message in the console*/

        StatusConsoleRed = 13, /*Red message in the console*/

        StatusDefault = 17, /*White message at the bottom of the game window and in the console*/
        StatusWarning = 18, /*Red message in game window and in the console*/
        EventAdvance = 19, /*White message in game window and in the console*/

        StatusSmall = 21, /*White message at the bottom of the game window"*/
        InfoDescription = 22, /*Green message in game window and in the console*/
        DamageDealt = 23,
        DamageReceived = 24,
        Healed = 25,
        Experience = 26,
        DamageOthers = 27,
        HealedOthers = 28,
        ExperienceOthers = 29,
        EventDefault = 30, /*White message at the bottom of the game window and in the console*/

        EventOrange = 36, /*Orange message in the console*/
        StatusConsoleOrange = 37,  /*Orange message in the console*/
    }
}
