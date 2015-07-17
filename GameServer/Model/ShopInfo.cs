namespace HerhangiOT.GameServer.Model
{
    public class ShopInfo
    {
	    public ushort ItemId { get; set; }
	    public int SubType { get; set; }
	    public uint BuyPrice { get; set; }
	    public uint SellPrice { get; set; }
	    public string RealName { get; set; }

	    public ShopInfo()
        {
		    ItemId = 0;
		    SubType = 1;
		    BuyPrice = 0;
		    SellPrice = 0;
	    }

        public ShopInfo(ushort itemId, int subType = 0, uint buyPrice = 0, uint sellPrice = 0, string realName = "")
        {
            ItemId = itemId;
            SubType = subType;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            RealName = realName;
        }
    }
}
