using Plugin.Core;
using Plugin.Core.SQL;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Data.Commands.CommandsInGame
{
    public static class SendCashToPlayerDev
    {
        public static string SendCashToPlayer(string str)
        {
            string[] strArray = str.Substring(str.IndexOf(" ") + 1).Split(' ');
            long int64 = Convert.ToInt64(strArray[0]);
            int int32 = Convert.ToInt32(strArray[1]);
            Account account = AccountManager.GetAccount(int64, 0);
            if (account == null || account.Cash + int32 > 999999999)
                return Translation.GetLabel("[*]SendCash_Fail4");
            // Proteção: não permitir valor negativo ou zero
            if (int32 < 0)
                return Translation.GetLabel("GiveCashFail2");
            if (!DaoManagerSQL.UpdateAccountCash(account.PlayerId, account.Cash + int32))
                return Translation.GetLabel("GiveCashFail2");
            account.Cash += int32;
            account.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0, account));
            SendItemInfo.LoadGoldCash(account);
            return Translation.GetLabel("GiveCashSuccessD", (object)account.Cash, (object)account.Nickname);
        }
    }
}
