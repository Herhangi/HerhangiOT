using HerhangiOT.GameServer.Scriptability;

namespace HerhangiOT.GameServer.Data.Vocations
{
    public class VocationDruid : Vocation
    {
        public VocationDruid()
        {
            Id = 2;
            ClientId = 4;
            Name = "Druid";
            ManaMultiplier = 1.1F;
            SkillMultipliers = new[] { 1.5F, 1.8F, 1.8F, 1.8F, 1.8F, 1.5F, 1.1F };
        }
    }
}