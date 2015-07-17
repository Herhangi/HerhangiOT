namespace HerhangiOT.GameServer.Enums
{
    public enum FluidTypes : byte
    {
        None = FluidColors.Empty,
        Water = FluidColors.Blue,
        Blood = FluidColors.Red,
        Beer = FluidColors.Brown,
        Slime = FluidColors.Green,
        Lemonade = FluidColors.Yellow,
        Milk = FluidColors.White,
        Mana = FluidColors.Purple,

        Life = FluidColors.Red + 8,
        Oil = FluidColors.Brown + 8,
        Urine = FluidColors.Yellow + 8,
        CoconutMilk = FluidColors.White + 8,
        Wine = FluidColors.Purple + 8,

        Mud = FluidColors.Brown + 16,
        FruitJuice = FluidColors.Yellow + 16,

        Lava = FluidColors.Red + 24,
        Rum = FluidColors.Brown + 24,
        Swamp = FluidColors.Green + 24,

        Tea = FluidColors.Brown + 32,
        Mead = FluidColors.Brown + 40,
    }
}
