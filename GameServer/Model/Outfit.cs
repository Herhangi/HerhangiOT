namespace HerhangiOT.GameServer.Model
{
    public class Outfit
    {
        public ushort LookType { get; set; }
        public ushort LookTypeEx { get; set; }
        public ushort LookMount { get; set; }
        public byte LookHead { get; set; }
        public byte LookBody { get; set; }
        public byte LookLegs { get; set; }
        public byte LookFeet { get; set; }
        public byte LookAddons { get; set; }

        public static readonly Outfit EmptyOutfit = new Outfit(); 
    }
}
