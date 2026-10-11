using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Managers.Events;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;

namespace Plugin.Core.Managers.Seasons
{
    public class BattlePassSync
    {
        private static readonly HashSet<string> GrantedThisPurchase = new HashSet<string>();
        private static readonly HashSet<long> GrantedThisTick = new HashSet<long>();


        public static void AddExp(Account player, int exp)
        {
            Console.WriteLine($"[BattlePass] Adding {exp} EXP to player {player.PlayerId} (current {player.SeasonExp})");

            int oldLevel = GetCurrentLevel(player.SeasonExp);
            player.SeasonExp += exp;
            DaoManagerSQL.UpdateBattlepassExp(player.PlayerId, (uint)player.SeasonExp);
            int newLevel = GetCurrentLevel(player.SeasonExp);

            Console.WriteLine($"[BattlePass] oldLevel={oldLevel}, newLevel={newLevel}");

            if (newLevel > oldLevel)
            {
                for (int lvl = oldLevel + 1; lvl <= newLevel; lvl++)
                {
                    // تسليم المكافآت العادية
                    GrantNormalReward(player, lvl);

                    //// تسليم المكافآت البريميوم فقط إذا اللاعب معه باتل باس
                    //if (player.IsPremiumBattlepass == 1)
                    //    GrantPremiumReward(player, lvl);
                }
            }
            else
            {
                Console.WriteLine("[BattlePass] No level up, no rewards granted");
            }

            player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(player));
        }

        public static void GrantNormalReward(Account player, int level)
        {
            var cards = CardData.LoadSeason();
            var reward = cards.FirstOrDefault(c => c.Number == level);
            if (reward == null || reward.NormalCard <= 0) return;

            // تحويل عدد الأيام إلى uint
            uint normalDays = reward.NormalDays > 0 ? (uint)reward.NormalDays : 1;

            // تسليم الجائزة العادية مرة واحدة فقط
            GrantItem(player, reward.NormalCard, (int)normalDays, level, "Normal");
        }

        //public static void GrantPremiumReward(Account player, int currentLevel)
        //{
        //    var cards = CardData.LoadSeason();

        //    for (int lvl = 1; lvl <= currentLevel; lvl++)
        //    {
        //        var reward = cards.FirstOrDefault(c => c.Number == lvl);
        //        if (reward == null) continue;
        //        if (reward.PremiumCardA > 0)
        //            GiveItem(player, reward.PremiumCardA, (uint)reward.PremiumADays, currentLevel, PROTOCOL_SEASON_CHALLENGE_INFO_ACK.SeasonName);


        //        if (reward.PremiumCardB > 0)
        //            GiveItem(player, reward.PremiumCardB, (uint)reward.PremiumBDays, currentLevel, PROTOCOL_SEASON_CHALLENGE_INFO_ACK.SeasonName);
        //    }
        //}

        private static long? GetItemObjectId(long ownerId, int itemId)
        {
            using (var conn = ConnectionSQL.GetInstance().Conn())
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
            SELECT object_id
            FROM player_items
            WHERE owner_id = @owner AND id = @id
            LIMIT 1;";
                cmd.Parameters.AddWithValue("@owner", ownerId);
                cmd.Parameters.AddWithValue("@id", itemId);

                var result = cmd.ExecuteScalar();
                return result == null ? null : (long?)result;
            }
        }
        //private static void GrantItemBattlePass(Account player, int itemId, int dbDays)
        //{
        //    var itemInfo = ShopManager.GetItemId(itemId)?.Item;
        //    if (itemInfo == null)
        //        return;

        //    uint bpCount = itemInfo.Equip == ItemEquipType.Durable
        //        ? (uint)dbDays
        //        : 1u;

       
        //    string baseName = itemInfo.Name;

        //    // إزالة أي duration مسبق من الاسم
        //    int index = baseName.LastIndexOf(" (");
        //    if (index > 0)
        //    {
        //        baseName = baseName.Substring(0, index); // فقط الاسم الأساسي
        //    }

        //    // توليد الاسم الجديد بناءً على عدد الثواني
        //    ItemsModel model = new ItemsModel
        //    {
        //        Id = itemId,
        //        Count = (uint)bpCount,
        //        Equip = itemInfo.Equip,
        //        Category = itemInfo.Category,
        //        Name = $"{baseName} ({FormatDuration(bpCount)})"
        //    };

         

        //    var objectId = GetItemObjectId(player.PlayerId, itemId);

        //    using (var conn = ConnectionSQL.GetInstance().Conn())
        //    using (var cmd = conn.CreateCommand())
        //    {
        //        conn.Open();

        //        if (objectId == null)
        //        {
                  

        //            Console.WriteLine($"[BattlePass] INSERT Player={player.PlayerId}, Item={itemId}, Days={bpCount}");
        //        }
        //        else
        //        {
        //            // ➕ تمديد المدة / زيادة العدد
        //            cmd.CommandText = @"
        // UPDATE player_items
        // SET count = count + @count
        // WHERE object_id = @obj;";

        //            cmd.Parameters.AddWithValue("@count", (long)bpCount);
        //            cmd.Parameters.AddWithValue("@obj", objectId.Value);

        //            cmd.ExecuteNonQuery();

        //            Console.WriteLine($"[BattlePass] EXTEND Player={player.PlayerId}, Item={itemId}, AddDays={bpCount}");

        //        }

        //    }
        //    player.SendPacket(
        //                    //new PROTOCOL_INVENTORY_GET_INFO_ACK(1, player, itemInfo)
        //                    new PROTOCOL_INVENTORY_GET_INFO_ACK(1, player, model)
        //                );
        //}


        private static string FormatDuration(uint seconds)
        {
            if (seconds % 86400 == 0) // لو قابل للقسمة على 24 ساعة
            {
                uint days = seconds / 86400;
                return $"{days} day{(days > 1 ? "s" : "")}";
            }
            else if (seconds % 3600 == 0) // ساعات كاملة
            {
                uint hours = seconds / 3600;
                return $"{hours} hour{(hours > 1 ? "s" : "")}";
            }
            else // ثواني عادية
            {
                return $"{seconds} sec{(seconds > 1 ? "s" : "")}";
            }
        }

        public static void GrantPremiumReward(Account player, int level)
        {
            if (player.IsPremiumBattlepass != 1)
                return;

            var reward = CardData.LoadSeason()
                                 .FirstOrDefault(c => c.Number == level);

            if (reward == null)
                return;
            if (reward.PremiumCardA > 0)
                GrantItemBattlePass(player, reward.PremiumCardA, (uint)reward.PremiumADays);

            if (reward.PremiumCardB > 0)
                GrantItemBattlePass(player, reward.PremiumCardB, (uint)reward.PremiumBDays);
        }
        private static void GrantItemBattlePass(Account player, int itemId, uint days)
        {
            var itemInfo = ShopManager.GetItemId(itemId)?.Item;

            bool isCoupon = false;

            // لو العنصر مش موجود كـ أسلحة/معدات
            if (itemInfo == null)
            {
                var effectGood = ShopManager.GetEffectGood(itemId, (int)days);
                if (effectGood != null)
                {
                    itemInfo = effectGood.Item;
                    isCoupon = true;
                }
                else
                {
                    // العنصر غير موجود في أي مكان
                    return;
                }
            }

            uint bpCount;
            if (IsTimeBasedItem(itemInfo) && !isCoupon)
            {
                // أسلحة/معدّات → القيمة بالثواني في DB
                bpCount = days;
            }
            else
            {
                // كوبونات → القيمة مباشرة (1, 3, 7, 30)
                bpCount = (uint)days;
            }

            string baseName = itemInfo.Name;
            int index = baseName.LastIndexOf(" (");
            if (index > 0)
                baseName = baseName.Substring(0, index);

            ItemsModel model = new ItemsModel
            {
                Id = itemId,
                Count = bpCount,
                Equip = itemInfo.Equip,
                Category = itemInfo.Category,
                Name = isCoupon
                        ? $"{baseName} ({bpCount})"
                        : $"{baseName} ({FormatDuration(bpCount)})"
            };

            var objectId = GetItemObjectId(player.PlayerId, itemId);

            using (var conn = ConnectionSQL.GetInstance().Conn())
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                if (objectId == null)
                {
                    // INSERT هنا لو تحب
                }
                else
                {
                    cmd.CommandText = @"
        UPDATE player_items
        SET count = count + @count
        WHERE object_id = @obj;";

                    cmd.Parameters.AddWithValue("@count", (long)bpCount);
                    cmd.Parameters.AddWithValue("@obj", objectId.Value);

                    cmd.ExecuteNonQuery();
                }
            }

            player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, player, model));
        }

        private static bool IsTimeBasedItem(ItemsModel item)
        {
            // Durable items عادة بالثواني
            return item.Equip == ItemEquipType.Durable || item.Equip == ItemEquipType.Temporary;
        }

        private static void GrantItemOnce(Account player, int itemId, int dbDays)
        {
            // 🔒 منع التكرار من DB (نهائي)
            if (ItemExistsInDb(player.PlayerId, itemId))
            {
                Console.WriteLine($"[BattlePass] SKIP DB owned item Player={player.PlayerId}, Item={itemId}");
                return;
            }

            var itemInfo = ShopManager.GetItemId(itemId)?.Item;
            if (itemInfo == null)
                return;

            uint count = itemInfo.Equip == ItemEquipType.Durable
                ? (uint)dbDays
                : 1u;

            var model = new ItemsModel(itemId)
            {
                Name = itemInfo.Name,
                Category = itemInfo.Category,
                Equip = itemInfo.Equip,
                Count = count
            };

            using (var conn = ConnectionSQL.GetInstance().Conn())
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
            INSERT INTO player_items (owner_id, id, name, count, equip)
            VALUES (@owner, @id, @name, @count, @equip);";

                cmd.Parameters.AddWithValue("@owner", player.PlayerId);
                cmd.Parameters.AddWithValue("@id", model.Id);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@count", (long)model.Count);
                cmd.Parameters.AddWithValue("@equip", (int)model.Equip);

                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"[BattlePass] Granted item Player={player.PlayerId}, Item={itemId}, Days={dbDays}");
        }

        private static bool ItemExistsInDb(long playerId, int itemId)
        {
            using (var conn = ConnectionSQL.GetInstance().Conn())
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
            SELECT 1
            FROM player_items
            WHERE owner_id = @owner AND id = @id
            LIMIT 1;";
                cmd.Parameters.AddWithValue("@owner", playerId);
                cmd.Parameters.AddWithValue("@id", itemId);

                return cmd.ExecuteScalar() != null;
            }
        }

        // دالة مساعدة لتسليم المكافآت مرة واحدة فقط
        private static void GrantItem(Account player, int itemId, int dbDays, int level, string seasonType)
        {
            // المفتاح لتتبع ما تم تسليمه
            string key = $"{player.PlayerId}:{itemId}:{seasonType}:{level}";

            if (GrantedThisPurchase.Contains(key))
            {
                Console.WriteLine($"[BattlePass] SKIP duplicate reward Player={player.PlayerId}, Item={itemId}, Level={level}");
                return;
            }

            GrantedThisPurchase.Add(key);

            var itemInfo = ShopManager.GetItemId(itemId)?.Item;
            if (itemInfo == null) return;

            var model = new ItemsModel(itemId)
            {
                Name = itemInfo.Name,
                Category = itemInfo.Category,
                Equip = itemInfo.Equip
            };

            uint addCount = itemInfo.Equip == ItemEquipType.Durable
                            ? (uint)dbDays // مباشرة من DB
                            : 1u;

            // تحقق من الانفنتوري
            var existingItem = player.Inventory.GetItem(itemId);

            if (existingItem != null)
            {
                // إذا موجود مسبقًا، نثبت العدد مباشرة حسب DB
                existingItem.Count = addCount;

                ComDiv.UpdateDB(
                    "player_items",
                    "count",
                    (long)existingItem.Count,
                    "owner_id",
                    player.PlayerId,
                    "id",
                    itemId
                );

                Console.WriteLine($"[BattlePass] Item {itemId} already exists for player {player.PlayerId}. Count set to {existingItem.Count} seconds");
            }
            else
            {
                // إضافة العنصر جديد
                model.Count = addCount;

                using (var conn = ConnectionSQL.GetInstance().Conn())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@owner", player.PlayerId);
                    cmd.Parameters.AddWithValue("@itmId", model.Id);
                    cmd.Parameters.AddWithValue("@ItmNm", model.Name);
                    cmd.Parameters.AddWithValue("@count", (long)model.Count);
                    cmd.Parameters.AddWithValue("@equip", (int)model.Equip);

                    cmd.CommandText = @"
                INSERT INTO player_items(owner_id, id, name, count, equip)
                VALUES(@owner, @itmId, @ItmNm, @count, @equip)
                RETURNING object_id;";

                    var data = cmd.ExecuteScalar();
                    if (data != null)
                        model.ObjectId = (long)data;

                    // ❌ متستخدمش AddItem
                    // player.Inventory.AddItem(model);

                    // ✅ أضف العنصر للـ Inventory يدويًا
                    player.Inventory.Items.Add(model);
                    Console.WriteLine($"[BattlePass] Added new item {itemId} for player {player.PlayerId}, Count={addCount} seconds");
                }

                player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, player, model));
            }

        }


        public static void ResetSeason(Account player)
        {
            player.SeasonExp = 0;
            player.IsPremiumBattlepass = 0;
            DaoManagerSQL.UpdateBattlepassExp(player.PlayerId, 0);
            DaoManagerSQL.UpdateBattlepassPremium(player.PlayerId, 0);
            player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(player));
        }

        public static int GetCurrentLevel(int exp)
        {
            var cards = CardData.LoadSeason();
            int level = 0;
            foreach (var card in cards)
            {
                if (exp >= card.RequiredExp)
                    level++;
                else
                    break;
            }
            return level == 0 ? 1 : level;
        }

        public static void ClearGrantedCache()
        {
            GrantedThisTick.Clear();
        }

        public static void GiveItem(Account player, int itemId, uint days, int level, string currentSeason)
        {
            // لا داعي لإعادة تعريف currentSeason، استخدم parameter مباشرة
            string key = $"{player.PlayerId}:{itemId}:{currentSeason}:{level}";

            if (GrantedThisPurchase.Contains(key))
            {
                Console.WriteLine($"[BattlePass] SKIP duplicate reward Player={player.PlayerId}, Item={itemId}, Level={level}");
                return;
            }

            GrantedThisPurchase.Add(key);

            var itemInfo = ShopManager.GetItemId(itemId)?.Item;
            if (itemInfo == null) return;

            var model = new ItemsModel(itemId)
            {
                Name = itemInfo.Name,
                Category = itemInfo.Category,
                Equip = itemInfo.Equip
            };

            // اجعل days قيمة معقولة لتجنب overflow
            uint safeDays = days > 0 && days < 50000 ? days : 1; // لو DB فيها رقم كبير جدًا، اعتبره يوم واحد
            uint addCount = model.Equip == ItemEquipType.Durable ? safeDays * 86400u : 1u;


            // تحقق من الانفنتوري
            var existingItem = player.Inventory.GetItem(itemId);

            if (existingItem != null)
            {
                // إذا موجود مسبقًا، نثبت المدة ولا نضيف أيام جديدة
                Console.WriteLine($"[BattlePass] Item {itemId} already exists for player {player.PlayerId}, skipping add");
            }
            else
            {
                model.Count = addCount;

                using (var conn = ConnectionSQL.GetInstance().Conn())
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@owner", player.PlayerId);
                    cmd.Parameters.AddWithValue("@itmId", model.Id);
                    cmd.Parameters.AddWithValue("@ItmNm", model.Name);
                    cmd.Parameters.AddWithValue("@count", (long)model.Count);
                    cmd.Parameters.AddWithValue("@equip", (int)model.Equip);

                    cmd.CommandText = @"
            INSERT INTO player_items(owner_id, id, name, count, equip)
            VALUES(@owner, @itmId, @ItmNm, @count, @equip)
            RETURNING object_id;";

                    var data = cmd.ExecuteScalar();
                    if (data != null) model.ObjectId = (long)data;
                }

               // player.Inventory.AddItem(model);
                Console.WriteLine($"[BattlePass] Added new item {itemId} for player {player.PlayerId}, Count={addCount} seconds");
            }

            player.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, player, model));
        }

    }
}