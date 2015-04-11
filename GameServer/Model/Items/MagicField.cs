using HerhangiOT.ServerLibrary.Utility;

namespace HerhangiOT.GameServer.Model.Items
{
    public class MagicField : Item
    {
        private long _creationTime;

        public MagicField(ushort id) : base(id)
        {
            _creationTime = Tools.GetSystemMilliseconds();
        }
    }
}
