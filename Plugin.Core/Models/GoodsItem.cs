using Plugin.Core.Enums;

namespace Plugin.Core.Models
{
    public class GoodsItem
    {
        public int PriceGold, PriceCash, StarGold, StarCash, AuthType, BuyType2, BuyType3, Id, Title, Visibility;
        public ItemTag Tag;
        public ItemsModel Item = new ItemsModel() { Equip = ItemEquipType.Durable };
        public GoodsItem()
        {
        }
    }
}