using Plugin.Core;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Data.Commands.CommandsInGame
{
    public static class GMDisguises
    {
        public static string SetHideColor(Account player)
        {
            if (player == null)
                return Translation.GetLabel("HideGMColorFail");
            if (player.Rank != 53 && player.Rank != 54)
                return Translation.GetLabel("HideGMColorNoRank");
            player.HideGMcolor = !player.HideGMcolor;
            return player.HideGMcolor ? Translation.GetLabel("HideGMColorSuccess1") : Translation.GetLabel("HideGMColorSuccess2");
        }

        public static string SetAntiKick(Account player)
        {
            if (player == null)
                return Translation.GetLabel("AntiKickGMFail");
            player.AntiKickGM = !player.AntiKickGM;
            return player.AntiKickGM ? Translation.GetLabel("AntiKickGMSuccess1") : Translation.GetLabel("AntiKickGMSuccess2");
        }

        public static string SetFakeRank(string str, Account player, RoomModel room)
        {
            int VALOR = int.Parse(str.Substring(3)); // Corrigido aqui
            if (VALOR > 60 || VALOR < 0)
                return Translation.GetLabel("FakeRankWrongValue");
            if (player.Bonus.FakeRank == VALOR)
                return Translation.GetLabel("FakeRankAlreadyFaked");
            if (!ComDiv.UpdateDB("player_bonus", "fake_rank", (object)VALOR, "owner_id", (object)player.PlayerId))
                return Translation.GetLabel("FakeRankFail");
            player.Bonus.FakeRank = VALOR;
            player.SendPacket(new PROTOCOL_BASE_INV_ITEM_DATA_ACK(0, player));
            room?.UpdateSlotsInfo();
            if (VALOR == 55)
                return Translation.GetLabel("FakeRankSuccess1");
            return Translation.GetLabel("FakeRankSuccess2", (object)VALOR);
        }

        public static string SetFakeRankVip(string str, Account player, RoomModel room)
        {
            // Garante que só pode alterar a própria patente
            if (player == null || player.Room == null || player.Room.GetPlayerBySlot(player.SlotId) != player)
                return "Você só pode definir uma patente falsa para você mesmo.";
    
            int VALOR = int.Parse(str.Substring(3));
            if (VALOR > 60 || VALOR < 0)
                return Translation.GetLabel("FakeRankWrongValue");
            if (player.Bonus.FakeRank == VALOR)
                return Translation.GetLabel("FakeRankAlreadyFaked");
            if (!ComDiv.UpdateDB("player_bonus", "fake_rank", (object)VALOR, "owner_id", (object)player.PlayerId))
                return Translation.GetLabel("FakeRankFail");
            player.Bonus.FakeRank = VALOR;
            player.SendPacket(new PROTOCOL_BASE_INV_ITEM_DATA_ACK(0, player));
            room?.UpdateSlotsInfo();
            if (VALOR == 55)
                return Translation.GetLabel("FakeRankSuccess1");
            return Translation.GetLabel("FakeRankSuccess2", (object)VALOR);
        }

        public static string SetFakeNick(string str, Account player, RoomModel room)
        {
            string str1 = str.Substring(3).Trim();
            if (str1.Length > ConfigLoader.MaxNickSize || str1.Length < ConfigLoader.MinNickSize)
                return Translation.GetLabel("FakeNickWrongLength");
            if (AccountManager.isPlayerNameExist(str1))
                return Translation.GetLabel("FakeNickAlreadyExist");
            if (!ComDiv.UpdateDB("accounts", "nickname", (object)str1, "player_id", (object)player.PlayerId))
                return Translation.GetLabel("FakeNickFail");
            player.Nickname = str1;
            player.SendPacket(new PROTOCOL_AUTH_CHANGE_NICKNAME_ACK(str1));
            if (room != null)
            {
                using (PROTOCOL_ROOM_GET_NICKNAME_ACK packet = new PROTOCOL_ROOM_GET_NICKNAME_ACK(player.SlotId, player.Nickname))
                    room.SendPacketToPlayers(packet);
            }
            if (player.ClanId > 0)
            {
                using (PROTOCOL_CS_MEMBER_INFO_CHANGE_ACK packet = new PROTOCOL_CS_MEMBER_INFO_CHANGE_ACK(player))
                    ClanManager.SendPacket(packet, player.ClanId, -1L, true, true);
            }
            AllUtils.SyncPlayerToFriends(player, true);
            return Translation.GetLabel("FakeNickSuccess", (object)str1);
        }

        public static string SetFakeNickVip(string str, Account player, RoomModel room)
        {
            // Garante que só pode alterar o próprio nick
            if (player == null || player.Room == null || player.Room.GetPlayerBySlot(player.SlotId) != player)
                return "Você só pode definir um nick falso para você mesmo.";
            string str1 = str.Substring(3).Trim();
            if (str1.Length > ConfigLoader.MaxNickSize || str1.Length < ConfigLoader.MinNickSize)
                return Translation.GetLabel("FakeNickWrongLength");
            if (AccountManager.isPlayerNameExist(str1))
                return Translation.GetLabel("FakeNickAlreadyExist");
            if (!ComDiv.UpdateDB("accounts", "nickname", (object)str1, "player_id", (object)player.PlayerId))
                return Translation.GetLabel("FakeNickFail");
            player.Nickname = str1;
            player.SendPacket(new PROTOCOL_AUTH_CHANGE_NICKNAME_ACK(str1));
            if (room != null)
            {
                using (PROTOCOL_ROOM_GET_NICKNAME_ACK packet = new PROTOCOL_ROOM_GET_NICKNAME_ACK(player.SlotId, player.Nickname))
                    room.SendPacketToPlayers(packet);
            }
            if (player.ClanId > 0)
            {
                using (PROTOCOL_CS_MEMBER_INFO_CHANGE_ACK packet = new PROTOCOL_CS_MEMBER_INFO_CHANGE_ACK(player))
                    ClanManager.SendPacket(packet, player.ClanId, -1L, true, true);
            }
            AllUtils.SyncPlayerToFriends(player, true);
            return Translation.GetLabel("FakeNickSuccess", (object)str1);
        }
    }
}

