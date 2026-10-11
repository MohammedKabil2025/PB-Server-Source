using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_LOBBY_ENTER_REQ : GameClientPacket
    {
        public PROTOCOL_LOBBY_ENTER_REQ(GameClient client, byte[] data)
        {
            Makeme(client, data);
        }
        public override void Read()
        {
        }
        public override void Run()
        {
            try
            {
                Account Player = Client.Player;
                if (Player == null)
                {
                    return;
                }
                Player.LastLobbyEnter = DateTimeUtil.Now();
                if (Player.ChannelId >= 0)
                {
                    ChannelModel Channel = Player.GetChannel();
                    if (Channel != null)
                    {
                        Channel.AddPlayer(Player.Session);
                    }
                }

                //force mode 📍 المكان بالضبط في PROTOCOL_LOBBY_ENTER_REQ.cs:


                if (Player.ForceCharacter == 1)
                {
                    string message = "You are in Forced Mode.\nCharacters and melee weapon are locked\n Inventory & Shop Locked.";
                    Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(message));

                    string message2 = "You are in Forced Mode. Characters and melee weapon are locked.";
                    Player.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK("System", 0, 5, false, message2));
                }

                //////////////////////////////////////////////

                RoomModel Room = Player.Room;
                if (Room != null)
                {
                    if (Player.SlotId >= 0 && Room.State >= RoomState.Loading && Room.Slots[Player.SlotId].State >= SlotState.LOAD)
                    {
                        Client.SendPacket(new PROTOCOL_LOBBY_ENTER_ACK(0));
                        return;
                    }
                    else
                    {
                        Room.RemovePlayer(Player, false);
                    }
                }
                AllUtils.SyncPlayerToFriends(Player, false);
                AllUtils.SyncPlayerToClanMembers(Player);
                AllUtils.GetXmasReward(Player);
                Client.SendPacket(new PROTOCOL_LOBBY_ENTER_ACK(0));
            }
            catch (Exception ex)
            {
                CLogger.Print($"PROTOCOL_LOBBY_ENTER_REQ: {ex.Message}", LoggerType.Error, ex);
            }
        }
    }
}