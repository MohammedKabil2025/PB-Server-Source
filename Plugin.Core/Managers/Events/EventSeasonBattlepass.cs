using Npgsql;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using System;
using System.Collections.Generic;
using System.Data;

namespace Plugin.Core.Managers.Events
{
    public class CardData
    {
        public int Number { get; set; }
        public int RequiredExp { get; set; }
        public int NormalCard { get; set; }
        public int PremiumCardA { get; set; }
        public int PremiumCardB { get; set; }

        public int NormalDays { get; set; }          // جديد
        public int PremiumADays { get; set; }        // جديد
        public int PremiumBDays { get; set; }        // جديد
        public uint StartDate { get; set; }
        public uint EndDate { get; set; }

        public static readonly List<CardData> Cards = new List<CardData>();

        // Static method to load cards from database and return the list
        public static List<CardData> LoadSeason()
        {
            List<CardData> cards = new List<CardData>();
            try
            {
                using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
                {
                    connection.Open();
                    using (NpgsqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT number, required_exp, normal_card, premium_card_a, premium_card_b,
                                   start_date, end_date, normal_days, premium_a_days, premium_b_days
                            FROM card_data
                            ORDER BY number ASC;";
                        command.CommandType = System.Data.CommandType.Text;

                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CardData card = new CardData()
                                {
                                    Number = reader.GetInt32(reader.GetOrdinal("number")),
                                    RequiredExp = reader.GetInt32(reader.GetOrdinal("required_exp")),
                                    NormalCard = reader.GetInt32(reader.GetOrdinal("normal_card")),
                                    PremiumCardA = reader.GetInt32(reader.GetOrdinal("premium_card_a")),
                                    PremiumCardB = reader.GetInt32(reader.GetOrdinal("premium_card_b")),
                                    StartDate = (uint)reader.GetInt64(reader.GetOrdinal("start_date")),
                                    EndDate = (uint)reader.GetInt64(reader.GetOrdinal("end_date")),
                                    NormalDays = reader.GetInt32(reader.GetOrdinal("normal_days")),
                                    PremiumADays = reader.GetInt32(reader.GetOrdinal("premium_a_days")),
                                    PremiumBDays = reader.GetInt32(reader.GetOrdinal("premium_b_days"))
                                };

                                cards.Add(card);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"Failed to load card data: {ex.Message}", LoggerType.Error, ex);
            }

            return cards;
        }
    }
}
//        public static void LoadCards()
//        {
//            try
//            {
//                using (NpgsqlConnection connection = ConnectionSQL.GetInstance().Conn())
//                {
//                    NpgsqlCommand command = connection.CreateCommand();
//                    connection.Open();
//                    command.CommandText = "SELECT * FROM card_data";
//                    command.CommandType = CommandType.Text;
//                    NpgsqlDataReader data = command.ExecuteReader();

//                    while (data.Read())
//                    {
//                        CardData card = new CardData()
//                        {
//                            Number = int.Parse(data["number"].ToString()),
//                            RequiredExp = int.Parse(data["required_exp"].ToString()),
//                            NormalCard = int.Parse(data["normal_card"].ToString()),
//                            PremiumCardA = int.Parse(data["premium_card_a"].ToString()),
//                            PremiumCardB = int.Parse(data["premium_card_b"].ToString()),
//                            StartDate = uint.Parse(data["start_date"].ToString()),
//                            EndDate = uint.Parse(data["end_date"].ToString())
//                        };

//                        //CLogger.Print($"[LoadCards] Card {card.Number} StartDate: {card.StartDate}, EndDate: {card.EndDate}", LoggerType.Info);

//                        Cards.Add(card);
//                    }

//                    command.Dispose();
//                    data.Close();
//                    connection.Dispose();
//                    connection.Close();
//                }
//            }
//            catch (Exception ex)
//            {
//                CLogger.Print(ex.Message, LoggerType.Error, ex);
//            }
//            //CLogger.Print($"Cards Loaded: {Cards.Count} Card Data", LoggerType.Info);
//            //CLogger.Print($"DateTimeUtil.Now(): {DateTimeUtil.Now()}", LoggerType.Info);
//        }
//    }
//}
