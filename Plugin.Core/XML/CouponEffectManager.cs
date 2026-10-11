using Microsoft.Extensions.Logging;
using Npgsql;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Xml;

namespace Plugin.Core.XML
{
    public static class CouponEffectManager
    {
        private static List<CouponFlag> Effects = new List<CouponFlag>();

        public static void LoadCouponFlags()
        {
            try
            {
                using (NpgsqlConnection npgsqlConnection = ConnectionSQL.GetInstance().Conn())
                {
                    NpgsqlCommand command = npgsqlConnection.CreateCommand();
                    npgsqlConnection.Open();
                    command.CommandText = "SELECT * FROM server_effect_flags";
                    command.CommandType = CommandType.Text;
                    NpgsqlDataReader npgsqlDataReader = command.ExecuteReader(CommandBehavior.Default);
                    while (npgsqlDataReader.Read())
                    {
                        CouponFlag couponFlag = new CouponFlag()
                        {
                            ItemId = npgsqlDataReader.GetInt32(0),
                            EffectFlag = (CouponEffects)npgsqlDataReader.GetInt64(1)
                        };
                        CouponEffectManager.Effects.Add(couponFlag);
                    }
                    command.Dispose();
                    npgsqlDataReader.Close();
                    npgsqlConnection.Dispose();
                    npgsqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.ToString(), LoggerType.Warning);
            }
        }

        public static CouponFlag getCouponEffect(int id)
        {
            for (int index = 0; index < CouponEffectManager.Effects.Count; ++index)
            {
                CouponFlag effect = CouponEffectManager.Effects[index];
                if (effect.ItemId == id)
                    return effect;
            }
            return (CouponFlag)null;
        }
    }
}