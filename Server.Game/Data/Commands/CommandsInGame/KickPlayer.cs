using Plugin.Core;
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
    public static class KickPlayer
    {
        public static string KickByNick(string str, Account player)
        {
            Account account = AccountManager.GetAccount(str.Substring(3), 1, 0);
            return KickPlayer.BaseKick(player, account);
        }

        public static string KickById(string str, Account player)
        {
            Account account = AccountManager.GetAccount(long.Parse(str.Substring(4)), 0);
            return KickPlayer.BaseKick(player, account);
        }

        private static string BaseKick(Account player, Account victim)
        {
            if (victim == null)
                return Translation.GetLabel("PlayerKickNotFound");
            if (victim.Access > player.Access)
                return Translation.GetLabel("PlayerBanAccessInvalid");
            if (victim.PlayerId == player.PlayerId)
                return Translation.GetLabel("PlayerKickKickYourself");
            if (victim.Connection != null)
            {
                victim.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), false);
                victim.Close(1000, true);
                return Translation.GetLabel("PlayerKickSuccess", (object)victim.Nickname);
            }
            return Translation.GetLabel("PlayerKickOffline", (object)victim.Nickname);
        }
    }
}
