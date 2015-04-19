using System.Runtime.CompilerServices;
using HerhangiOT.GameServer.Model;

namespace HerhangiOT.GameServer.Enums
{
    public enum Genders : byte
    {
        First = 0,
        Female = First,
        Male = 1,
        Last = Male
    }

    public static class GenderStringifier
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Stringify(Player player)
        {
            return (player.Gender == Genders.Female) ? "her" : "his";
        }
    }
}
