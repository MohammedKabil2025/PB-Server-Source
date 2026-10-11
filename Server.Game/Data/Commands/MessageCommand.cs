// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Data.Commands.MessageCommand
using System;
using System.Collections.Generic;
using System.Linq;
using Server.Game.Data.Commands;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;

public class MessageCommand : ICommand
{
    public string Command => "sendmsg";

    public string Description => "Send messages to room/channel/all players";

    public string Permission => "helpcommand";

    public string Args => "%options% %text%";

    public string Execute(string Command, string[] Args, Account Player)
    {
        if (Args == null || Args.Length < 2)
        {
            return "Usage: #sendmsg <room/channel/player> <message>";
        }
        string options = Args[0].ToLower().Trim();
        string message = string.Join(" ", Args.Skip(1)).Trim();
        if (string.IsNullOrEmpty(message))
        {
            return "Message cannot be empty.";
        }
        if (message.Length > 500)
        {
            return "Message is too long. Maximum 500 characters.";
        }
        try
        {
            switch (options)
            {
                case "room":
                    return SendToRoom(Player, message);
                case "channel":
                    return SendToChannel(Player, message);
                case "player":
                case "all":
                    return SendToAllPlayers(message);
                default:
                    return "Invalid option. Use: room, channel, or player/all";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in MessageCommand: " + ex.Message);
            return "An error occurred while sending the message.";
        }
    }

    private string SendToRoom(Account player, string message)
    {
        RoomModel room = player.Room;
        if (room == null)
        {
            return "You must be in a room to send a room message.";
        }
        try
        {
            using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(message))
            {
                room.SendPacketToPlayers(packet);
            }
            return $"Message sent to room {room.RoomId}";
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending to room: " + ex.Message);
            return "Failed to send message to room.";
        }
    }

    private string SendToChannel(Account player, string message)
    {
        ChannelModel channel = player.GetChannel();
        if (channel == null)
        {
            return "You must be in a channel lobby to send a channel message.";
        }
        try
        {
            List<Account> waitPlayers = channel.GetWaitPlayers();
            if (waitPlayers == null || waitPlayers.Count == 0)
            {
                return "No players in channel to receive the message.";
            }
            using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(message))
            {
                channel.SendPacketToWaitPlayers(packet);
            }
            return $"Message sent to {waitPlayers.Count} player(s) in channel";
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending to channel: " + ex.Message);
            return "Failed to send message to channel.";
        }
    }

    private string SendToAllPlayers(string message)
    {
        try
        {
            SortedList<long, Account> players = AccountManager.Accounts;
            if (players == null || players.Count == 0)
            {
                return "No players online to receive the message.";
            }
            int sentCount = 0;
            using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(message))
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
}
