using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network.ServerPacket;
using System;
using System.Reflection;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_AUTH_FRIEND_ACCEPT_REQ : GameClientPacket
    {
        private int Index;
        private uint Error;
        public PROTOCOL_AUTH_FRIEND_ACCEPT_REQ(GameClient client, byte[] data)
        {
            Makeme(client, data);
        }
        public override void Read()
        {
            Index = ReadC();
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
                FriendModel Friend = Player.Friend.GetFriend(Index);
                if (Friend != null && Friend.State > 0)
                {
                    Account MemberUser = AccountManager.GetAccount(Friend.PlayerId, 287);
                    if (MemberUser != null)
                    {
                        if (Friend.Info == null)
                        {
                            Friend.SetModel(MemberUser.PlayerId, MemberUser.Rank, MemberUser.NickColor, MemberUser.Nickname, MemberUser.IsOnline, MemberUser.Status);
                        }
                        else
                        {
                            Friend.Info.SetInfo(MemberUser.Rank, MemberUser.NickColor, MemberUser.Nickname, MemberUser.IsOnline, MemberUser.Status);
                        }
                        Friend.State = 0;
                        DaoManagerSQL.UpdatePlayerFriendState(Player.PlayerId, Friend);
                        Client.SendPacket(new PROTOCOL_AUTH_FRIEND_INFO_CHANGE_ACK(FriendChangeState.Accept, null, 0, Index));
                        Client.SendPacket(new PROTOCOL_AUTH_FRIEND_INFO_CHANGE_ACK(FriendChangeState.Update, Friend, Index));
                        int idx = -1;
                        FriendModel FriendUser = MemberUser.Friend.GetFriend(Player.PlayerId, out idx);
                        if (FriendUser != null && FriendUser.State > 0)
                        {
                            if (FriendUser.Info == null)
                            {
                                FriendUser.SetModel(Player.PlayerId, Player.Rank, Player.NickColor, Player.Nickname, Player.IsOnline, Player.Status);
                            }
                            else
                            {
                                FriendUser.Info.SetInfo(Player.Rank, Player.NickColor, Player.Nickname, Player.IsOnline, Player.Status);
                            }

                            FriendUser.State = 0;
                            DaoManagerSQL.UpdatePlayerFriendState(MemberUser.PlayerId, FriendUser);
                            SendFriendInfo.Load(MemberUser, FriendUser, 1);
                            MemberUser.SendPacket(new PROTOCOL_AUTH_FRIEND_INFO_CHANGE_ACK(FriendChangeState.Update, FriendUser, idx), false);
                        }
                    }
                    else
                    {
                        Error = 0x80000000;//STR_TBL_GUI_BASE_NO_USER_IN_USERLIST
                    }
                }
                else
                {
                    Error = 0x80000000;//STR_TBL_GUI_BASE_NO_USER_IN_USERLIST
                }
                if (Error > 0)
                {
                    Client.SendPacket(new PROTOCOL_AUTH_FRIEND_ACCEPT_ACK(Error));
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"PROTOCOL_AUTH_FRIEND_ACCEPT_REQ: {ex.Message}", LoggerType.Error, ex);
            }
        }
    }
}
