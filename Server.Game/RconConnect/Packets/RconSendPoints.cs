using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network.ServerPacket;
using System;
using System.Security.Policy;

namespace Server.Game.RconConnect.Packets
{
    class RconSendPoints : RconReceive
    {
        public override void Run()
        {
            // Get infos
            long Id = PopLong();
            int Points = PopInt();

            // Execute the command
            Account Player = AccountManager.GetAccount(Id, 0);

            MessageModel msg2 = new MessageModel(15)
            {
                SenderName = "EROS PB",
                SenderId = 0,
                Text = $"Parabéns! {Points} em gold foram adicionados à sua conta com sucesso.",
                State = NoteMessageState.Unreaded
            };

            if (Player != null)
            {
                if (DaoManagerSQL.UpdateAccountGold(Player.PlayerId, Player.Gold + Points))
                {
                    Player.Gold += Points;
                    Player.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0, Player));
                    SendItemInfo.LoadGoldCash(Player);
                    CLogger.Print("Player " + Id + " received " + Points + " points", LoggerType.Warning);
                    Player.SendPacket(new PROTOCOL_MESSENGER_NOTE_RECEIVE_ACK(msg2), true);
                }
            }
        }
    }
}
