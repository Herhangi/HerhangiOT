namespace HerhangiOT.GameServer.Model
{
    public abstract class Thing
    {
        public virtual ushort GetThingId() { return 0; }

        public virtual string GetDescription(int lookDistance = 0) { return string.Empty; }

        public virtual void SetParent(Thing parent) { }
    }
}
