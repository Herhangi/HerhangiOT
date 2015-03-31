namespace HerhangiOT.ServerLibrary.Database.Model
{
    public class GameWorldModel
    {
        public byte GameWorldId { get; set; }
        public string GameWorldName { get; set; }
        public string GameWorldIP { get; set; }
        public ushort GameWorldPort { get; set; }
        public string Secret { get; set; }
    }
}
