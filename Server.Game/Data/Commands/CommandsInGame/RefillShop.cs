using Microsoft.Extensions.Logging;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Data.Commands.CommandsInGame
{
    public static class RefillShop
    {
        public static string SimpleRefill(Account player)
        {
            ShopManager.Reset();
            ShopManager.Load(1);
            CLogger.Print(Translation.GetLabel("RefillShopWarn", (object)player.Nickname), LoggerType.Info);
            return Translation.GetLabel("RefillShopMsg");
        }

        public static string InstantRefill(Account player)
        {
            ShopManager.Reset();
            ShopManager.Load(1);
            for (int index = 0; index < ShopManager.ShopDataItems.Count; ++index)
            {
                ShopData shopDataItem = ShopManager.ShopDataItems[index];
                player.SendPacket(new PROTOCOL_AUTH_SHOP_ITEMLIST_ACK(shopDataItem, ShopManager.TotalItems));
            }
            for (int index = 0; index < ShopManager.ShopDataGoods.Count; ++index)
            {
                ShopData shopDataGood = ShopManager.ShopDataGoods[index];
                player.SendPacket(new PROTOCOL_AUTH_SHOP_GOODSLIST_ACK(shopDataGood, ShopManager.TotalGoods));
            }
            for (int index = 0; index < ShopManager.ShopDataItemRepairs.Count; ++index)
            {
                ShopData shopDataItemRepair = ShopManager.ShopDataItemRepairs[index];
                player.SendPacket(new PROTOCOL_AUTH_SHOP_REPAIRLIST_ACK(shopDataItemRepair, ShopManager.TotalRepairs));
            }
            if (player.CafePC == 0)
            {
                for (int index = 0; index < ShopManager.ShopDataMt1.Count; ++index)
                {
                    ShopData data = ShopManager.ShopDataMt1[index];
                    player.SendPacket(new PROTOCOL_AUTH_SHOP_MATCHINGLIST_ACK(data, ShopManager.TotalMatching1));
                }
            }
            else
            {
                for (int index = 0; index < ShopManager.ShopDataMt2.Count; ++index)
                {
                    ShopData data = ShopManager.ShopDataMt2[index];
                    player.SendPacket(new PROTOCOL_AUTH_SHOP_MATCHINGLIST_ACK(data, ShopManager.TotalMatching2));
                }
            }
            player.SendPacket(new PROTOCOL_SHOP_GET_SAILLIST_ACK(true));
            return Translation.GetLabel("RefillShopMsg");
        }
    }
}
