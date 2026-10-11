using Plugin.Core.Managers.Events;
using Plugin.Core.SQL;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;
using System.Linq;

namespace Server.Game.Data.Commands
{
    // Token: 0x02000210 RID: 528
    public class BattleEndCommand : ICommand
    {
        // Token: 0x17000011 RID: 17
        // (get) Token: 0x060006CA RID: 1738 RVA: 0x00040BE6 File Offset: 0x0003EDE6
        public string Command
        {
            get
            {
                return "end";
            }
        }

        // Token: 0x17000012 RID: 18
        // (get) Token: 0x060006CB RID: 1739 RVA: 0x00040BED File Offset: 0x0003EDED
        public string Command2
        {
            get
            {
                return "buybattlepass";
            }
        }

        // Token: 0x17000013 RID: 19
        // (get) Token: 0x060006CC RID: 1740 RVA: 0x00040BF4 File Offset: 0x0003EDF4
        public string Description
        {
            get
            {
                return "Fim da batalha do trabalho em andamento";
            }
        }

        // Token: 0x17000014 RID: 20
        // (get) Token: 0x060006CD RID: 1741 RVA: 0x00040BFB File Offset: 0x0003EDFB
        public string Description2
        {
            get
            {
                return "COMANDO apenas no Lobby Chat e na Sala";
            }
        }

        // Token: 0x17000015 RID: 21
        // (get) Token: 0x060006CE RID: 1742 RVA: 0x00040C02 File Offset: 0x0003EE02
        public string Permission
        {
            get
            {
                return "helpcommand";
            }
        }

        // Token: 0x17000016 RID: 22
        // (get) Token: 0x060006CF RID: 1743 RVA: 0x00040C09 File Offset: 0x0003EE09
        public string Args
        {
            get
            {
                return "";
            }
        }

        // Token: 0x060006D0 RID: 1744 RVA: 0x00040C10 File Offset: 0x0003EE10
        public string Execute(string Command, string[] Args, Account Player)
        {
            RoomModel Room = Player.Room;
            bool flag = Room == null;
            string result;
            if (flag)
            {
                Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("É necessária uma sala para executar o comando."));
                result = "É necessária uma sala para executar o comando.";
            }
            else
            {
                bool flag2 = !Room.IsPreparing() || !AllUtils.PlayerIsBattle(Player);
                if (flag2)
                {
                    Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("Ops! A batalha não começou."));
                    result = "Ops! A batalha não começou.";
                }
                else
                {
                    AllUtils.EndBattle(Room);
                    Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("A batalha terminou com sucesso!by GM -_-"));
                    result = "A batalha terminou.";
                }
            }
            return result;
        }

        // Token: 0x060006D1 RID: 1745 RVA: 0x00040C9C File Offset: 0x0003EE9C
        public string Execute2(string Command2, string[] Args, Account Player)
        {
            RoomModel Room = Player.Room;
            int HargaBattlepassPremium = 20000;
            bool flag = Player.Cash < HargaBattlepassPremium;
            string result;

            if (flag)
            {
                Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(
                    "Dinheiro insuficiente para comprar o Battlepass Premium!"));
                result = "Seu dinheiro não é suficiente para comprar o Battlepass Premium.";
            }
            else
            {
                Player.Cash -= HargaBattlepassPremium;
                DaoManagerSQL.UpdateBattlepassPremium(Player.PlayerId, 1);

                // Calculate remain days from season
                var season = CardData.LoadSeason().FirstOrDefault();
                int remainDays = 0;
                if (season != null && season.EndDate > 0)
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    remainDays = (int)((season.EndDate - now) / 86400);
                    if (remainDays < 0) remainDays = 0;
                }

                Player.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0U, Player));

                // ✅ FIX: pass all required params
                Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
                    error: 0,                   // success
                    passType: 1,                // 1 = premium
                    price: HargaBattlepassPremium,
                    currency: 1,                // 1 = cash
                    remainDays: remainDays
                ));

                Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Player));
                SendItemInfo.LoadGoldCash(Player);
                Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(
                    "Battlepass Premium adquirido com sucesso!"));
                result = "Comprou o passe de batalha premium com sucesso.";
            }

            return result;
        }

    }
}
