namespace HerhangiOT.GameServer.Enums
{
    public enum ConditionAttributions
    {
        Type = 1,
        ID,
        Ticks,
        HealthTicks,
        HealthGain,
        ManaTicks,
        ManaGain,
        Delayed,
        Owner,
        IntervalData,
        SpeedDelta,
        FormulaMinA,
        FormulaMinB,
        FormulaMaxA,
        FormulaMaxB,
        LightColor,
        LightLevel,
        LightTicks,
        LightInterval,
        SoulTicks,
        SoulGain,
        Skills,
        Stats,
        Outfit,
        PeriodDamage,
        IsBuff,
        SubID,

        //reserved for serialization
        End = 254,
    };
}