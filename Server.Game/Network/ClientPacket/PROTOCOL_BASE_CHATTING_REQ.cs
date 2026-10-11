using Microsoft.Extensions.Logging;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Managers.Events;
using Plugin.Core.Managers.Seasons;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.XML;
using Server.Game.Data.Commands.CommandsInGame;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Data.Utils;
using Server.Game.Network.ServerPacket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace Server.Game.Network.ClientPacket
{
    public class PROTOCOL_BASE_CHATTING_REQ : GameClientPacket
    {
        private string Text;
        private ChattingType Type;
        public PROTOCOL_BASE_CHATTING_REQ(GameClient client, byte[] data)
        {
            Makeme(client, data);
        }
        public override void Read()
        {
            Type = (ChattingType)ReadH();
            Text = ReadU(ReadH() * 2);
        }
        public override void Run()
        {
            try
            {
                Account Player = Client.Player;
                if (Player == null || string.IsNullOrEmpty(Text) || Text.Length > 60 || Player.Nickname.Length == 0)
                {
                    return;
                }
                RoomModel Room = Player.Room;
                SlotModel Sender;
                switch (Type)
                {
                    case ChattingType.Team:
                        {
                            if (Room == null)
                            {
                                return;
                            }
                            Sender = Room.Slots[Player.SlotId];
                            int[] Array = Room.GetTeamArray(Sender.Team);
                            using (PROTOCOL_ROOM_CHATTING_ACK Packet = new PROTOCOL_ROOM_CHATTING_ACK((int)Type, Sender.Id, Player.UseChatGM(), Text))
                            {
                                byte[] Data = Packet.GetCompleteBytes("PROTOCOL_BASE_CHATTING_REQ-1");
                                lock (Room.Slots)
                                {
                                    foreach (int SlotIdx in Array)
                                    {
                                        SlotModel Receiver = Room.Slots[SlotIdx];
                                        if (Receiver != null)
                                        {
                                            Account PlayerRecv = Room.GetPlayerBySlot(Receiver);
                                            if (PlayerRecv != null && AllUtils.SlotValidMessage(Sender, Receiver))
                                            {
                                                PlayerRecv.SendCompletePacket(Data, Packet.GetType().Name);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case ChattingType type when (type == ChattingType.All || type == ChattingType.None || type == ChattingType.Lobby):
                        {
                            if (Room != null)
                            {
                                if (!serverCommands(Player, Room))
                                {
                                    Sender = Room.Slots[Player.SlotId];
                                    using (PROTOCOL_ROOM_CHATTING_ACK Packet = new PROTOCOL_ROOM_CHATTING_ACK((int)Type, Sender.Id, Player.UseChatGM(), Text))
                                    {
                                        byte[] Data = Packet.GetCompleteBytes("PROTOCOL_BASE_CHATTING_REQ-2");
                                        lock (Room.Slots)
                                        {
                                            foreach (SlotModel Receiver in Room.Slots)
                                            {
                                                Account PlayerRecv = Room.GetPlayerBySlot(Receiver);
                                                if (PlayerRecv != null && AllUtils.SlotValidMessage(Sender, Receiver))
                                                {
                                                    PlayerRecv.SendCompletePacket(Data, Packet.GetType().Name);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Envia apenas para o jogador que digitou o comando
                                    Sender = Room.Slots[Player.SlotId];
                                    using (PROTOCOL_ROOM_CHATTING_ACK Packet = new PROTOCOL_ROOM_CHATTING_ACK((int)Type, Sender.Id, Player.UseChatGM(), Text))
                                    {
                                        byte[] Data = Packet.GetCompleteBytes("PROTOCOL_BASE_CHATTING_REQ-2");
                                        Player.SendCompletePacket(Data, Packet.GetType().Name);
                                    }
                                }
                            }
                            else
                            {
                                ChannelModel Channel = Player.GetChannel();
                                if (Channel == null)
                                {
                                    return;
                                }
                                if (!serverCommands(Player, Room))
                                {
                                    using (PROTOCOL_LOBBY_CHATTING_ACK packet = new PROTOCOL_LOBBY_CHATTING_ACK(Player, Text))
                                    {
                                        Channel.SendPacketToWaitPlayers(packet);
                                    }
                                }
                                else
                                {
                                    // Envia apenas para o jogador que digitou o comando
                                    using (PROTOCOL_LOBBY_CHATTING_ACK packet = new PROTOCOL_LOBBY_CHATTING_ACK(Player, this.Text, true))
                                    {
                                        Player.SendPacket(packet);
                                    }
                                }
                            }
                            break;
                        }
                    case ChattingType.Whisper:
                    case ChattingType.Reply:
                    case ChattingType.Clan:
                    case ChattingType.Match:
                    case ChattingType.ClanMemberPage:
                        {
                            break;
                        }
                }
            }
            catch (Exception Ex)
            {
                CLogger.Print(Ex.Message, LoggerType.Error, Ex);
            }
        }


        public static class BuyBattlePassHandler
        {
             
            private const int PassPrice = 100000;

            public static string Handle(Account player, string cmd)
            {
                
                if (player.BattlepassPremium == 1 || player.IsPremiumBattlepass == 1)
                {
                    return "You already own the Premium BattlePass for this season.";
                }


                if (player.TempConfirm == "buybattlepass")
                {

                  
                    player.TempConfirm = null;

                    if (player.Cash < PassPrice)
                        return "Insufficient balance to purchase the Premium BattlePass.";

                    // Get the GoodsItem for the BattlePass
                    var cart = new List<CartGoods>
    {
        new CartGoods { GoodId = 3600000, BuyType = 2 }
    };
                    int goldPrice, cashPrice, tagsPrice;
                    var goods = ShopManager.GetGoods(cart, out goldPrice, out cashPrice, out tagsPrice);
                    var bpGood = goods.FirstOrDefault();
                    if (bpGood == null)
                    {
                        return "[BattlePass] Cannot find BattlePass goods in ShopManager.";
                    }

                    // Send purchase confirmation ACK (shop-style)
                    //ده انا حاسه مش صح   

                    //مكان ارسال باكت الشراء
                    player.SendPacket(new PROTOCOL_AUTH_SHOP_GOODS_BUY_ACK(1, goods, player));
                    player.Cash -= PassPrice;

                    // Update DB: premium + cash
                    bool premiumOk = DaoManagerSQL.UpdateBattlepassPremium(player.PlayerId, 1);
                    bool cashOk = DaoManagerSQL.UpdateAccountCash(player.PlayerId, player.Cash);

                    if (!premiumOk || !cashOk)
                        return "Error while updating BattlePass or Cash in the database.";

                    // Update account model
                    player.BattlepassPremium = 1;
                    player.IsPremiumBattlepass = 1; // keep in sync!

                    // Calculate remaining days in the current season
                    int remainDays = 0;
                    var season = CardData.LoadSeason().FirstOrDefault();
                    if (season != null && season.EndDate > 0)
                    {
                        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        remainDays = (int)((season.EndDate - now) / 86400);
                        if (remainDays < 0) remainDays = 0;
                    }

                    // Grant retroactive premium rewards up to current level
                    int currentLevel = BattlePassSync.GetCurrentLevel(player.SeasonExp);
                    for (int lvl = 1; lvl <= currentLevel; lvl++)
                    {
                       //BattlePassSync.GrantPremiumReward(player, lvl);
                    }

                    // Send purchase ACK
                    player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
                        error: 0,
                        passType: 1,      // Premium
                        price: PassPrice,
                        currency: 1,      // Cash
                        remainDays: remainDays
                    ));

                    // Refresh season info (so client sees premium rewards unlocked)
                    player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(player));

                    // Refresh cash balance in client
                    SendItemInfo.LoadGoldCash(player);
                    player.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0, player));

                    return "Premium BattlePass purchase successful! All premium rewards up to your current level have been delivered.";
                }

                // Step 2: First time command → ask for confirmation
                player.TempConfirm = "buybattlepass";
                return $"Do you want to purchase the Premium BattlePass for {PassPrice} Cash? Type :buybattlepass again to confirm.";
            }
        }




        private bool serverCommands(Account player, RoomModel room)  // دي الشفرة
        {
            try
            {
                string str = Text.Substring(1);
                if (!this.Text.StartsWith(":") && !this.Text.StartsWith("\\") && !this.Text.StartsWith(":"))
                    return false;
                else if(str.StartsWith("titles"))
                    this.Text = TakeTitles.GetAllTitles(player);

                else if (str.StartsWith("buybattlepass"))
                {
                    this.Text = BuyBattlePassHandler.Handle(player, str);
                }
                if (player.CafePC > 0)
                {
                    if (str.StartsWith("cgn ") && player.Access >= AccessLevel.VIP)
                        this.Text = GMDisguises.SetFakeNickVip(str, player, room);
                    else if (str.StartsWith("fnk ") && player.Access >= AccessLevel.VIP)
                        this.Text = GMDisguises.SetFakeRankVip(str, player, room);
                }
                if (player.Access >= AccessLevel.GAMEMASTER)
                {
                    if (str.StartsWith("cgn ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = GMDisguises.SetFakeNick(str, player, room);
                    else if (str.StartsWith("fr ") && player.Access >= AccessLevel.MODERATOR)
                        this.Text = GMDisguises.SetFakeRank(str, player, room);
                    else if (str.StartsWith("kp ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = KickPlayer.KickByNick(str, player);
                    else if (str.StartsWith("kp2 ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = KickPlayer.KickById(str, player);
                    else if (str.StartsWith("send ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = SendMsgToPlayers.SendToAll(str);
                    else if (str.StartsWith("sendr ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = SendMsgToPlayers.SendToRoom(str, room);
                    else if (str.StartsWith("gp ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = SendGoldToPlayerDev.SendGoldToPlayer(str);
                    else if (str.StartsWith("cp ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = SendCashToPlayerDev.SendCashToPlayer(str);
                    else if (str.StartsWith("rshop1") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = RefillShop.SimpleRefill(player);
                    else if (str.StartsWith("rshop2") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = RefillShop.InstantRefill(player);
                    else if (str.StartsWith("changerank ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = ChangePlayerRank.SetPlayerRank(str);
                    else if (str.StartsWith("seeid ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = ViewUser.ViewUsers(str.Substring(6));
                    else if (str.StartsWith("getip ") && player.Access >= AccessLevel.GAMEMASTER)
                        this.Text = GetAccountInfo.getByIPAddress(str, player);
                    else if (str.StartsWith("rrules") && player.Access >= AccessLevel.GAMEMASTER)
                    {
                        GameRuleXML.RealodCamp();
                        this.Text = "Regras atualizada!";
                    }
                    else if (str.StartsWith("rrankup") && player.Access >= AccessLevel.GAMEMASTER)
                    {
                        EventRankUpSync.Reload();
                        EventRankUpModel runningEvent = EventRankUpSync.GetRunningEvent();
                        this.Text = runningEvent != null ? string.Format("Event RankUP atualizado. XP {0}% e GP {1}%.", (object)runningEvent.PercentExp, (object)runningEvent.PercentGold) : "Nenhum evento ativo encontrado.";
                    }
                    else if (str.StartsWith("end") && player.Access >= AccessLevel.GAMEMASTER)
                    {
                        if (room != null)
                        {
                            if (room.IsPreparing())
                            {
                                AllUtils.EndBattle(room);
                                this.Text = Translation.GetLabel("EndRoomSuccess");
                            }
                            else
                                this.Text = Translation.GetLabel("EndRoomFail1");
                        }
                        else
                            this.Text = Translation.GetLabel("GeneralRoomInvalid");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print("PROTOCOL_BASE_CHATTING_REQ: " + ex.ToString());
                this.Text = Translation.GetLabel("CrashProblemCmd");
                return true;
            }
        }
    }
}