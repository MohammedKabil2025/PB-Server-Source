using Microsoft.Extensions.Logging;
using Npgsql;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Server.Game.Data.Managers
{
    public static class AccountManager
    {
        public static SortedList<long, Account> Accounts = new SortedList<long, Account>();
        public static void AddAccount(Account acc)
        {
            lock (Accounts)
            {
                if (!Accounts.ContainsKey(acc.PlayerId))
                {
                    Accounts.Add(acc.PlayerId, acc);
                }
            }
        }
        public static Account GetAccountDB(object valor, int type, int searchDBFlag)
        {
            if (type == 2 && (long)valor == 0 || (type == 0 || type == 1) && (string)valor == "")
            {
                CLogger.Print("GetAccountDB: Valor is null or empty.", LoggerType.Error);
                return null;
            }
            Account Player = null;
            try
            {
                using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = connection.CreateCommand();
                    connection.Open();
                    command.Parameters.AddWithValue("@value", valor);
                    command.CommandText = "SELECT * FROM accounts WHERE " + (type == 0 ? "username" : type == 1 ? "nickname" : "player_id") + "=@value";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader Data = command.ExecuteReader();
                    while (Data.Read())
                    {
                        Player = new Account()
                        {
                            Username = Data["username"].ToString(),
                            Password = Data["password"].ToString()
                        };
                        Player.SetPlayerId(long.Parse(Data["player_id"].ToString()), searchDBFlag);
                        Player.Email = Data["email"].ToString();
                        Player.Age = int.Parse(Data["age"].ToString());
                        Player.SetPublicIP(Data.GetString(5));
                        Player.Nickname = Data["nickname"].ToString();
                        Player.NickColor = int.Parse(Data["nick_color"].ToString());
                        Player.Rank = int.Parse(Data["rank"].ToString());
                        Player.Exp = int.Parse(Data["experience"].ToString());
                        Player.Gold = int.Parse(Data["gold"].ToString());
                        Player.Cash = int.Parse(Data["cash"].ToString());
                        Player.CafePC = (CafeEnum)int.Parse(Data["pc_cafe"].ToString());
                        Player.Access = (AccessLevel)int.Parse(Data["access_level"].ToString());
                        Player.IsOnline = bool.Parse(Data["online"].ToString());
                        Player.TourneyLevel = int.Parse(Data["tourney_level"].ToString());
                        Player.ClanId = int.Parse(Data["clan_id"].ToString());
                        Player.ClanAccess = int.Parse(Data["clan_access"].ToString());
                        Player.Effects = (CouponEffects)long.Parse(Data["coupon_effect"].ToString());
                        Player.Status.SetData(uint.Parse(Data["status"].ToString()), Player.PlayerId);
                        Player.LastRankUpDate = uint.Parse(Data["last_rank_update"].ToString());
                        Player.BanObjectId = long.Parse(Data["ban_object_id"].ToString());
                        Player.Ribbon = int.Parse(Data["ribbon"].ToString());
                        Player.Ensign = int.Parse(Data["ensign"].ToString());
                        Player.Medal = int.Parse(Data["medal"].ToString());
                        Player.MasterMedal = int.Parse(Data["master_medal"].ToString());
                        Player.Mission.Mission1 = int.Parse(Data["mission_id1"].ToString());
                        Player.Mission.Mission2 = int.Parse(Data["mission_id2"].ToString());
                        Player.Mission.Mission3 = int.Parse(Data["mission_id3"].ToString());
                        Player.Tags = int.Parse(Data["tags"].ToString());
                        Player.InventoryPlus = int.Parse(Data["inventory_plus"].ToString());
                        Player.SeasonExp = int.Parse(Data["season_exp"].ToString());
                        Player.IsPremiumBattlepass = int.Parse(Data["battlepass_premium"].ToString());

                        //force mode 📄 الملف الثاني: Server.Game/Data/Managers/AccountManager.cs
                        Player.ForceCharacter = Data["force_character"] != DBNull.Value ? int.Parse(Data["force_character"].ToString()) : 0;// ✅ جديد
                        ////////////////////////////////////////////////////////////////////
                        ///

                        AddAccount(Player);

                    }
                    command.Dispose();
                    Data.Close();
                    connection.Dispose();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"was a problem loading accounts!\r\n{ex.Message}", LoggerType.Error, ex);
            }
            return Player;
        }

        public static DataTable getAllShopClient(string db)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    npgsqlConnection.Open();
                    using (NpgsqlCommand command = npgsqlConnection.CreateCommand())
                    {
                        command.CommandText = $"SELECT * FROM {db}";

                        using (NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(command))
                        {
                            dataAdapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Houve um problema ao carregar o shop.");
                Console.WriteLine(ex.ToString());
            }
            return dataTable;
        }

        public static List<string> getAccountsByIP(string ip)
        {
            List<string> accountsByIp = new List<string>();
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.Parameters.AddWithValue("@ip", (object)ip);
                    command.CommandText = "SELECT nickname FROM accounts WHERE ip4_address=@ip";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                        accountsByIp.Add(npgsqlDataReader.GetString(0));
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print("was a problem loading your account through Ip\r\n" + ex.ToString());
            }
            return accountsByIp;
        }

        public static bool isPlayerNameExist(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            try
            {
                int num = 0;
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand npgsqlCommand = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    npgsqlCommand.CommandType = CommandType.Text;
                    npgsqlCommand.Parameters.AddWithValue("@name", name);
                    npgsqlCommand.CommandText = "SELECT COUNT(*) FROM accounts WHERE nickname=@name";
                    num = Convert.ToInt32(npgsqlCommand.ExecuteScalar());
                    npgsqlCommand.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }

                return num > 0;
            }
            catch
            {
                return true;
            }
        }

        public static bool updateAccountCash(long player_id, int cash)
        {
            if (player_id == 0L || cash == -1)
            {
                return false;
            }
            // Proteção: nunca permitir valor negativo ou zero
            if (cash <= 0)
            {
                CLogger.Print($"Tentativa de atualizar CASH para valor inválido: {cash} para player_id: {player_id}", LoggerType.Warning);
                return false;
            }
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand npgsqlCommand = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    npgsqlCommand.CommandType = CommandType.Text;
                    npgsqlCommand.Parameters.AddWithValue("@owner", player_id);
                    npgsqlCommand.Parameters.AddWithValue("@cash", cash);
                    npgsqlCommand.CommandText = "UPDATE accounts SET cash=@cash WHERE player_id=@owner";
                    npgsqlCommand.ExecuteNonQuery();
                    npgsqlCommand.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.ToString(), LoggerType.Error);
                return false;
            }
        }

        public static bool updateAccountGold(long player_id, int gold)
        {
            if (player_id == 0L || gold == -1)
            {
                return false;
            }
            // Proteção: nunca permitir valor negativo ou zero
            if (gold <= 0)
            {
                CLogger.Print($"Tentativa de atualizar GOLD para valor inválido: {gold} para player_id: {player_id}", LoggerType.Warning);
                return false;
            }
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand npgsqlCommand = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    npgsqlCommand.CommandType = CommandType.Text;
                    npgsqlCommand.Parameters.AddWithValue("@owner", player_id);
                    npgsqlCommand.Parameters.AddWithValue("@gold", gold);
                    npgsqlCommand.CommandText = "UPDATE accounts SET gold=@gold WHERE player_id=@owner";
                    npgsqlCommand.ExecuteNonQuery();
                    npgsqlCommand.Dispose();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.ToString(), LoggerType.Error);
                return false;
            }
        }

        public static void checkDelete(long player_id, int item_id, long count)
        {
            long countToken = 0;
            long objID = 0;
            Account account = null;
            account = new Account();
            account = AccountManager.GetAccount(player_id, true);

            using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
            {
                NpgsqlCommand npgsqlCommand = npgsqlConnection.CreateCommand();
                npgsqlConnection.Open();
                npgsqlCommand.CommandText = $"SELECT * FROM player_items WHERE owner_id={player_id} AND id={item_id}";
                npgsqlCommand.CommandType = CommandType.Text;
                NpgsqlDataReader npgsqlDataReader = npgsqlCommand.ExecuteReader();

                while (npgsqlDataReader.Read())
                {
                    objID = npgsqlDataReader.GetInt64(0);
                    countToken = npgsqlDataReader.GetInt64(4);
                }

                npgsqlCommand.Dispose();
                npgsqlDataReader.Close();

                if (countToken > 0)
                {
                    // Soma o count ao countToken existente
                    countToken += count;

                    // Atualiza a tabela no banco de dados com o novo valor de countToken
                    NpgsqlCommand updateCommand = npgsqlConnection.CreateCommand();
                    updateCommand.CommandText = $"UPDATE player_items SET count={countToken} WHERE owner_id={player_id} AND id={item_id} AND object_id={objID}";
                    updateCommand.CommandType = CommandType.Text;
                    updateCommand.ExecuteNonQuery();
                    updateCommand.Dispose();
                }
                else
                {

                }
                npgsqlConnection.Close();
                npgsqlConnection.Dispose();
            }
        }


        //public static void GetFriendlyAccounts(PlayerFriends System)
        //{
        //    if (System == null || System.Friends.Count == 0)
        //    {
        //        return;
        //    }
        //    try
        //    {
        //        using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
        //        {
        //            NpgsqlCommand command = connection.CreateCommand();
        //            connection.Open();
        //            string loaded = "";
        //            List<string> parameters = new List<string>();
        //            for (int idx = 0; idx < System.Friends.Count; idx++)
        //            {
        //                FriendModel friend = System.Friends[idx];
        //                string param = "@valor" + idx;
        //                command.Parameters.AddWithValue(param, friend.PlayerId);
        //                parameters.Add(param);
        //            }
        //            loaded = string.Join(",", parameters.ToArray());
        //            command.CommandText = "SELECT nickname, player_id, rank, online, status FROM accounts WHERE player_id in (" + loaded + ") ORDER BY player_id";
        //            NpgsqlDataReader Data = command.ExecuteReader();
        //            while (Data.Read())
        //            {
        //                FriendModel Friend = System.GetFriend(long.Parse(Data["player_id"].ToString()));
        //                if (Friend != null)
        //                {
        //                    Friend.Info.Nickname = Data["nickname"].ToString();
        //                    Friend.Info.Rank = int.Parse(Data["rank"].ToString());
        //                    Friend.Info.IsOnline = bool.Parse(Data["online"].ToString());
        //                    Friend.Info.Status.SetData(uint.Parse(Data["status"].ToString()), Friend.PlayerId);
        //                }
        //            }
        //            command.Dispose();
        //            Data.Dispose();
        //            Data.Close();
        //            connection.Dispose();
        //            connection.Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CLogger.Print($"was a problem loading (FriendlyAccounts); {ex.Message}", LoggerType.Error, ex);
        //    }
        //}
        //public static void GetFriendlyAccounts(PlayerFriends System)
        //{
        //    if (System == null || System.Friends.Count == 0)
        //    {
        //        return;
        //    }
        //    try
        //    {
        //        using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
        //        {
        //            NpgsqlCommand command = connection.CreateCommand();
        //            connection.Open();
        //            string loaded = "";
        //            List<string> parameters = new List<string>();
        //            for (int idx = 0; idx < System.Friends.Count; idx++)
        //            {
        //                FriendModel friend = System.Friends[idx];
        //                string param = "@valor" + idx;
        //                command.Parameters.AddWithValue(param, friend.PlayerId);
        //                parameters.Add(param);
        //            }
        //            loaded = string.Join(",", parameters.ToArray());
        //            command.CommandText = "SELECT nickname, player_id, rank, online, status FROM accounts WHERE player_id in (" + loaded + ") ORDER BY player_id";
        //            NpgsqlDataReader Data = command.ExecuteReader();
        //            while (Data.Read())
        //            {
        //                FriendModel Friend = System.GetFriend(long.Parse(Data["player_id"].ToString()));
        //                if (Friend != null)
        //                {
        //                    Friend.Info.Nickname = Data["nickname"].ToString();
        //                    Friend.Info.Rank = int.Parse(Data["rank"].ToString());
        //                    Friend.Info.IsOnline = bool.Parse(Data["online"].ToString());
        //                    Friend.Info.Status.SetData(uint.Parse(Data["status"].ToString()), Friend.PlayerId);
        //                }
        //            }
        //            command.Dispose();
        //            Data.Dispose();
        //            Data.Close();
        //            connection.Dispose();
        //            connection.Close();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CLogger.Print($"was a problem loading (FriendlyAccounts); {ex.Message}", LoggerType.Error, ex);
        //    }
        //}
        //    csharp Server.Game\Data\Managers\AccountManager.cs
        //public static void GetFriendlyAccounts(PlayerFriends system)
        //        {
        //            if (system == null || system.Friends == null || system.Friends.Count == 0)
        //                return;

        //            try
        //            {
        //                using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
        //                using (NpgsqlCommand command = connection.CreateCommand())
        //                {
        //                    connection.Open();

        //                    // اجمع فقط أصدقاء ليس لديهم حالة معلقة (state == 0) ولا تم تعليمهم كمحذوفين
        //                    List<string> parameters = new List<string>();
        //                    for (int i = 0; i < system.Friends.Count; i++)
        //                    {
        //                        FriendModel friend = system.Friends[i];
        //                        // خليها
        //                        if (friend == null || friend.Removed)
        //                            continue; // تجاهل طلبات الانتظار أو المحذوفين

        //                        string param = "@p" + i;
        //                        command.Parameters.AddWithValue(param, friend.PlayerId);
        //                        parameters.Add(param);
        //                    }

        //                    if (parameters.Count == 0)
        //                    {
        //                        CLogger.Print($"GetFriendlyAccounts: no valid friends to query for owner list.", LoggerType.Debug);
        //                        return;
        //                    }

        //                    command.CommandText = @"
        //                SELECT player_id, nickname, rank, nick_color, online, status
        //                FROM accounts
        //                WHERE player_id IN (" + string.Join(",", parameters) + ")";

        //                    using (NpgsqlDataReader data = command.ExecuteReader())
        //                    {
        //                        while (data.Read())
        //                        {
        //                            long playerId = data.GetInt64(data.GetOrdinal("player_id"));
        //                            FriendModel friend = system.GetFriend(playerId);
        //                            if (friend == null)
        //                                continue;

        //                            // اقرأ الحقول بأمان
        //                            string nickname = data.IsDBNull(data.GetOrdinal("nickname")) ? "" : data.GetString(data.GetOrdinal("nickname"));
        //                            int rank = data.IsDBNull(data.GetOrdinal("rank")) ? 0 : data.GetInt32(data.GetOrdinal("rank"));
        //                            int nickColor = data.IsDBNull(data.GetOrdinal("nick_color")) ? 0 : data.GetInt32(data.GetOrdinal("nick_color"));
        //                            bool isOnline = !data.IsDBNull(data.GetOrdinal("online")) && data.GetBoolean(data.GetOrdinal("online"));
        //                            uint statusValue = 0;
        //                            if (!data.IsDBNull(data.GetOrdinal("status")))
        //                                statusValue = unchecked((uint)data.GetInt64(data.GetOrdinal("status")));

        //                            if (friend.Info == null)
        //                            {
        //                                friend.SetModel(playerId, rank, nickColor, nickname, isOnline, new AccountStatus());
        //                                friend.Info.Status.SetData(statusValue, friend.PlayerId);
        //                            }
        //                            else
        //                            {
        //                                friend.Info.Nickname = nickname;
        //                                friend.Info.Rank = rank;
        //                                friend.Info.NickColor = nickColor;
        //                                friend.Info.SetOnlineStatus(isOnline);
        //                                friend.Info.Status.SetData(statusValue, friend.PlayerId);
        //                            }

        //                            //// تصحيح حالة "online" وهمية
        //                            //if (friend.Info.IsOnline && !Accounts.ContainsKey(friend.PlayerId))
        //                            //{
        //                            //    friend.Info.SetOnlineStatus(false);
        //                            //    friend.Info.Status.ResetData(friend.PlayerId);
        //                            //}
        //                        }
        //                    }
        //                }
        //                CLogger.Print($"GetFriendlyAccounts: Populated friend infos (owner list).", LoggerType.Info);
        //            }
        //            catch (Exception ex)
        //            {
        //                CLogger.Print($"GetFriendlyAccounts ERROR: {ex.Message}", LoggerType.Error, ex);
        //            }
        //        }
        //public static void GetFriendlyAccounts(PlayerFriends system)
        //{
        //    if (system?.Friends == null || system.Friends.Count == 0)
        //        return;

        //    try
        //    {
        //        using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
        //        using (NpgsqlCommand command = connection.CreateCommand())
        //        {
        //            connection.Open();

        //            List<long> friendIds = new List<long>();

        //            // 1️⃣ فلترة صح
        //            foreach (FriendModel friend in system.Friends)
        //            {
        //                if (friend == null)
        //                    continue;

        //                // فقط الفرند المقبولين
        //                if (friend.Removed || friend.State != 0)
        //                    continue;

        //                friendIds.Add(friend.PlayerId);
        //            }

        //            if (friendIds.Count == 0)
        //            {
        //                CLogger.Print("GetFriendlyAccounts: no accepted friends.", LoggerType.Debug);
        //                return;
        //            }

        //            // 2️⃣ Parameters نظيفة بدون i
        //            for (int i = 0; i < friendIds.Count; i++)
        //            {
        //                command.Parameters.AddWithValue($"@p{i}", friendIds[i]);
        //            }

        //            command.CommandText = $@"
        //        SELECT player_id, nickname, rank, nick_color, online, status
        //        FROM accounts
        //        WHERE player_id IN ({string.Join(",", friendIds.Select((_, i) => $"@p{i}"))})
        //    ";

        //            using (NpgsqlDataReader data = command.ExecuteReader())
        //            {
        //                while (data.Read())
        //                {
        //                    long playerId = data.GetInt64(data.GetOrdinal("player_id"));
        //                    FriendModel friend = system.GetFriend(playerId);
        //                    if (friend == null)
        //                        continue;

        //                    string nickname = data.IsDBNull(data.GetOrdinal("nickname")) ? "" : data.GetString(data.GetOrdinal("nickname"));
        //                    int rank = data.IsDBNull(data.GetOrdinal("rank")) ? 0 : data.GetInt32(data.GetOrdinal("rank"));
        //                    int nickColor = data.IsDBNull(data.GetOrdinal("nick_color")) ? 0 : data.GetInt32(data.GetOrdinal("nick_color"));
        //                    bool isOnline = !data.IsDBNull(data.GetOrdinal("online")) && data.GetBoolean(data.GetOrdinal("online"));

        //                    uint statusValue = 0;
        //                    if (!data.IsDBNull(data.GetOrdinal("status")))
        //                        statusValue = unchecked((uint)data.GetInt64(data.GetOrdinal("status")));

        //                    if (friend.Info == null)
        //                    {
        //                        friend.SetModel(playerId, rank, nickColor, nickname, isOnline, new AccountStatus());
        //                    }
        //                    else
        //                    {
        //                        friend.Info.SetInfo(rank, nickColor, nickname, isOnline, friend.Info.Status);
        //                    }

        //                    friend.Info.Status.SetData(statusValue, friend.PlayerId);
        //                }
        //            }
        //        }

        //        // 3️⃣ تنظيف أي Friend فاضي (مهم جدًا)
        //        system.Friends.RemoveAll(f => f == null || f.Info == null);

        //        CLogger.Print($"GetFriendlyAccounts: Loaded info for {system.Friends.Count} friend(s).", LoggerType.Info);
        //    }
        //    catch (Exception ex)
        //    {
        //        CLogger.Print($"GetFriendlyAccounts ERROR: {ex.Message}", LoggerType.Error, ex);
        //    }
        //}
        public static void GetFriendlyAccounts(PlayerFriends System)
        {
            if (System == null || System.Friends.Count == 0)
            {
                return;
            }
            try
            {
                using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = connection.CreateCommand();
                    connection.Open();
                    string loaded = "";
                    List<string> parameters = new List<string>();
                    for (int idx = 0; idx < System.Friends.Count; idx++)
                    {
                        FriendModel friend = System.Friends[idx];
                        string param = "@valor" + idx;
                        command.Parameters.AddWithValue(param, friend.PlayerId);
                        parameters.Add(param);
                    }
                    loaded = string.Join(",", parameters.ToArray());
                    command.CommandText = "SELECT nickname, player_id, rank, online, status FROM accounts WHERE player_id in (" + loaded + ") ORDER BY player_id";
                    NpgsqlDataReader Data = command.ExecuteReader();
                    while (Data.Read())
                    {
                        FriendModel Friend = System.GetFriend(long.Parse(Data["player_id"].ToString()));
                        if (Friend != null)
                        {
                            Friend.Info.Nickname = Data["nickname"].ToString();
                            Friend.Info.Rank = int.Parse(Data["rank"].ToString());
                            Friend.Info.IsOnline = bool.Parse(Data["online"].ToString());
                            Friend.Info.Status.SetData(uint.Parse(Data["status"].ToString()), Friend.PlayerId);
                        }
                    }
                    command.Dispose();
                    Data.Dispose();
                    Data.Close();
                    connection.Dispose();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"was a problem loading (FriendlyAccounts); {ex.Message}", LoggerType.Error, ex);
            }
        }
        public static DataTable getAllAccounts(string db)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    npgsqlConnection.Open();

                    using (NpgsqlCommand command = npgsqlConnection.CreateCommand())
                    {
                        command.CommandText = $"SELECT player_id, nickname, rank, gold, cash, pc_cafe, access_level, online FROM {db}";

                        using (NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(command))
                        {
                            dataAdapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Houve um problema ao carregar as contas.");
                Console.WriteLine(ex.ToString());
            }

            return dataTable;
        }

        public static Account getAccount(
          string text,
          int type,
          int searchFlag)
        {
            if (string.IsNullOrEmpty(text))
                return (Account)null;
            IList<Account> values = AccountManager.Accounts.Values;
            for (int index = 0; index < values.Count; ++index)
            {
                Account account = values[index];
                if (account != null && (type == 1 && account.Nickname == text && account.Nickname.Length > 0 || type == 0 && string.Compare(account.Username, text) == 0))
                    return account;
            }
            return AccountManager.GetAccountDB((object)text, type, searchFlag);
        }
        public static Account GetAccount(long id, int searchFlag)
        {
            if (id == 0)
            {
                return null;
            }
            try
            {
                return Accounts.TryGetValue(id, out Account Player) ? Player : GetAccountDB(id, 2, searchFlag);
            }
            catch
            {
                return null;
            }
        }
        public static Account GetAccount(long id, bool noUseDB)
        {
            if (id == 0)
            {
                return null;
            }
            try
            {
                return Accounts.TryGetValue(id, out Account Player) ? Player : (noUseDB ? null : GetAccountDB(id, 2, 287));
            }
            catch
            {
                return null;
            }
        }
        public static Account GetAccount(string text, int type, int searchFlag)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            IList<Account> List = Accounts.Values;
            foreach (Account Player in List)
            {
                if (Player != null && (type == 1 && Player.Nickname == text && Player.Nickname.Length > 0 || type == 0 && string.Compare(Player.Username, text) == 0))
                {
                    return Player;
                }
            }
            return GetAccountDB(text, type, searchFlag);
        }
        public static bool UpdatePlayerName(string name, long playerId)
        {
            return ComDiv.UpdateDB("accounts", "nickname", name, "player_id", playerId);
        }
    }
}