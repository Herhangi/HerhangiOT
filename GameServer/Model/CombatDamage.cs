using HerhangiOT.GameServer.Enums;

namespace HerhangiOT.GameServer.Model
{
    public class CombatDamage
    {
        public CombatTypeFlags PrimaryType { get; set; }
        public int PrimaryValue { get; set; }
        public CombatTypeFlags SecondaryType { get; set; }
        public int SecondaryValue { get; set; }
	    public CombatOrigins Origin { get; set; }
    }
}
