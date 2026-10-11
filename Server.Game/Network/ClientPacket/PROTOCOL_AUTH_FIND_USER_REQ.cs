using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_FIND_USER_REQ : GameClientPacket
    {
        private string name;
        private uint Error;
        public PROTOCOL_AUTH_FIND_USER_REQ(GameClient client, byte[] data)
        {
            Makeme(client, data);
        }
        public override void Read()
        {
            name = ReadU(33);
        }
        public override void Run()
        {
            try
            {
                Account player = Client.Player;
                if (player == null)
                    return;

                if (string.IsNullOrEmpty(name))
                {
                    Client.SendPacket(new PROTOCOL_AUTH_FIND_USER_ACK(0x80001803, null));
                    return;
                }

                Account target = AccountManager.GetAccount(name, 1, 31);

                // المستخدم غير موجود أو بيبحث عن نفسه
                if (target == null || target.PlayerId == player.PlayerId)
                {
                    Client.SendPacket(new PROTOCOL_AUTH_FIND_USER_ACK(0x80001803, null));
                    return;
                }

                // نجاح
                Client.SendPacket(new PROTOCOL_AUTH_FIND_USER_ACK(0, target));
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
        }
    }
}