using Microsoft.Extensions.Logging;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.RconConnect.Packets
{
    public class SendItemPlayer : RconReceive
    {
        public override void Run()
        {
            try
            {
                // Get infos
                int item_id = PopInt();
                int category = PopInt();
                string item_name = PopString();
                int equip = PopInt();
                int itemcount = PopInt();
                int player_id = PopInt();

                // Execute the command
                Account Player = AccountManager.GetAccount(player_id, 0);
                if (Player != null)
                {
                    SendItemWebLoja(item_id, category, item_name, equip, itemcount, player_id);
                }
                else
                {
                    LogError("Player not found or account retrieval failed.");
                }
            }
            catch (Exception ex)
            {
                LogError("Error processing Rcon command: " + ex.Message);
            }
        }

        public static void SendItemWebLoja(int item_id, int category, string item_name, int equip, int itemcount, int player_id)
        {
            try
            {
                Account account = AccountManager.GetAccount(player_id, true);
                if (account == null)
                {
                    CLogger.Print("Account not found or retrieval failed.", LoggerType.Error);
                    return;
                }

                MessageModel msg2 = new MessageModel(15)
                {
                    SenderName = "EROS PB",
                    SenderId = 0,
                    Text = $"Foi adicionado um item {item_name} em seu inventario!",
                    State = NoteMessageState.Unreaded
                };

                CLogger.Print("Player " + player_id + " received " + item_name + " put " + itemcount + " days", LoggerType.Warning);

                AccountManager.checkDelete(player_id, item_id, itemcount);

                account.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, account, new ItemsModel(item_id, (ItemCategory)category, item_name, (ItemEquipType)equip, (uint)itemcount)), true);
                account.SendPacket(new PROTOCOL_BASE_GET_MYINFO_BASIC_ACK(account));
                account.SendPacket(new PROTOCOL_MESSENGER_NOTE_RECEIVE_ACK(msg2), true);
            }
            catch (Exception ex)
            {
                CLogger.Print("Error sending item to player: " + ex.Message, LoggerType.Warning);
            }
        }


        private void LogError(string message)
        {
            CLogger.Print($"[Error] {message}", LoggerType.Warning);
        }
    }
}
