using Plugin.Core;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Plugin.Core.XML;
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
    public static class ChangePlayerRank
    {
        public static string SetPlayerRank(string str)
        {
            string[] strArray = str.Substring(str.IndexOf(" ") + 1).Split(' ');
            long int64 = Convert.ToInt64(strArray[0]);
            int int32 = Convert.ToInt32(strArray[1]);
            if (int32 > 60 || int32 == 56 || int32 < 0 || int64 <= 0L)
                return Translation.GetLabel("ChangePlyRankWrongValue");
            Account account = AccountManager.GetAccount(int64, 0);
            if (account == null)
                return Translation.GetLabel("ChangePlyRankFailPlayer");
            if (!ComDiv.UpdateDB("accounts", "rank", (object)int32, "player_id", (object)account.PlayerId))
                return Translation.GetLabel("ChangePlyRankFailUnk");
            RankModel rank = PlayerRankXML.GetRank(int32);
            account.Rank = int32;
            SendItemInfo.LoadGoldCash(account);
            account.SendPacket(new PROTOCOL_BASE_RANK_UP_ACK(account.Rank, rank != null ? rank.OnNextLevel : 0), false);
            if (account.Room != null)
                account.Room.UpdateSlotsInfo();
            return Translation.GetLabel("ChangePlyRankSuccess", (object)int32);
        }
    }
}
