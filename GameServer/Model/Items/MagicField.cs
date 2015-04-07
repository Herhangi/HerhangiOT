using System;

namespace HerhangiOT.GameServer.Model.Items
{
    public class MagicField : Item
    {
        private long _creationTime;

        public MagicField(ushort id) : base(id)
        {
            _creationTime = DateTime.Now.Ticks;
        }
    }
}
