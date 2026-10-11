// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Network.ClientPacket.PROTOCOL_BATTLE_STARTBATTLE_REQ
using System;
using System.Collections.Generic;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Server.Game;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network;
using Server.Game.Network.ServerPacket;

public class PROTOCOL_BATTLE_STARTBATTLE_REQ : GameClientPacket
{
    private static Dictionary<int, DateTime> _roomTimers = new Dictionary<int, DateTime>();

    private const int MAX_WAIT_TIME = 10;

    public PROTOCOL_BATTLE_STARTBATTLE_REQ(GameClient client, byte[] data)
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
            RoomModel Room = Player.Room;
            if (Room != null && Room.IsPreparing())
            {
                bool IsBotMode = Room.IsBotMode();
                SlotModel Slot = Room.GetSlot(Player.SlotId);
                if (Slot == null)
                {
                    return;
                }
                if (Slot.State == SlotState.PRESTART)
                {
                    Room.ChangeSlotState(Slot, SlotState.BATTLE_READY, sendInfo: true);
                    Slot.StopTiming();
                    if (IsBotMode)
                    {
                        Client.SendPacket(new PROTOCOL_BATTLE_CHANGE_DIFFICULTY_LEVEL_ACK(Room));
                    }
                    Client.SendPacket(new PROTOCOL_ROOM_CHANGE_ROOMINFO_ACK(Room, IsBotMode));
                    int TeamCT = 0;
                    int TeamFR = 0;
                    int FR = 0;
                    int CT = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        SlotModel PlayerSlot = Room.Slots[i];
                        if (PlayerSlot.State < SlotState.LOAD)
                        {
                            continue;
                        }
                        if (PlayerSlot.Team == TeamEnum.FR_TEAM)
                        {
                            FR++;
                        }
                        else
                        {
                            CT++;
                        }
                        if (PlayerSlot.State >= SlotState.BATTLE_READY)
                        {
                            if (PlayerSlot.Team == TeamEnum.FR_TEAM)
                            {
                                TeamFR++;
                            }
                            else
                            {
                                TeamCT++;
                            }
                        }
                    }
                    if (!AllPlayersReady(Room))
                    {
                        if (!_roomTimers.ContainsKey(Room.RoomId))
                        {
                            _roomTimers[Room.RoomId] = DateTime.Now.AddSeconds(10.0);
                            return;
                        }
                        if (DateTime.Now < _roomTimers[Room.RoomId])
                        {
                            return;
                        }
                        _roomTimers.Remove(Room.RoomId);
                    }
                    if (Room.State == RoomState.Battle ||
                    (Room.Slots[Room.Leader].State >= SlotState.BATTLE_READY
                    && IsBotMode && ((Room.Leader % 2 == 0 && TeamFR > FR / 2) || (Room.Leader % 2 == 1
                    && TeamCT > CT / 2))) || (Room.Slots[Room.Leader].State >= SlotState.BATTLE_READY &&
                    (((!ConfigLoader.IsTestMode || ConfigLoader.UdpType != UdpState.RELAY)
                    && TeamCT > CT / 2 && TeamFR > FR / 2) || (ConfigLoader.IsTestMode
                    && ConfigLoader.UdpType == UdpState.RELAY))))
                    {
                        Room.SpawnReadyPlayers(IsBotMode);
                        if (_roomTimers.ContainsKey(Room.RoomId))
                        {
                            _roomTimers.Remove(Room.RoomId);
                        }
                    }
                }
                else
                {
                    Client.SendPacket(new PROTOCOL_SERVER_MESSAGE_KICK_BATTLE_PLAYER_ACK(EventErrorEnum.BATTLE_FIRST_HOLE));
                    Client.SendPacket(new PROTOCOL_BATTLE_GIVEUPBATTLE_ACK(Player, 0));
                    Room.ChangeSlotState(Slot, SlotState.NORMAL, sendInfo: true);
                    AllUtils.BattleEndPlayersCount(Room, IsBotMode);
                }
            }
            else
            {
                Client.SendPacket(new PROTOCOL_SERVER_MESSAGE_KICK_BATTLE_PLAYER_ACK(EventErrorEnum.BATTLE_FIRST_HOLE));
                Client.SendPacket(new PROTOCOL_BATTLE_STARTBATTLE_ACK());
                Room?.ChangeSlotState(Player.SlotId, SlotState.NORMAL, sendInfo: true);
                if (Room == null && Player != null)
                {
                    Client.SendPacket(new PROTOCOL_LOBBY_ENTER_ACK(0u));
                }
            }
        }
        catch (Exception ex)
        {
            CLogger.Print("PROTOCOL_BATTLE_STARTBATTLE_REQ: " + ex.Message, LoggerType.Error, ex);
        }
    }

    private bool AllPlayersReady(RoomModel room)
    {
        SlotModel[] slots = room.Slots;
        foreach (SlotModel slot in slots)
        {
            if (slot.State >= SlotState.LOAD && slot.State < SlotState.BATTLE_READY)
            {
                return false;
            }
        }
        return true;
    }
}
