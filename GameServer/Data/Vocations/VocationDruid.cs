using HerhangiOT.GameServerLibrary.Model;

namespace HerhangiOT.GameServerLibrary.Model.Vocations
{
    public class VocationDruid : Vocation
    {
        public VocationDruid()
        {
            Id = 2;
            Name = "Druid";
            ManaMultiplier = 1.1F;
            SkillMultipliers = new[] { 1.5F, 1.8F, 1.8F, 1.8F, 1.8F, 1.5F, 1.1F };
        }
    }
}