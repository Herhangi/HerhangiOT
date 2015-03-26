namespace HerhangiOT.GameServerLibrary.Model.Items
{
    public class BedItem : Item
    {
        protected House House;
        protected long SleepStart;
        protected uint SleeperGuid;

        public BedItem(ushort id) : base(id)
        {
            House = null;
            InternalRemoveSleeper();
        }

        protected void InternalRemoveSleeper()
        {
            SleepStart = 0;
            SleeperGuid = 0;
            SetSpecialDescription("Nobody is sleeping there.");
        }
    }
}
