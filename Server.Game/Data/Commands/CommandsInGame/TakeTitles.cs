using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Data.Commands.CommandsInGame
{
    public static class TakeTitles
    {
        public static string GetAllTitles(Account p)
        {
            if (p.Title.OwnerId == 0L)
            {
                DaoManagerSQL.CreatePlayerTitlesDB(p.PlayerId);
                p.Title = new PlayerTitles()
                {
                    OwnerId = p.PlayerId
                };
            }
            PlayerTitles titles = p.Title;
            int num = 0;
            for (int titleId = 1; titleId <= 44; ++titleId)
            {
                TitleModel title = TitleSystemXML.GetTitle(titleId);
                if (title != null && !titles.Contains(title.Flag))
                {
                    ++num;
                    titles.Add(title.Flag);
                    if (titles.Slots < title.Slot)
                        titles.Slots = title.Slot;
                }
            }
            if (num <= 0)
                return "Você já liberou seus titulos";
            ComDiv.UpdateDB("player_titles", "slots", titles.Slots, "owner_id", p.PlayerId);
            DaoManagerSQL.UpdatePlayerTitlesFlags(p.PlayerId, titles.Flags);
            p.SendPacket(new PROTOCOL_BASE_USER_TITLE_INFO_ACK(p));
            return "Titulos liberado [Relogue]";
        }
    }
}
