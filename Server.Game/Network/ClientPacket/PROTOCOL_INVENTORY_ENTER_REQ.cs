using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Runtime.Remoting.Messaging;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_INVENTORY_ENTER_REQ : GameClientPacket
    {
        public PROTOCOL_INVENTORY_ENTER_REQ(GameClient client, byte[] data)
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

                //force mode  الملف: Server.Game/Network/ClientPacket/PROTOCOL_INVENTORY_ENTER_REQ.cs

                // ✅ لو اللاعب في وضع الإجبار
                if (Player.ForceCharacter == 1)
                {
                    //string message = "⚠️ You are in Forced Mode.\nInventory access is locked.";
                    //Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(message));
                    //CLogger.Print("[ForcedCharacter] Blocked inventory entry for player " + Player.PlayerId, LoggerType.Warning);
                    string message = "You are in Forced Mode. Inventory access is locked.";
                    Player.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK("System", 0, 5, false, message));
                    // ✅ إرسال باكت الرفض عشان العميل متعلقش
                    Client.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("Access denied."));
                    return; // ❌ منع الدخول
                }

                //////////////////////////////////////////////


                RoomModel Room = Player.Room;
                if (Room != null)
                {
                    Room.ChangeSlotState(Player.SlotId, SlotState.INVENTORY, false);
                    Room.StopCountDown(Player.SlotId);
                    Room.UpdateSlotsInfo();
                }
                Client.SendPacket(new PROTOCOL_INVENTORY_ENTER_ACK());
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }
    }
}
