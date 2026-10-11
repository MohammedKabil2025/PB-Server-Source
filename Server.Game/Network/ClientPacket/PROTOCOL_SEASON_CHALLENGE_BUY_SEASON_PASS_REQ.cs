using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Managers.Events;
using Plugin.Core.Managers.Seasons;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_REQ : GameClientPacket
    {
        //private uint Error;
        public PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_REQ(GameClient Client, byte[] Buffer)
        {
            Makeme(Client, Buffer);
        }
        public override void Read()
        {
            //Error = 0x8000000;
            //Console.WriteLine("Season pass buy!");
        }
        public override void Run()
        {
            try
            {
                Account Player = Client.Player;
                if (Player == null)
                    return;

                // ✅ Shop item (season pass)
                int battlePassItemId = 3600000;
                GoodsItem bpGood = ShopManager.GetItemId(battlePassItemId);
                if (bpGood == null)
                {
                    CLogger.Print("[BattlePass] Shop item not found (system_shop).");
                    return;
                }

                // Pick which price to use
                int priceX = bpGood.PriceCash > 0 ? bpGood.PriceCash : bpGood.PriceGold;
                // 0 = Gold, 1 = Cash
                int currencyX = bpGood.PriceCash > 0 ? 1 : 0;

                if (Player.IsPremiumBattlepass == 1)
                {
                    Client.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
                        0x80000000,  // error code (already purchased)
                        1,           // passType (1 = premium)
                        priceX,
                        currencyX,
                        0            // remainDays = 0 since already owned
                    ));
                    return;
                }
                // ✅ Mark premium
                Player.IsPremiumBattlepass = 1;
                DaoManagerSQL.UpdateBattlepassPremium(Player.PlayerId, 1);

                // ✅ Grant rewards for current level
                int level = BattlePassSync.GetCurrentLevel(Player.SeasonExp);
                for (int i = 1; i <= level; i++)
                  //  BattlePassSync.GrantReward(Player, i);

                // ✅ ACK popup
              //  int price = bpGood.PriceCash > 0 ? bpGood.PriceCash : bpGood.PriceGold;
               // int currency = bpGood.PriceCash > 0 ? 1 : 0;

                //Client.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
                //    0,        // error code (0 = success)
                //    1,        // passType (1 = premium battlepass)
                //    price,    // use the right price field
                //    currency, // 0 = gold, 1 = cash
                //    30        // remain days (or calculate from start/end date)
                //));


                // ✅ Update UI info
                Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Player));
            }
            catch (Exception ex)
            {
                CLogger.Print($"[BattlePass] BUY_SEASON_PASS_REQ: {ex.Message}", LoggerType.Error, ex);
            }
        }
    }
}