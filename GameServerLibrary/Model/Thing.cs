namespace HerhangiOT.GameServerLibrary.Model
{
    public abstract class Thing
    {
        public abstract ushort GetThingId();

        public abstract string GetDescription(int lookDistance = 0);
    }
}
