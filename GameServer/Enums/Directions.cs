namespace HerhangiOT.GameServer.Enums
{
    public enum Directions : byte
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,

        DiagonalMask = 4,
        SouthWest = DiagonalMask | 0,
        SouthEast = DiagonalMask | 1,
        NorthWest = DiagonalMask | 2,
        NorthEast = DiagonalMask | 3,

        Last = NorthEast,
        SouthAlt = 8,
        EastAlt = 9,
        None = 10,
    }
}
