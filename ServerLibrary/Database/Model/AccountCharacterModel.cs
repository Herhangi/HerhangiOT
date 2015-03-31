namespace HerhangiOT.ServerLibrary.Database.Model
{
    public class AccountCharacterModel
    {
        public int CharacterId { get; set; } // I will not be using this in Json datastore
        public string CharacterName { get; set; }
        public int ServerId { get; set; }
    }
}
