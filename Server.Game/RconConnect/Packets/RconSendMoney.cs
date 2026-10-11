using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network.ServerPacket;
using System;
using System.Runtime.Remoting.Messaging;

namespace Server.Game.RconConnect.Packets
{
    public class RconSendMoney : RconReceive
    {
        public override void Run()
        {
            // Get infos
            long Id = PopLong();
            int Money = PopInt();

            // Execute the command
            Account Player = AccountManager.GetAccount(Id, 0);

            MessageModel msg2 = new MessageModel(15)
            {
                SenderName = "EROS PB",
                SenderId = 0,
                Text = $"Parabéns! {Money} em cash foram adicionados à sua conta com sucesso.",
                State = NoteMessageState.Unreaded
            };


            if (Player != null)
            {
                if (DaoManagerSQL.UpdateAccountCash(Player.PlayerId, Player.Cash + Money))
                {
                    Player.Cash += Money;
                    Player.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0, Player), false);
                    SendItemInfo.LoadGoldCash(Player);
                    CLogger.Print("Player " + Id + " received " + Money + " money", LoggerType.Warning);
                    Player.SendPacket(new PROTOCOL_MESSENGER_NOTE_RECEIVE_ACK(msg2), true);
                }
            }
        }
    }
}
