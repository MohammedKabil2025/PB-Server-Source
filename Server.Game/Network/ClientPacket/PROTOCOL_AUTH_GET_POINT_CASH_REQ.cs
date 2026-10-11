using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_GET_POINT_CASH_REQ : GameClientPacket
    {
        public PROTOCOL_AUTH_GET_POINT_CASH_REQ(GameClient client, byte[] data)
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
                Client.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0, Player));
                //Load season pass
                Client.SendPacket(new PROTOCOL_SEASON_CHALLENGE_SEASON_CHANGE(Player));
                Client.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Player));
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }
    }
}