namespace HerhangiOT.GameServerLibrary.Model.Items
{
    public class Door : Item
    {
        private House _house;

        public Door(ushort id) : base(id)
        {
            _house = null;
        }
    }
}
