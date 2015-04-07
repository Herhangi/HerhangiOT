namespace HerhangiOT.GameServer.Model.Vocations
{
    public class VocationNone : Vocation
    {
        public VocationNone()
        {
            Id = 0;
            Name = "None";
            ManaMultiplier = 4.0F;
            SkillMultipliers = new []{ 1.5F, 2.0F, 2.0F, 2.0F, 2.0F, 1.5F, 1.1F };
        }
    }
}
