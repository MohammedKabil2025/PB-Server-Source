//using Plugin.Core.Managers.Events;
//using Server.Game.Data.Models;
//using Server.Game.Network.ServerPacket;
//using System.Security.Principal;

//namespace Plugin.Core.Managers.Seasons
//{
//    public class BattlePassSync
//    {
//        public static void SendBattlePass(Account player)
//        {
//            // Load from DB (your CardData loader)
//            var cards = CardData.LoadSeason();

//            // Send season challenge info to client
//            player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(player));

//            // (Optional) log or debug how many cards were loaded
//            // CLogger.Print($"Sent BattlePass to {player.Nickname}: {cards.Count} cards loaded", LoggerType.Info);
//        }
//    }
//}
