using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Data.Commands.CommandsInGame
{
    public static class GetAccountInfo
    {
        public static string getById(string str, Account player)
        {
            return GetAccountInfo.BaseCode(AccountManager.GetAccount(long.Parse(str.Substring(5)), 2), player);
        }

        public static string getByNick(string str, Account player)
        {
            return GetAccountInfo.BaseCode(AccountManager.getAccount(str.Substring(5), 1, 2), player);
        }

        private static string BaseCode(Account p, Account player)
        {
            if (p == null || player == null)
                return Translation.GetLabel("GI_Fail");
            DateTime dateTime = p.LastLoginDate != 0U ? DateTime.ParseExact(p.LastLoginDate.ToString(), "yyMMddHHmm", (IFormatProvider)CultureInfo.InvariantCulture) : new DateTime();
            dateTime.AddYears(-10);
            string str1 = Translation.GetLabel("GI_Title") + "\n" + Translation.GetLabel("GI_Id", (object)p.PlayerId) + "\n" + Translation.GetLabel("GI_Nick", (object)p.Nickname) + "\n" + Translation.GetLabel("GI_Rank", (object)p.Rank)  + "\n" + Translation.GetLabel("GI_KD", (object)p.Statistic.GetKDRatio()) + "\n" + Translation.GetLabel("GI_HS", (object)p.Statistic.GetHSRatio()) + "\n" + Translation.GetLabel("GI_LastLogin", dateTime == new DateTime() ? (object)"Nunca" : (object)dateTime.ToString("dd/MM/yy HH:mm")) + "\n" + Translation.GetLabel("GI_LastIP", player.Access >= AccessLevel.GAMEMASTER ? (object)p.PublicIP.ToString() : (object)Translation.GetLabel("GI_BlockedInfo")) + "\n" + Translation.GetLabel("GI_BanObj", (object)player.BanObjectId);
            string str2;
            if (player.Access >= AccessLevel.GAMEMASTER)
                str2 = str1 + "\n" + Translation.GetLabel("GI_HaveAccess2", (object)p.Access);
            else
                str2 = str1 + "\n" + Translation.GetLabel("GI_HaveAccess1", p.HaveGMLevel() ? (object)Translation.GetLabel("GI_Yes") : (object)Translation.GetLabel("GI_No"));
            string msg = str2 + "\n" + Translation.GetLabel("GI_UsingFake", p.Bonus == null || p.Bonus.FakeRank == 55 ? (object)Translation.GetLabel("GI_No") : (object)Translation.GetLabel("GI_Yes")) + "\n" + Translation.GetLabel("GI_Channel", p.ChannelId >= 0 ? (object)string.Format("{0:0##}", (object)(p.ChannelId + 1)) : (object)Translation.GetLabel("GI_None1")) + "\n" + Translation.GetLabel("GI_Room", p.Room != null ? (object)string.Format("{0:0##}", (object)(p.Room.RoomId + 1)) : (object)Translation.GetLabel("GI_None2"));
            player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(msg));
            return Translation.GetLabel("GI_Success");
        }

        public static string getByIPAddress(string str, Account player)
        {
            List<string> accountsByIp = AccountManager.getAccountsByIP(str.Substring(6));
            string msg = Translation.GetLabel("AccIP_Title");
            foreach (string str1 in accountsByIp)
                msg = msg + "\n" + str1;
            player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(msg));
            return Translation.GetLabel("AccIP_Result", (object)accountsByIp.Count);
        }
    }
}
