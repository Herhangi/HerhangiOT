using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class Creature : Thing
    {
        public const double SpeedA = 857.36;
        public const double SpeedB = 261.29;
        public const double SpeedC = -4795.01;

        public uint Id { get; protected set; }
        public Tile Parent { get; protected set; }
        public Position Position { get; protected set; }

        public ushort Health { get; protected set; }
        public ushort HealthMax { get; protected set; }
        public Directions Direction { get; protected set; }

        public Outfit CurrentOutfit { get; protected set; }
        public Outfit DefaultOutfit { get; protected set; }
        public LightInfo InternalLight { get; protected set; }

        public uint BaseSpeed { get; protected set; }
        public uint VarSpeed { get; protected set; }
        public uint Speed { get { return BaseSpeed + VarSpeed; } }
        public uint StepSpeed { get { return Speed; } }

        public Creature Master { get; protected set; }

        public bool IsHealthHidden { get; protected set; }
        public bool IsInternalRemoved { get; protected set; }

        public virtual bool CanSeeInvisibility()
        {
            return false;
        }

        public virtual bool IsInGhostMode()
        {
            return false;
        }

        public bool IsInvisible()
        {
            return false; //TODO: FILL METHOD
        }

        public virtual string GetName()
        {
            return string.Empty;
        }

        public virtual CreatureTypes GetCreatureType()
        {
            return CreatureTypes.Player;
        }
        
		public virtual SpeechBubbles GetSpeechBubble()
        {
			return SpeechBubbles.None;
		}
    }
}
