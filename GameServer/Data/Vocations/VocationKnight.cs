using HerhangiOT.GameServer.Model;

namespace HerhangiOT.GameServer.Model.Vocations
{
    public class VocationKnight : Vocation
    {
        public VocationKnight()
        {
            Id = 4;
            ClientId = 1;
            Name = "Knight";
            ManaMultiplier = 3.0F;
            SkillMultipliers = new[] { 1.1F, 1.1F, 1.1F, 1.1F, 1.4F, 1.1F, 1.1F };
        }
    }
}