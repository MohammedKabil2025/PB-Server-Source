using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Managers.Events;
using Plugin.Core.Models;
using Server.Auth.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_SEASON_CHALLENGE_INFO_ACK : AuthServerPacket
    {
        private readonly Account player;
        private readonly string SeasonName = "BattlePass";
        private readonly short SeasonDays = 50;

        public PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Account player)
        {
            this.player = player;
        }

        public override void Write()
        {
            var cards = CardData.LoadSeason();
            if (cards == null || cards.Count == 0)
            {
                CLogger.Print("Card data list is null or empty.", LoggerType.Warning);
                return;
            }

            int currentLevel = DetermineCurrentLevel((int)player.SeasonExp, cards);
            int normalLevelsComplete = currentLevel > 0 ? currentLevel - 1 : 0;
            int premiumLevelsComplete = player.IsPremiumBattlepass == 1 ? normalLevelsComplete : 0;
            int remainingBytes = 99 - cards.Count;
            var lastCard = cards.Last();

            WriteH(8449);
            WriteH(0);
            WriteD(1); // enable season
            WriteC((byte)currentLevel);
            WriteD(player.SeasonExp);
            WriteC((byte)normalLevelsComplete);
            WriteC((byte)premiumLevelsComplete);
            WriteC((byte)player.IsPremiumBattlepass);
            WriteC(0);
            WriteD(currentLevel);
            WriteC(1); // active
            WriteU(SeasonName, 42);
            WriteH(SeasonDays);
            WriteD(0);

            foreach (var card in cards)
            {
                //int normalToSend = ResolveShopId(card.NormalCard);
                //int premiumAToSend = ResolveShopId(card.PremiumCardA);
                //int premiumBToSend = ResolveShopId(card.PremiumCardB);

                // keep exact 4 ints per card (16 bytes) — that's what the client expects
                WriteD(ResolveShopId(card.NormalCard, card.NormalDays));
                WriteD(ResolveShopId(card.PremiumCardA, card.PremiumADays));
                WriteD(ResolveShopId(card.PremiumCardB, card.PremiumBDays));
                WriteD(card.RequiredExp);
            }

            for (int i = 0; i < remainingBytes; i++)
                WriteB(new byte[16]);

            WriteB(new byte[12]);

            WriteD(lastCard.StartDate);
            WriteD(lastCard.EndDate);
        }

        //private int ResolveShopId(int itemId, int seconds)
        //{
        //    var good = ShopManager.GetItemByCount(itemId, seconds);
        //    return good?.Id ?? itemId;
        //}

        private int ResolveShopId(int itemId, int seconds)
        {
            // 1️⃣ item عادي (system_shop)
            var good = ShopManager.GetItemByCount(itemId, seconds);
            if (good != null)
                return good.Id;

            // 2️⃣ coupon / effect (system_shop_effects)
            var effectGood = ShopManager.GetEffectGood(itemId, seconds);
            if (effectGood != null)
                return effectGood.Id;

            // fallback
            return itemId;
        }


        private static int DetermineCurrentLevel(int playerExp, List<CardData> cards)
        {
            int level = 0;
            foreach (var card in cards)
            {
                if (playerExp >= card.RequiredExp)
                    level++;
                else
                    break;
            }
            return level == 0 ? 1 : level;
        }
    }
}
