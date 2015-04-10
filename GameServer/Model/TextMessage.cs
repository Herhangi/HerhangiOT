using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class TextMessage
    {
        public MessageTypes Type { get; set; }
        public string Text { get; set; }
        public Position Position { get; set; }

        public uint PrimaryValue { get; set; }
        public TextColors PrimaryColor { get; set; }
        public uint SecondaryValue { get; set; }
        public TextColors SecondaryColor { get; set; }
    }
}
