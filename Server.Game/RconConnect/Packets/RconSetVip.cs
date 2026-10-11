using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.RconConnect.Packets
{
    public class RconSetVip : RconReceive
    {
        public override void Run()
        {
            long Id = PopLong();
            int vipType = PopInt();

            Account Player = AccountManager.GetAccount(Id, 0);

            MessageModel msg2 = new MessageModel(15)
            {
                SenderName = "EROS PB",
                SenderId = 0,
                Text = $"Parabéns! Você acaba de receber um vip em sua conta, validos por 30 dias",
                State = NoteMessageState.Unreaded
            };

            if (Player != null)
            {
                if (DaoManagerSQL.InsertAccountIfNotExists(Player.PlayerId, vipType, 4))
                {
                    CLogger.Print("Player " + Id + " received " + vipType + " vip", LoggerType.Warning);
                    Player.SendPacket(new PROTOCOL_MESSENGER_NOTE_RECEIVE_ACK(msg2), true);
                }
            }
        }
    }
}
