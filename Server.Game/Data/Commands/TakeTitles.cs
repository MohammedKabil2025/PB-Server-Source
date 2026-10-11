using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;


namespace Server.Game.Data.Commands
{
    public class TakeTitles : ICommand
    {
        public string Command => "mytitles";
        public string Description => "Para liberar todos os titulos";
        public string Permission => "all";
        public string Args => "";

        public string Execute(string command, string[] args, Account p)
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
            ComDiv.UpdateDB("player_titles", "slots", (object)titles.Slots, "owner_id", (object)p.PlayerId);
            DaoManagerSQL.UpdatePlayerTitlesFlags(p.PlayerId, titles.Flags);
            p.SendPacket(new PROTOCOL_BASE_USER_TITLE_INFO_ACK(p));
            return "Titulos liberado [Relogue]";
        }
    }
}
