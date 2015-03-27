namespace HerhangiOT.ServerLibrary.Model
{
    public class GameWorld
    {
        public byte GameWorldId { get; set; }
        public string GameWorldName { get; set; }
        public string GameWorldIP { get; set; }
        public ushort GameWorldPort { get; set; }
        public string Secret { get; set; }
    }
}
