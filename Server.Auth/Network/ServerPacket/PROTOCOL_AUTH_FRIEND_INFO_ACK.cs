//using Plugin.Core.Models;
//using Plugin.Core.Utility;
//using System.Collections.Generic;

//namespace Server.Auth.Network.ServerPacket
//{
//    public class PROTOCOL_AUTH_FRIEND_INFO_ACK : AuthServerPacket
//    {
//        private readonly List<FriendModel> Friends;
//        public PROTOCOL_AUTH_FRIEND_INFO_ACK(List<FriendModel> Friends)
//        {
//            this.Friends = Friends;
//        }
//        public override void Write()
//        {
//            WriteH(786);
//            WriteC((byte)Friends.Count);
//            foreach (FriendModel Friend in Friends)
//            {
//                PlayerInfo Info = Friend.Info;
//                if (Info == null)
//                {
//                    WriteB(new byte[24]);
//                }
//                else
//                {
//                    WriteC((byte)(Info.Nickname.Length + 1));
//                    WriteN(Info.Nickname, Info.Nickname.Length + 2, "UTF-16LE");
//                    WriteQ(Info.PlayerId);
//                    WriteD(ComDiv.GetFriendStatus(Friend));
//                    WriteD(uint.MaxValue);
//                    WriteC((byte)Info.Rank);
//                    WriteB(new byte[6]);
//                }
//            }
//        }
//    }
//}


using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using System;
using System.Collections.Generic;

namespace Server.Auth.Network.ServerPacket
{
    public class PROTOCOL_AUTH_FRIEND_INFO_ACK : AuthServerPacket
    {
        private readonly List<FriendModel> Friends;
        public PROTOCOL_AUTH_FRIEND_INFO_ACK(List<FriendModel> Friends)
        {
            this.Friends = Friends;
        }
        public override void Write()
        {
            WriteH(786);
            WriteC((byte)Friends.Count);

            for (int i = 0; i < Friends.Count; i++)
            {
                FriendModel friend = Friends[i];
                PlayerInfo info = friend.Info;

                string nickname = info?.Nickname ?? "null";
                long playerId = info?.PlayerId ?? 0;
                long accountId = friend?.PlayerId ?? -1; // FriendModel'da varsa

                try
                {
                    if (info == null || info.Nickname == null)
                    {
                        CLogger.Print(
                            $"[FRIEND_ACK][WARN] Null Info/Nickname | Index={i}, PlayerId={playerId}, Nick='{nickname}', AccountId={accountId}",
                            LoggerType.Warning
                        );
                    }

                    WriteC((byte)(info?.Nickname?.Length + 1 ?? 1));
                    WriteN(info?.Nickname ?? string.Empty, (info?.Nickname?.Length ?? 0) + 2, "UTF-16LE");
                    WriteQ(info?.PlayerId ?? 0);
                    WriteD(ComDiv.GetFriendStatus(friend));
                    WriteD(uint.MaxValue);
                    WriteC((byte)(info?.Rank ?? 0));
                    WriteB(new byte[6]);
                }
                catch (Exception ex)
                {
                    CLogger.Print(
                        $"[FRIEND_ACK][EXCEPTION] Write failure | Index={i}, PlayerId={playerId}, Nick='{nickname}', AccountId={accountId}, Error={ex.Message}",
                        LoggerType.Error,
                        ex
                    );
                }
            }
        }
    }
}
