// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Network.ClientPacket.PROTOCOL_BATTLE_READYBATTLE_REQ
using System;
using System.Collections.Generic;
using System.Linq;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.XML;
using Server.Game;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network;
using Server.Game.Network.ServerPacket;

public class PROTOCOL_BATTLE_READYBATTLE_REQ : GameClientPacket
{
    private int Error;

    public PROTOCOL_BATTLE_READYBATTLE_REQ(GameClient client, byte[] data)
    {
        Makeme(client, data);
    }

    public override void Read()
    {
        ReadC();
        Error = ReadD();
    }

    private bool ClassicModeCheck(Account p, RoomModel room)
    {
        if (!room.Name.ToLower().Contains("@camp") && !room.Name.ToLower().Contains("camp") && !room.Name.ToLower().Contains("@cnpb") && !room.Name.ToLower().Contains("cnpb") && !room.Name.ToLower().Contains("@lan") && !room.Name.ToLower().Contains("@lan"))
        {
            return false;
        }
        List<string> blockedItems = new List<string>();
        PlayerEquipment equip = p.Equipment;
        string gameMode = string.Empty;
        if (room.Name.ToLower().Contains("@camp") || room.Name.ToLower().Contains("camp"))
        {
            gameMode = "Camp";
            CheckEquipmentForMode(GameRuleXML.itemscamp, equip, ref blockedItems, gameMode, p);
        }
        if (room.Name.ToLower().Contains("@cnpb") || room.Name.ToLower().Contains("cnpb"))
        {
            gameMode = "CNPB";
            CheckEquipmentForMode(GameRuleXML.itemscnpb, equip, ref blockedItems, gameMode, p);
        }
        if (room.Name.ToLower().Contains("@lan") || room.Name.ToLower().Contains("@lan"))
        {
            gameMode = "LAN";
            CheckEquipmentForMode(GameRuleXML.itemslan, equip, ref blockedItems, gameMode, p);
        }
        if (blockedItems.Count > 0)
        {
            foreach (string item in blockedItems.Distinct())
            {
                p.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(item));
            }
            p.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("Os seguintes itens foram bloqueados no modo " + gameMode + ": " + string.Join(", ", blockedItems.Select((string x) => x.Split('-')[0]).Distinct())));
            return true;
        }
        return false;
    }

    private void CheckEquipmentForMode(List<int> blockedItems, PlayerEquipment equip, ref List<string> blockedList, string gameMode, Account player)
    {
        for (int i = 0; i < blockedItems.Count; i++)
        {
            int id = blockedItems[i];
            CheckAndAddBlockedItem(id, equip.WeaponPrimary, ref blockedList, "Arma Primária", gameMode, player);
            CheckAndAddBlockedItem(id, equip.WeaponSecondary, ref blockedList, "Arma Secundária", gameMode, player);
            CheckAndAddBlockedItem(id, equip.WeaponMelee, ref blockedList, "Faca", gameMode, player);
            CheckAndAddBlockedItem(id, equip.WeaponExplosive, ref blockedList, "Explosivo", gameMode, player);
            CheckAndAddBlockedItem(id, equip.WeaponSpecial, ref blockedList, "Item Especial", gameMode, player);
            CheckAndAddBlockedItem(id, equip.CharaRedId, ref blockedList, "Personagem Vermelho", gameMode, player);
            CheckAndAddBlockedItem(id, equip.CharaBlueId, ref blockedList, "Personagem Azul", gameMode, player);
            CheckAndAddBlockedItem(id, equip.PartHead, ref blockedList, "Chapéu", gameMode, player);
            CheckAndAddBlockedItem(id, equip.DinoItem, ref blockedList, "Dinossauro", gameMode, player);
            CheckAndAddBlockedItem(id, equip.BeretItem, ref blockedList, "Boina", gameMode, player);
        }
    }

    private void CheckAndAddBlockedItem(int blockedId, int itemId, ref List<string> blockedList, string categoryName, string gameMode, Account player)
    {
        if (itemId == blockedId)
        {
            string itemName = GetItemNameFromInventory(player, itemId);
            string message = $"{categoryName} [{itemName} - ID: {itemId}] foi bloqueado no modo {gameMode}";
            blockedList.Add(message);
        }
    }

    private string GetItemNameFromInventory(Account player, int itemId)
    {
        ItemsModel item = player.Inventory.Items.FirstOrDefault((ItemsModel x) => x.Id == itemId);
        return (item != null) ? item.Name : "Desconhecido";
    }

    private void CheckItem(int blockedId, int itemId, ref List<string> blockedList, string categoryName, string gameMode)
    {
        if (itemId == blockedId)
        {
            blockedList.Add($"{categoryName} (ID: {itemId})");
        }
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
            RoomModel Room = Player.Room;
            if (Room == null || Room.GetLeader() == null || !Room.GetChannel(out var Channel) || !Room.GetSlot(Player.SlotId, out var Slot))
            {
                return;
            }
            if (Slot.Equipment == null)
            {
                Client.SendPacket(new PROTOCOL_BATTLE_READYBATTLE_ACK(2147487915u));
                return;
            }
            bool IsBotMode = Room.IsBotMode();
            Slot.SpecGM = (Error == 1 && Player.IsGM()) || (Room.RoomType == RoomCondition.Ace && (Slot.Id < 0 || Slot.Id > 1));
            if (ConfigLoader.EnableRules && ClassicModeCheck(Player, Room))
            {
                return;
            }
            if (Room.Leader == Player.SlotId)
            {
                if (Room.State != RoomState.Ready && Room.State != RoomState.CountDown)
                {
                    return;
                }
                int TotalEnemys = 0;
                int FRPlayers = 0;
                int CTPlayers = 0;
                GetReadyPlayers(Room, ref FRPlayers, ref CTPlayers, ref TotalEnemys);
                if (ConfigLoader.IsTestMode && ConfigLoader.UdpType == UdpState.RELAY)
                {
                    TotalEnemys = 1;
                }
                MapMatch Match = SystemMapXML.GetMapLimit((int)Room.MapId, (int)Room.Rule);
                if (Match != null && Match.Limit == 8 && (FRPlayers >= 4 || CTPlayers >= 4) && Channel.Type != ChannelType.Clan)
                {
                    Client.SendPacket(new PROTOCOL_ROOM_UNREADY_4VS4_ACK());
                }
                else
                {
                    if (ClanMatchCheck(Room, Channel.Type, TotalEnemys))
                    {
                        return;
                    }
                    if (IsBotMode || Room.RoomType == RoomCondition.Tutorial || (TotalEnemys > 0 && !IsBotMode))
                    {
                        Room.ChangeSlotState(Slot, SlotState.READY, sendInfo: false);
                        if (Room.State != RoomState.CountDown)
                        {
                            TryBalanceTeams(Room, IsBotMode);
                        }
                        if (Room.ThisModeHaveCD())
                        {
                            if (Room.State == RoomState.Ready)
                            {
                                SlotModel[] Slots = new SlotModel[2]
                                {
                                    Room.GetSlot(0),
                                    Room.GetSlot(1)
                                };
                                if (Room.RoomType == RoomCondition.Ace && (Slots[0].State != SlotState.READY || Slots[1].State != SlotState.READY))
                                {
                                    Client.SendPacket(new PROTOCOL_BATTLE_READYBATTLE_ACK(2147487753u));
                                    Room.ChangeSlotState(Room.Leader, SlotState.NORMAL, sendInfo: false);
                                    Room.StopCountDown(CountDownEnum.StopByHost);
                                }
                                else
                                {
                                    Room.State = RoomState.CountDown;
                                    Room.UpdateRoomInfo();
                                    Room.StartCountDown();
                                }
                            }
                            else if (Room.State == RoomState.CountDown)
                            {
                                Room.ChangeSlotState(Room.Leader, SlotState.NORMAL, sendInfo: false);
                                Room.StopCountDown(CountDownEnum.StopByHost);
                            }
                        }
                        else
                        {
                            Room.StartBattle(updateInfo: false);
                        }
                        Room.UpdateSlotsInfo();
                    }
                    else if (TotalEnemys == 0 && !IsBotMode)
                    {
                        Client.SendPacket(new PROTOCOL_BATTLE_READYBATTLE_ACK(2147487753u));
                    }
                }
            }
            else if (Room.Slots[Room.Leader].State >= SlotState.LOAD)
            {
                if (Slot.State == SlotState.NORMAL)
                {
                    if (Room.BalanceType == 1 && !IsBotMode)
                    {
                        AllUtils.TryBalancePlayer(Room, Player, InBattle: true, ref Slot);
                    }
                    Room.ChangeSlotState(Slot, SlotState.LOAD, sendInfo: true);
                    Slot.SetMissionsClone(Player.Mission);
                    Client.SendPacket(new PROTOCOL_BATTLE_READYBATTLE_ACK((uint)Slot.State));
                    Client.SendPacket(new PROTOCOL_BATTLE_START_GAME_ACK(Room));
                    PROTOCOL_BATTLE_START_GAME_TRANS_ACK packet = new PROTOCOL_BATTLE_START_GAME_TRANS_ACK(Room, Slot, Player.Title);
                    Room.SendPacketToPlayers(packet, SlotState.READY, 1, Slot.Id);
                    return;
                }
            }
            else if (Slot.State == SlotState.NORMAL)
            {
                Room.ChangeSlotState(Slot, SlotState.READY, sendInfo: true);
                if (Room.State == RoomState.CountDown)
                {
                    TryBalanceTeams(Room, IsBotMode);
                }
            }
            else if (Slot.State == SlotState.READY)
            {
                Room.ChangeSlotState(Slot, SlotState.NORMAL, sendInfo: false);
                if (Room.State == RoomState.CountDown && Room.GetPlayingPlayers((Room.Leader % 2 == 0) ? TeamEnum.CT_TEAM : TeamEnum.FR_TEAM, SlotState.READY, 0) == 0)
                {
                    Room.ChangeSlotState(Room.Leader, SlotState.NORMAL, sendInfo: false);
                    Room.StopCountDown(CountDownEnum.StopByPlayer);
                }
                Room.UpdateSlotsInfo();
            }
        }
        catch (Exception ex)
        {
            CLogger.Print("PROTOCOL_BATTLE_READYBATTLE_REQ: " + ex.Message, LoggerType.Error, ex);
        }
    }

    private void GetReadyPlayers(RoomModel Room, ref int FRPlayers, ref int CTPlayers, ref int TotalEnemys)
    {
        SlotModel[] slots = Room.Slots;
        foreach (SlotModel Slot in slots)
        {
            if (Slot.State == SlotState.READY)
            {
                if (Slot.Team == TeamEnum.FR_TEAM)
                {
                    FRPlayers++;
                }
                else
                {
                    CTPlayers++;
                }
            }
        }
        if (Room.Leader % 2 == 0)
        {
            TotalEnemys = CTPlayers;
        }
        else
        {
            TotalEnemys = FRPlayers;
        }
    }

    private bool ClanMatchCheck(RoomModel Room, ChannelType Type, int TotalEnemys)
    {
        if (ConfigLoader.IsTestMode || Type != ChannelType.Clan)
        {
            return false;
        }
        if (!AllUtils.Have2ClansToClanMatch(Room))
        {
            Client.SendPacket(new PROTOCOL_BATTLE_READYBATTLE_ACK(2147487857u));
            return true;
        }
        if (TotalEnemys > 0 && !AllUtils.HavePlayersToClanMatch(Room))
        {
            Client.SendPacket(new PROTOCOL_BATTLE_READYBATTLE_ACK(2147487858u));
            return true;
        }
        return false;
    }

    private void TryBalanceTeams(RoomModel Room, bool IsBotMode)
    {
        if (Room.BalanceType != 1 || IsBotMode)
        {
            return;
        }
        int[] array;
        switch (AllUtils.GetBalanceTeamIdx(Room, inBattle: false, TeamEnum.ALL_TEAM))
        {
            case TeamEnum.ALL_TEAM:
                return;
            default:
                array = Room.CT_TEAM;
                break;
            case TeamEnum.CT_TEAM:
                array = Room.FR_TEAM;
                break;
        }
        int[] TeamArray = array;
        SlotModel LastSlot = null;
        for (int i = TeamArray.Length - 1; i >= 0; i--)
        {
            SlotModel Slot = Room.Slots[TeamArray[i]];
            if (Slot.State == SlotState.READY && Room.Leader != Slot.Id)
            {
                LastSlot = Slot;
                break;
            }
        }
        if (LastSlot != null && Room.GetPlayerBySlot(LastSlot, out var Player))
        {
            AllUtils.TryBalancePlayer(Room, Player, InBattle: false, ref LastSlot);
        }
    }
}
