using Plugin.Core.Managers.Events;
using Plugin.Core.SQL;
using Server.Game.Data.Commands;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network.ServerPacket;
using System;
using System.Linq;

namespace Server.Game.Data.CommandHandlers
{
    public class BuyBattlePassCommand : ICommand
    {
        public string Command => "buybattlepass";
        public string Description => "Comprar BattlePass Premium";
        public string Permission => "all";
        public string Args => "[1]";

        public string Execute(string command, string[] args, Account player)
        {
            if (args.Length < 1)
            {
                return "Uso: :buybattlepass [1]";
            }

            bool changeValue = int.TryParse(args[0], out int val) && val == 1;
            if (!changeValue)
            {
                return "Valor inválido. Use 1 para comprar.";
            }

            if (player.Cash < 100000)
            {
                return "Saldo insuficiente para comprar BattlePass Premium.";
            }

            player.Cash -= 100000;

            bool updated = DaoManagerSQL.UpdateBattlepassPremium(player.PlayerId, 1);
            if (!updated)
            {
                return "Erro ao atualizar BattlePass no banco de dados.";
            }

            // Enviar pacotes para atualizar o cliente
            int price = 100000;
            int passType = 1; // 1 = Premium Pass
            int currency = 1; // 1 = Cash
            int remainDays = 0;

            // pega a data final da temporada atual
            var season = CardData.LoadSeason().FirstOrDefault();
            if (season != null && season.EndDate > 0)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                remainDays = (int)((season.EndDate - now) / 86400);
                if (remainDays < 0) remainDays = 0;
            }

            player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
                error: 0,
                passType: passType,
                price: price,
                currency: currency,
                remainDays: remainDays
            ));

            player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(player));
            SendItemInfo.LoadGoldCash(player);
            player.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0, player));


            return "Compra do BattlePass Premium efetuada com sucesso! Faça login novamente para ativar.";
        }
    }
}
