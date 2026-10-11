using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;

namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_BASE_USER_ENTER_REQ : GameClientPacket
    {
        private uint Error;
        private long PlayerId;
        private string Username;
        public PROTOCOL_BASE_USER_ENTER_REQ(GameClient client, byte[] data)
        {
            Makeme(client, data);
        }
        public override void Read()
        {
            Username = ReadS(ReadC());
            PlayerId = ReadQ();
        }
        public override void Run()
        {
            try
            {
                if (Client == null)
                {
                    return;
                }
                if (Client.Player != null)
                {
                    Error = 0x80000000;
                }
                else
                {
                    Account Player = AccountManager.GetAccountDB(PlayerId, 2, 31);
                    if (Player != null && Player.Username == Username && Player.Status.ServerId != byte.MaxValue)
                    {
                        Client.PlayerId = Player.PlayerId;
                        Player.Connection = Client;
                        Player.ServerId = Client.ServerId;
                        Player.GetAccountInfos(7935);
                        
                        AllUtils.LoadPlayerInventory(Player);
                        AllUtils.LoadPlayerMissions(Player);
                        AllUtils.EnableQuestMission(Player);
                        AllUtils.ValidatePlayerInventoryStatus(Player);

                        //force mode 📄 الملف الرابع: Server.Game/Network/ClientPacket/PROTOCOL_BASE_USER_ENTER_REQ.cs

                        //// بعد ولا قبل ؟
                        // ✅ استدعاء دالة القوة بعد تحميل بيانات اللاعب
                        //CLogger.Print($"[ForcedCharacter] Checking player {Player.PlayerId} on entry", LoggerType.Debug);
                        //AllUtils.LoadForcedCharacter(Player);

                        // ✅ التعامل مع حالة force_character
                        if (Player.ForceCharacter == 0)
                        {
                            AllUtils.RemoveForcedItems(Player);
                        }
                        else if (Player.ForceCharacter == 1)
                        {
                            CLogger.Print($"[ForcedCharacter] Checking player {Player.PlayerId} on entry", LoggerType.Debug);
                            AllUtils.LoadForcedCharacter(Player);

                        }

                        //////////////////////////////////////////////////////////



                        Player.SetPublicIP(Client.GetAddress());
                        Player.Session = new PlayerSession() { SessionId = Client.SessionId, PlayerId = Client.PlayerId };
                        Player.Status.UpdateServer((byte)Client.ServerId);
                        Player.UpdateCacheInfo();
                        Client.Player = Player;
                        ComDiv.UpdateDB("accounts", "ip4_address", Player.PublicIP.ToString(), "player_id", Player.PlayerId);
                        Client.SendPacket(new PROTOCOL_SEASON_CHALLENGE_SEASON_CHANGE(Player));
                        Client.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Player));
                        Player.Connection.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK("Info", 0, 5, false, "To Release Titles Use :titles"));
                    }
                    else
                    {
                        Error = 0x80000000;
                    }
                }
                Client.SendPacket(new PROTOCOL_BASE_USER_ENTER_ACK(Error));
            }
            catch (Exception ex)
            {
                CLogger.Print($"PROTOCOL_BASE_USER_ENTER_REQ: {ex.Message}", LoggerType.Error, ex);
            }
        }
    }
}
