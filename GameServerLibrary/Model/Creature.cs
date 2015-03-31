namespace HerhangiOT.GameServerLibrary.Model
{
    public class Creature : Thing
    {
        public Tile Parent { get; protected set; }
        public Position Position { get; protected set; }

        public virtual bool CanSeeInvisibility()
        {
            return false;
        }

        public virtual bool IsInGhostMode()
        {
            return false;
        }
    }
}
