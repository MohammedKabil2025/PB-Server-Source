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
    public static class SendMsgToPlayers
    {
        public static string SendToAll(string str)
        {
            try
            {
                string msg = str.Substring(5);
                SortedList<long, Account> players = AccountManager.Accounts;
                if (players == null || players.Count == 0)
                {
                    return "No players online to receive the message.";
                }
                int sentCount = 0;
                using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(msg))
                {
                    byte[] data = packet.GetCompleteBytes("Player.MessageCommands");
                    foreach (Account member in players.Values)
                    {
                        try
                        {
                            member.SendCompletePacket(data, packet.GetType().Name);
                            sentCount++;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                return $"Message sent to {sentCount} player(s) online";
            }
            catch (Exception ex2)
            {
                Console.WriteLine("Error sending to all players: " + ex2.Message);
                return "Failed to send message to players.";
            }
        }

        public static string SendToRoom(string str, RoomModel room)
        {
            string msg = str.Substring(3);
            if (room == null)
                return Translation.GetLabel("GeneralRoomInvalid");
            using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(msg))
                room.SendPacketToPlayers(packet);
            return Translation.GetLabel("MsgRoomPlayers");
        }
    }
}
