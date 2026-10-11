using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;
using System;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_FRIEND_INVITED_REQUEST_REQ : GameClientPacket
    {
        private int Index;
        public override void Read()
        {
            Index = ReadC();
        }
        public override void Run()
        {
            try
            {
                Account Player = Client.Player;
                if (Player == null || ComDiv.GetDuration(Player.LastFriendInviteRoom) < 1)
                {
                    return;
                }
                Account Member = GetFriend(Player);
                if (Member != null)
                {
                    if (Member.Status.ServerId == 255 || Member.Status.ServerId == 0)
                    {
                        Client.SendPacket(new PROTOCOL_AUTH_FRIEND_INVITED_ACK(0x80003002));
                        return;
                    }
                    else if (Member.MatchSlot >= 0)
                    {
                        Client.SendPacket(new PROTOCOL_AUTH_FRIEND_INVITED_ACK(0x80003003));
                        return;
                    }
                    int PlayerIndex = Member.Friend.GetFriendIdx(Player.PlayerId);
                    if (PlayerIndex == -1)
                    {
                        Client.SendPacket(new PROTOCOL_AUTH_FRIEND_INVITED_ACK(0x8000103E));
                    }
                    else if (Member.IsOnline)
                    {
                        Member.SendPacket(new PROTOCOL_AUTH_FRIEND_INVITED_REQUEST_ACK(PlayerIndex), false);
                    }
                    else
                    {
                        Client.SendPacket(new PROTOCOL_AUTH_FRIEND_INVITED_ACK(0x8000103F));
                    }
                    Player.LastFriendInviteRoom = DateTimeUtil.Now();
                }
                else
                {
                    Client.SendPacket(new PROTOCOL_AUTH_FRIEND_INVITED_ACK(0x8000103D));
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"PROTOCOL_AUTH_FRIEND_INVITED_REQUEST_REQ: {ex.Message}", LoggerType.Error, ex);
            }
        }
        private Account GetFriend(Account Player)
        {
            FriendModel Friend = Player.Friend.GetFriend(Index);
            if (Friend == null)
            {
                return null;
            }
            return AccountManager.GetAccount(Friend.PlayerId, 287);
        }
    }
}