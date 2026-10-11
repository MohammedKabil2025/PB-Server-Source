using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Server.Auth.Data.Models;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_CS_MEMBER_INFO_ACK : AuthServerPacket
    {
        private readonly List<Account> Players;
        public PROTOCOL_CS_MEMBER_INFO_ACK(List<Account> Players)
        {
            this.Players = Players;
        }
        public override void Write()
        {
            WriteH(1869);
            WriteC((byte)Players.Count);
            foreach (Account Member in Players)
            {
                var Status = ComDiv.GetClanStatus(Member.Status, Member.IsOnline);
                WriteC((byte)(Member.Nickname.Length + 1));
                WriteN(Member.Nickname, Member.Nickname.Length + 2, "UTF-16LE");
                WriteQ(Member.PlayerId);
                WriteQ(Status);
                WriteC((byte)Member.Rank);
                WriteC((byte)Member.NickColor);
                //new
                WriteD(1); //member.Statistic.Clan need to be checked. Sometimes it detected Null so MemberList PAGE NOT SHOWS PLAYER.
                WriteD(1); //member.Statistic.Clan need to be checked. Sometimes it detected Null so MemberList PAGE NOT SHOWS PLAYER.
                WriteD(Member.NameCard); //namecard
                WriteC((byte)1); //bonus.nickEffect
                WriteD(10); //Medals of the week
                WriteD(20); //Medals of the month
                WriteD(30); //Medal Gained ( total )
            }
        }
    }
}
