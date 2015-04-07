namespace HerhangiOT.GameServer.Model.Items
{
    public class DepotLocker : Container
    {
        private ushort _depotId;

        public DepotLocker(ushort id) : base(id)
        {
            _depotId = 0;
            MaxSize = 3;
        }
    }
}
