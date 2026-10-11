using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Managers.Events;
using Plugin.Core.Managers.Seasons;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;

//انسخ دول والمكرر شيله هناك

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ : GameClientPacket
    {
        private List<CartGoods> ShopCart = new List<CartGoods>();
        public PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ(GameClient client, byte[] data)
        {
            Makeme(client, data);
        }
        public override void Read()
        {
            int count = ReadH();
            for (int i = 0; i < count; i++)
            {
                CartGoods Cart = new CartGoods()
                {
                    GoodId = ReadD(),
                    BuyType = ReadC(),
                };
                ShopCart.Add(Cart);
                ReadQ();
            }
        }
        //        public override void Run()
        //        {
        //            try
        //            {
        //                Account Player = Client.Player;
        //                if (Player == null)
        //                {
        //                    return;
        //                }
        //                if (Player.Inventory.Items.Count >= 500)
        //                {
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487929));
        //                    return;
        //                }
        //                List<GoodsItem> Goods = ShopManager.GetGoods(ShopCart, out int Gold, out int Cash, out int Tags);
        //                if (Goods.Count == 0)
        //                {
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487767));
        //                }
        //                else if (0 > (Player.Gold - Gold) || 0 > (Player.Cash - Cash) || 0 > (Player.Tags - Tags))
        //                {
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487768));
        //                }
        //                else if (DaoManagerSQL.UpdateAccountValuable(Player.PlayerId, (Player.Gold - Gold), (Player.Cash - Cash), (Player.Tags - Tags)))
        //                {
        //                    Player.Gold -= Gold;
        //                    Player.Cash -= Cash;
        //                    Player.Tags -= Tags;
        //                    foreach (GoodsItem Good in Goods)
        //                    {
        //                        Client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, Player, Good.Item));
        //                    }
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(1, Goods, Player));
        //                }
        //                else
        //                {
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487769));
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                CLogger.Print($"PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ: {ex.Message}", LoggerType.Error, ex);
        //            }
        //        }
        //    }
        //}

        // ================= BuyBattlePass كما هي =================
        private void BuyBattlePass(Account player)
        {
            const int BattlePassPrice = 2500;

            if (player.IsPremiumBattlepass == 1 || player.BattlepassPremium == 1)
            {
                Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487770));
                return;
            }

            if (player.Cash < BattlePassPrice)
            {
                Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487768));
                return;
            }

            player.Cash -= BattlePassPrice;
            DaoManagerSQL.UpdateAccountCash(player.PlayerId, player.Cash);

            player.IsPremiumBattlepass = 1;
            //player.BattlepassPremium = 1;
            DaoManagerSQL.UpdateBattlepassPremium(player.PlayerId, 1);

            int remainDays = 0;
            var season = CardData.LoadSeason().FirstOrDefault();
            if (season != null)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                remainDays = (int)((season.EndDate - now) / 86400);
                if (remainDays < 0) remainDays = 0;
            }

            BattlePassSync.ClearGrantedCache();
            int level = BattlePassSync.GetCurrentLevel(player.SeasonExp);
            for (int lvl = 1; lvl <= level; lvl++)
            {
                BattlePassSync.GrantPremiumReward(player, lvl);
            }
            BattlePassSync.ClearGrantedCache();

            player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
                error: 0,
                passType: 1,
                price: BattlePassPrice,
                currency: 1,
                remainDays: remainDays
            ));
            player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(player));
        }

        private static readonly object BattlePassLock = new object(); // قفل الباتل باس



        //        public override void Run() // buy problem
        //        {
        //            try
        //            {
        //                Account Player = Client.Player;
        //                if (Player == null)
        //                    return;

        //                if (Player.Inventory.Items.Count >= 500)
        //                {
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487929));
        //                    return;
        //                }

        //                List<GoodsItem> Goods = ShopManager.GetGoods(ShopCart, out int Gold, out int Cash, out int Tags);

        //                if (Goods.Count == 0)
        //                {
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487767));
        //                    return;
        //                }
        //                else if (0 > (Player.Gold - Gold) || 0 > (Player.Cash - Cash) || 0 > (Player.Tags - Tags))
        //                {
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487768));
        //                    return;
        //                }
        //                else if (DaoManagerSQL.UpdateAccountValuable(
        //                             Player.PlayerId,
        //                             Player.Gold - Gold,
        //                             Player.Cash - Cash,
        //                             Player.Tags - Tags))
        //                {
        //                    Player.Gold -= Gold;
        //                    Player.Cash -= Cash;
        //                    Player.Tags -= Tags;

        //                    foreach (GoodsItem Good in Goods)
        //                    {
        //                        // ================= BATTLE PASS =================
        //                        if (ComDiv.GetIdStatics(Good.Item.Id, 1) == 36)
        //                        {
        //                            if (Player.IsPremiumBattlepass == 1 || Player.BattlepassPremium == 1)
        //                            {
        //                                Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487770));
        //                                return;
        //                            }

        //                            Player.IsPremiumBattlepass = 1;
        //                            Player.BattlepassPremium = 1;

        //                            DaoManagerSQL.UpdateBattlepassPremium(Player.PlayerId, 1);

        //                            int remainDays = 0;
        //                            var season = CardData.LoadSeason().FirstOrDefault();
        //                            if (season != null && season.EndDate > 0)
        //                            {
        //                                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //                                remainDays = (int)((season.EndDate - now) / 86400);
        //                                if (remainDays < 0) remainDays = 0;
        //                            }

        //                            int level = BattlePassSync.GetCurrentLevel(Player.SeasonExp);
        //                            for (int lvl = 1; lvl <= level; lvl++)
        //                            {
        //                                BattlePassSync.GrantPremiumReward(Player, lvl);
        //                            }

        //                            Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
        //                                error: 0,
        //                                passType: 1,
        //                                price: Cash,
        //                                currency: 1,
        //                                remainDays: remainDays
        //                            ));

        //                            Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Player));
        //                        }
        //                        // ================= NORMAL ITEM =================
        //                        else
        //                        {
        //                            Client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(0, Player, Good.Item));
        //                        }
        //                    }

        //                    // ✅ ACK النجاح النهائي (مهم جدًا)
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(1, Goods, Player));
        //                }
        //                else
        //                {
        //                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487769));
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                CLogger.Print(
        //                    $"PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ: {ex.Message}",
        //                    LoggerType.Error,
        //                    ex
        //                );
        //            }
        //        }

        //    }
        //}

        public override void Run()
        {
            try
            {
                Account Player = Client.Player;
                if (Player == null)
                    return;

                if (Player.Inventory.Items.Count >= 500)
                {
                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487929));
                    return;
                }

                List<GoodsItem> Goods = ShopManager.GetGoods(ShopCart, out int Gold, out int Cash, out int Tags);

                if (Goods.Count == 0)
                {
                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487767));
                    return;
                }

                if (0 > (Player.Gold - Gold) || 0 > (Player.Cash - Cash) || 0 > (Player.Tags - Tags))
                {
                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487768));
                    return;
                }

                if (!DaoManagerSQL.UpdateAccountValuable(Player.PlayerId, Player.Gold - Gold, Player.Cash - Cash, Player.Tags - Tags))
                {
                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487769));
                    return;
                }

                Player.Gold -= Gold;
                Player.Cash -= Cash;
                Player.Tags -= Tags;

                bool battlePassBought = false;

                foreach (GoodsItem Good in Goods)
                {
                    if (ComDiv.GetIdStatics(Good.Item.Id, 1) == 36)
                    {
                        // فقط شراء Battle Pass مرة واحدة
                        if (!battlePassBought)
                        {
                            BuyBattlePass(Player);
                            battlePassBought = true;
                        }
                    }
                    else
                    {
                        // إضافة أي Item عادي للانفنتوري بدون مشاكل
                        Client.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, Player, Good.Item));
                    }
                }

                // ACK النجاح النهائي لجميع العناصر
                Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(1, Goods, Player));
            }
            catch (Exception ex)
            {
                CLogger.Print($"PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ: {ex.Message}", LoggerType.Error, ex);
            }
        }

    }
}

//        public override void Run()
//        {
//            try
//            {
//                Account Player = Client.Player;
//                if (Player == null)
//                    return;

//                if (Player.Inventory.Items.Count >= 500)
//                {
//                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487929));
//                    return;
//                }

//                List<GoodsItem> Goods = ShopManager.GetGoods(ShopCart, out int Gold, out int Cash, out int Tags);

//                if (Goods.Count == 0)
//                {
//                    Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487767));
//                    return;
//                }

//                // ================= خصم Normal Items فقط =================
//                bool hasNormalItems = Goods.Any(g => ComDiv.GetIdStatics(g.Item.Id, 1) != 36);
//                if (hasNormalItems)
//                {
//                    if (0 > (Player.Gold - Gold) || 0 > (Player.Cash - Cash) || 0 > (Player.Tags - Tags))
//                    {
//                        Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487768));
//                        return;
//                    }

//                    if (!DaoManagerSQL.UpdateAccountValuable(
//                            Player.PlayerId,
//                            Player.Gold - Gold,
//                            Player.Cash - Cash,
//                            Player.Tags - Tags))
//                    {
//                        Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(2147487769));
//                        return;
//                    }

//                    Player.Gold -= Gold;
//                    Player.Cash -= Cash;
//                    Player.Tags -= Tags;
//                }

//                bool battlePassProcessed = false;

//                foreach (GoodsItem Good in Goods)
//                {
//                    // Battle Pass
//                    if (ComDiv.GetIdStatics(Good.Item.Id, 1) == 36)
//                    {
//                        if (battlePassProcessed)
//                            continue; // تجاهل أي تكرار

//                        BuyBattlePass(Player);
//                        battlePassProcessed = true;
//                    }
//                    else
//                    {
//                        Client.SendPacket(
//                            new PROTOCOL_INVENTORY_GET_INFO_ACK(0, Player, Good.Item)
//                        );
//                    }
//                }


//                // ================= ACK النجاح النهائي =================
//                Client.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(1, Goods, Player));
//            }
//            catch (Exception ex)
//            {
//                CLogger.Print(
//                    $"PROTOCOL_AUTH_SHOP_GOODS_BUY_REQ: {ex.Message}",
//                    LoggerType.Error,
//                    ex
//                );
//            }
//        }
//    }
//}