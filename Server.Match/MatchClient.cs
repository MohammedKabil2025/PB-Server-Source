// Server.Match, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Match.MatchClient
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Network;
using Plugin.Core.SharpDX;
using Plugin.Core.Utility;
using Server.Match.Data.Enums;
using Server.Match.Data.Managers;
using Server.Match.Data.Models;
using Server.Match.Data.Models.Event;
using Server.Match.Data.Models.Event.Event;
using Server.Match.Data.Models.SubHead;
using Server.Match.Data.Sync.Server;
using Server.Match.Data.Utils;
using Server.Match.Network.Actions.Damage;
using Server.Match.Network.Actions.Event;
using Server.Match.Network.Actions.SubHead;
using Server.Match.Network.Packets;

public class MatchClient
{
    private readonly Socket MainSocket;

    private readonly IPEndPoint RemoteAddress;

    public MatchClient(Socket MainSocket, IPEndPoint RemoteAddress)
    {
        this.MainSocket = MainSocket;
        this.MainSocket.DontFragment = false;
        this.MainSocket.Ttl = 128;
        this.RemoteAddress = RemoteAddress;
    }

    public void BeginReceive(byte[] Buffer, DateTime Date)
    {
        PacketModel Packet = new PacketModel
        {
            Data = Buffer,
            ReceiveDate = Date
        };
        SyncClientPacket C = new SyncClientPacket(Packet.Data);
        Packet.Opcode = C.ReadC();
        Packet.Slot = C.ReadC();
        Packet.Time = C.ReadT();
        Packet.Round = C.ReadC();
        Packet.Length = C.ReadUH();
        Packet.Respawn = C.ReadC();
        Packet.RoundNumber = C.ReadC();
        Packet.AccountId = C.ReadC();
        Packet.Unk = C.ReadC();
        Packet.Unk2 = C.ReadD();
        if (Packet.Length > Packet.Data.Length)
        {
            /*CLogger.Print($"Packet with invalid size canceled. [ Length: {Packet.Length} DataLength: {Packet.Data.Length} ]", LoggerType.Warning);
            return;*/
        }
        AllUtils.GetDecryptedData(Packet);
        if (ConfigLoader.IsTestMode && Packet.Unk > 0)
        {
            CLogger.Print(Bitwise.ToHexData($"[N] Test Mode, Packet Unk: {Packet.Unk}", Packet.Data), LoggerType.Opcode);
            CLogger.Print(Bitwise.ToHexData($"[D] Test Mode, Packet Unk: {Packet.Unk}", Packet.WithoutEndData), LoggerType.Opcode);
        }
        if (!ConfigLoader.EnableLog || Packet.Opcode == 131 || Packet.Opcode == 132 || Packet.Opcode == 3 || Packet.Opcode == 4)
        {
        }
        ReadPacket(Packet);
    }

    public void ReadPacket(PacketModel Packet)
    {
        byte[] WithEndData = Packet.WithEndData;
        byte[] WithoutEndData = Packet.WithoutEndData;
        SyncClientPacket C = new SyncClientPacket(WithEndData);
        int BasicBufferLength = WithoutEndData.Length;
        try
        {
            uint UniqueRoomId = 0u;
            uint RoomSeed = 0u;
            int DedicationSlot = 0;
            RoomModel Room = null;
            switch (Packet.Opcode)
            {
                case 65:
                    {
                        string Version2 = $"{C.ReadH()}.{C.ReadH()}";
                        UniqueRoomId = C.ReadUD();
                        RoomSeed = C.ReadUD();
                        DedicationSlot = C.ReadC();
                        Room = RoomsManager.CreateOrGetRoom(UniqueRoomId, RoomSeed);
                        if (Room == null)
                        {
                            break;
                        }
                        PlayerModel Player = Room.AddPlayer(RemoteAddress, Packet, Version2);
                        if (Player != null)
                        {
                            if (!Player.Integrity)
                            {
                                Player.ResetBattleInfos();
                            }
                            byte[] Code = PROTOCOL_CONNECT.GET_CODE();
                            SendPacket(Code, Player.Client);
                            if (ConfigLoader.IsTestMode)
                            {
                                CLogger.Print($"Player Connected. [{Player.Client.Address}:{Player.Client.Port}]", LoggerType.Warning);
                            }
                        }
                        break;
                    }
                case 132:
                    {
                        C.Advance(BasicBufferLength);
                        UniqueRoomId = C.ReadUD();
                        DedicationSlot = C.ReadC();
                        RoomSeed = C.ReadUD();
                        Room = RoomsManager.GetRoom(UniqueRoomId, RoomSeed);
                        if (Room == null)
                        {
                            break;
                        }
                        PlayerModel Player2 = Room.GetPlayer(Packet.Slot, RemoteAddress);
                        if (Player2 == null || !Player2.AccountIdIsValid(Packet.AccountId))
                        {
                            break;
                        }
                        Room.BotMode = true;
                        byte[] Actions = PROTOCOL_BOTS_ACTION.GET_CODE(WithoutEndData, Room, IsBotData: true);
                        byte[] Code2 = AllUtils.BaseWriteCode(132, Actions, Packet.Slot, AllUtils.GetDuration(Player2.StartTime), Packet.Round);
                        for (int i = 0; i < 16; i++)
                        {
                            PlayerModel PlayerR = Room.Players[i];
                            if (PlayerR.Client != null && PlayerR.AccountIdIsValid() && i != Packet.Slot)
                            {
                                SendPacket(Code2, PlayerR.Client);
                            }
                        }
                        break;
                    }
                case 131:
                    {
                        C.Advance(BasicBufferLength);
                        UniqueRoomId = C.ReadUD();
                        DedicationSlot = C.ReadC();
                        RoomSeed = C.ReadUD();
                        Room = RoomsManager.GetRoom(UniqueRoomId, RoomSeed);
                        if (Room == null)
                        {
                            break;
                        }
                        PlayerModel Player5 = Room.GetPlayer(Packet.Slot, RemoteAddress);
                        if (Player5 == null || !Player5.AccountIdIsValid(Packet.AccountId))
                        {
                            break;
                        }
                        Room.BotMode = true;
                        PlayerModel Member = Room.GetPlayer(DedicationSlot, Active: false);
                        byte[] Actions3 = PROTOCOL_BOTS_ACTION.GET_CODE(WithoutEndData, Room, IsBotData: true);
                        byte[] Code4 = AllUtils.BaseWriteCode(132, Actions3, DedicationSlot, AllUtils.GetDuration(Member.StartTime), Packet.Round);
                        for (int k = 0; k < 16; k++)
                        {
                            PlayerModel PlayerR3 = Room.Players[k];
                            if (PlayerR3.Client != null && PlayerR3.AccountIdIsValid() && k != Packet.Slot)
                            {
                                SendPacket(Code4, PlayerR3.Client);
                            }
                        }
                        break;
                    }
                case 97:
                    {
                        UniqueRoomId = C.ReadUD();
                        DedicationSlot = C.ReadC();
                        RoomSeed = C.ReadUD();
                        Room = RoomsManager.GetRoom(UniqueRoomId, RoomSeed);
                        byte[] Data = Packet.Data;
                        if (Room == null)
                        {
                            break;
                        }
                        PlayerModel Player3 = Room.GetPlayer(Packet.Slot, RemoteAddress);
                        if (Player3 != null)
                        {
                            if (ConfigLoader.SendPingSync)
                            {
                                Player3.Latency = AllUtils.PingTime($"{RemoteAddress.Address}", Data, MainSocket.Ttl, 120, isFragmented: false, out var PlayerPing);
                                Player3.Ping = PlayerPing;
                                SendMatchInfo.SendPingSync(Room, Player3);
                            }
                            Player3.LastPing = Packet.ReceiveDate;
                            SendPacket(Data, RemoteAddress);
                        }
                        break;
                    }
                case 4:
                    {
                        C.Advance(BasicBufferLength);
                        UniqueRoomId = C.ReadUD();
                        DedicationSlot = C.ReadC();
                        RoomSeed = C.ReadUD();
                        Room = RoomsManager.GetRoom(UniqueRoomId, RoomSeed);
                        if (Room == null)
                        {
                            break;
                        }
                        PlayerModel Player4 = Room.GetPlayer(Packet.Slot, RemoteAddress);
                        if (Player4 == null || !Player4.AccountIdIsValid(Packet.AccountId))
                        {
                            break;
                        }
                        Player4.RespawnByUser = Packet.Respawn;
                        Room.BotMode = true;
                        if (Room.StartTime == default(DateTime))
                        {
                            break;
                        }
                        byte[] Actions2 = PROTOCOL_BOTS_ACTION.GET_CODE(WithoutEndData, Room, IsBotData: false);
                        byte[] Code3 = AllUtils.BaseWriteCode(4, Actions2, Packet.Slot, AllUtils.GetDuration(Player4.StartTime), Packet.Round);
                        for (int j = 0; j < 16; j++)
                        {
                            PlayerModel PlayerR2 = Room.Players[j];
                            if (PlayerR2.Client != null && PlayerR2.AccountIdIsValid() && j != Packet.Slot)
                            {
                                SendPacket(Code3, PlayerR2.Client);
                            }
                        }
                        break;
                    }
                case 3:
                    {
                        C.Advance(BasicBufferLength);
                        UniqueRoomId = C.ReadUD();
                        DedicationSlot = C.ReadC();
                        RoomSeed = C.ReadUD();
                        Room = RoomsManager.GetRoom(UniqueRoomId, RoomSeed);
                        if (Room == null)
                        {
                            break;
                        }
                        PlayerModel Player6 = Room.GetPlayer(Packet.Slot, RemoteAddress);
                        if (Player6 == null || !Player6.AccountIdIsValid(Packet.AccountId))
                        {
                            break;
                        }
                        Player6.RespawnByUser = Packet.Respawn;
                        if (Room.StartTime == default(DateTime))
                        {
                            break;
                        }
                        if (Room.BotMode)
                        {
                            byte[] Actions4 = PROTOCOL_BOTS_ACTION.GET_CODE(WithoutEndData, Room, IsBotData: false);
                            byte[] Code5 = AllUtils.BaseWriteCode(4, Actions4, Packet.Slot, AllUtils.GetDuration(Room.StartTime), Packet.Round);
                            for (int l = 0; l < 16; l++)
                            {
                                PlayerModel PlayerR4 = Room.Players[l];
                                if (PlayerR4.Client != null && PlayerR4.AccountIdIsValid() && l != Packet.Slot)
                                {
                                    SendPacket(Code5, PlayerR4.Client);
                                }
                            }
                            break;
                        }
                        if (DedicationSlot == 255)
                        {
                            CLogger.Print($"Battle Opcode: {Packet.Opcode}; DedicationSlot invalid in PVP! DedicationSlot: {DedicationSlot}", LoggerType.Warning);
                        }
                        byte[] Actions5 = WritePlayerActionData(WithoutEndData, Room, AllUtils.GetDuration(Player6.StartTime), Packet);
                        byte[] Code6 = AllUtils.BaseWriteCode(4, Actions5, 255, AllUtils.GetDuration(Room.StartTime), Packet.Round);
                        for (int m = 0; m < 16; m++)
                        {
                            PlayerModel PlayerR5 = Room.Players[m];
                            if (PlayerR5.Client != null && PlayerR5.AccountIdIsValid())
                            {
                                SendPacket(Code6, PlayerR5.Client);
                            }
                        }
                        break;
                    }
                case 67:
                    {
                        string Version = $"{C.ReadH()}.{C.ReadH()}";
                        UniqueRoomId = C.ReadUD();
                        RoomSeed = C.ReadUD();
                        DedicationSlot = C.ReadC();
                        Room = RoomsManager.GetRoom(UniqueRoomId, RoomSeed);
                        if (Room != null)
                        {
                            if (Room.RemovePlayer(RemoteAddress, Packet, Version) && ConfigLoader.IsTestMode)
                            {
                                CLogger.Print($"Player Disconnected. [{RemoteAddress.Address}:{RemoteAddress.Port}]", LoggerType.Warning);
                            }
                            if (Room.GetPlayersCount() == 0)
                            {
                                RoomsManager.RemoveAssists(Room.UniqueRoomId, Room.RoomSeed);
                                RoomsManager.RemoveRoom(Room.UniqueRoomId, Room.RoomSeed);
                            }
                        }
                        break;
                    }
                default:
                    CLogger.Print(Bitwise.ToHexData($"Opcode Not Found: [{Packet.Opcode}]", WithEndData), LoggerType.Opcode);
                    break;
            }
        }
        catch (Exception ex)
        {
            CLogger.Print(ex.Message, LoggerType.Error, ex);
        }
    }

    public List<ObjectHitInfo> GetPlayerActionData(ActionModel Action, RoomModel Room, float Time, out byte[] EventsData)
    {
        EventsData = new byte[0];
        if (Room == null)
        {
            return null;
        }
        if (Action.Data.Length == 0)
        {
            return new List<ObjectHitInfo>();
        }
        byte[] Data = Action.Data;
        List<ObjectHitInfo> Objs = new List<ObjectHitInfo>();
        SyncClientPacket C = new SyncClientPacket(Data);
        SyncServerPacket S = new SyncServerPacket();
        uint Event = 0u;
        PlayerModel Player = Room.GetPlayer(Action.Slot, Active: false);
        if (Action.Flag.HasFlag(UdpGameEvent.ActionState))
        {
            Event += 256;
            ActionStateInfo Info = ActionState.ReadInfo(Action, C, GenLog: false);
            if (!Room.BotMode && Player != null && Player.Equip != null)
            {
                int WeaponId = 0;
                byte Extensions = 0;
                if (Info.Action.HasFlag(ActionFlag.WeaponSync))
                {
                    if (Info.Flag.HasFlag(WeaponSyncType.Primary))
                    {
                        WeaponId = Player.Equip.WpnPrimary;
                    }
                    if (Info.Flag.HasFlag(WeaponSyncType.Secondary))
                    {
                        WeaponId = Player.Equip.WpnSecondary;
                    }
                    if (Info.Flag.HasFlag(WeaponSyncType.Melee))
                    {
                        WeaponId = Player.Equip.WpnMelee;
                    }
                    if (Info.Flag.HasFlag(WeaponSyncType.Explosive))
                    {
                        WeaponId = Player.Equip.WpnExplosive;
                    }
                    if (Info.Flag.HasFlag(WeaponSyncType.Special))
                    {
                        WeaponId = Player.Equip.WpnSpecial;
                    }
                    if (Info.Flag.HasFlag(WeaponSyncType.Mission) && Room.RoomType == RoomCondition.Bomb)
                    {
                        WeaponId = 5009001;
                    }
                    if (Info.Flag.HasFlag(WeaponSyncType.Dual))
                    {
                        Extensions = 16;
                        WeaponId = Player.Equip.WpnPrimary;
                    }
                }
                ObjectHitInfo Obj = new ObjectHitInfo(5)
                {
                    ObjId = Player.Slot,
                    WeaponId = WeaponId,
                    Extensions = Extensions
                };
                Objs.Add(Obj);
            }
            ActionState.WriteInfo(S, Info);
            Info = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.Animation))
        {
            Event += 2;
            AnimationInfo Info2 = Animation.ReadInfo(Action, C, GenLog: false);
            Animation.WriteInfo(S, Info2);
            Info2 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.PosRotation))
        {
            Event += 134217728;
            PosRotationInfo Info3 = PosRotation.ReadInfo(Action, C, GenLog: false);
            if (Player != null)
            {
                Player.Position = new Half3(Info3.RotationX, Info3.RotationY, Info3.RotationZ);
            }
            Action.Flag += 8388608u;
            PosRotation.WriteInfo(S, Info3);
            Info3 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.SoundPosRotation))
        {
            Event += 8388608;
            SoundPosRotationInfo Info4 = SoundPosRotation.ReadInfo(Action, C, Time, GenLog: false);
            SoundPosRotation.WriteInfo(S, Info4);
            Info4 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.UseObject))
        {
            Event += 4;
            List<UseObjectInfo> Infos = UseObject.ReadInfo(Action, C, GenLog: false);
            for (int i = 0; i < Infos.Count; i++)
            {
                UseObjectInfo Info5 = Infos[i];
                if (!Room.BotMode && Info5.ObjectId != ushort.MaxValue)
                {
                    ObjectInfo Obj2 = Room.GetObject(Info5.ObjectId);
                    if (Obj2 == null)
                    {
                        continue;
                    }
                    bool SecurityBlock = false;
                    if (Info5.SpaceFlags.HasFlag(CharaMoves.HeliInMove))
                    {
                        DateTime Date = Obj2.UseDate;
                        if (Date.ToString("yyMMddHHmms") == "0101010000")
                        {
                            SecurityBlock = true;
                        }
                    }
                    if (Info5.SpaceFlags.HasFlag(CharaMoves.HeliUnknown))
                    {
                    }
                    if (Info5.SpaceFlags.HasFlag(CharaMoves.HeliLeave))
                    {
                    }
                    if (Info5.SpaceFlags.HasFlag(CharaMoves.HeliStopped))
                    {
                        AnimModel Anim = Obj2.Animation;
                        if (Anim != null && Anim.Id == 0)
                        {
                            Obj2.Model.GetAnim(Anim.NextAnim, 0f, 0f, Obj2);
                        }
                    }
                    if (!SecurityBlock)
                    {
                        ObjectHitInfo ObjH = new ObjectHitInfo(3)
                        {
                            ObjSyncId = 1,
                            ObjId = Obj2.Id,
                            ObjLife = Obj2.Life,
                            AnimId1 = 255,
                            AnimId2 = ((Obj2.Animation != null) ? Obj2.Animation.Id : 255),
                            SpecialUse = AllUtils.GetDuration(Obj2.UseDate)
                        };
                        Objs.Add(ObjH);
                    }
                }
                else
                {
                    AllUtils.RemoveHit(Infos, i--);
                }
            }
            UseObject.WriteInfo(S, Infos);
            Infos = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.ActionForObjectSync))
        {
            Event += 16;
            ActionObjectInfo Info6 = ActionForObjectSync.ReadInfo(Action, C, GenLog: false);
            ActionForObjectSync.WriteInfo(S, Info6);
            Info6 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.RadioChat))
        {
            Event += 32;
            RadioChatInfo Info7 = RadioChat.ReadInfo(Action, C, GenLog: false);
            RadioChat.WriteInfo(S, Info7);
            Info7 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.WeaponSync))
        {
            Event += 67108864;
            WeaponSyncInfo Info8 = WeaponSync.ReadInfo(Action, C, GenLog: false);
            if (Player != null)
            {
                Player.Extensions = Info8.Extensions;
                Player.WeaponId = Info8.WeaponId;
                Player.WeaponClass = Info8.WeaponClass;
                Room.SyncInfo(Objs, 2);
            }
            WeaponSync.WriteInfo(S, Info8);
            Info8 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.WeaponRecoil))
        {
            Event += 128;
            WeaponRecoilInfo Info9 = WeaponRecoil.ReadInfo(Action, C, GenLog: false);
            WeaponRecoil.WriteInfo(S, Info9);
            Info9 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.HpSync))
        {
            Event += 8;
            HPSyncInfo Info10 = HpSync.ReadInfo(Action, C, GenLog: false);
            HpSync.WriteInfo(S, Info10);
            Info10 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.Suicide))
        {
            Event += 1048576;
            List<SuicideInfo> Hits = Suicide.ReadInfo(Action, C, GenLog: false);
            int BasicInfo = 0;
            if (Player != null)
            {
                List<DeathServerData> Deaths = new List<DeathServerData>();
                int ObjIdx = -1;
                for (int j = 0; j < Hits.Count; j++)
                {
                    SuicideInfo Hit = Hits[j];
                    if (Player != null && !Player.Dead && Player.Life > 0)
                    {
                        int Damage = AllUtils.GetHitDamageBot(Hit.HitInfo);
                        int KillerId = Hit.ObjectId;
                        int Type = AllUtils.GetObjectType(Hit.HitInfo);
                        CharaHitPart HitPart = (CharaHitPart)((Hit.HitInfo >> 4) & 0x3F);
                        CharaDeath DeathType = AllUtils.GetCharaDeath(Hit.HitInfo);
                        if (Type == 1)
                        {
                            ObjIdx = KillerId;
                        }
                        BasicInfo = Hit.WeaponId;
                        Player.Life -= Damage;
                        AllUtils.BaseAssistLogic(Room, Player, Player, Damage);
                        if (Player.Life <= 0)
                        {
                            DamageManager.SetDeath(Deaths, Player, Player, DeathType);
                        }
                        else
                        {
                            DamageManager.SetHitEffect(Objs, Player, DeathType, HitPart);
                        }
                        ObjectHitInfo Obj3 = new ObjectHitInfo(2)
                        {
                            ObjId = Player.Slot,
                            ObjLife = Player.Life,
                            DeathType = DeathType,
                            HitPart = HitPart,
                            WeaponId = BasicInfo,
                            Position = Hit.PlayerPos
                        };
                        Objs.Add(Obj3);
                    }
                    else
                    {
                        AllUtils.RemoveHit(Hits, j--);
                    }
                }
                if (Deaths.Count > 0)
                {
                    SendMatchInfo.SendDeathSync(Room, Player, ObjIdx, BasicInfo, Deaths);
                }
                Deaths = null;
            }
            else
            {
                Hits = new List<SuicideInfo>();
            }
            Suicide.WriteInfo(S, Hits);
            Hits = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.MissionData))
        {
            Event += 2048;
            MissionDataInfo Info11 = MissionData.ReadInfo(Action, C, Time, GenLog: false);
            if (Room.Map != null && Player != null && !Player.Dead && Info11.PlantTime > 0f && !Info11.BombEnum.HasFlag(BombFlag.Stop))
            {
                BombPosition Bomb = Room.Map.GetBomb(Info11.BombId);
                if (Bomb != null)
                {
                    bool IsDefuse = Info11.BombEnum.HasFlag(BombFlag.Defuse);
                    Vector3 BombVec3d = (IsDefuse ? Room.BombPosition : (Info11.BombEnum.HasFlag(BombFlag.Start) ? Bomb.Position : new Half3(0, 0, 0)));
                    double PlayerDistance = Vector3.DistanceBomb(Player.Position, BombVec3d);
                    if ((Bomb.EveryWhere || PlayerDistance <= 2.0) && ((Player.Team == 1 && IsDefuse) || (Player.Team == 0 && !IsDefuse)))
                    {
                        if (Player.C4Time != Info11.PlantTime)
                        {
                            Player.C4First = DateTimeUtil.Now();
                            Player.C4Time = Info11.PlantTime;
                        }
                        double Seconds = ComDiv.GetDuration(Player.C4First);
                        float Objective = (IsDefuse ? Player.DefuseDuration : Player.PlantDuration);
                        if ((Time >= Info11.PlantTime + Objective || Seconds >= (double)Objective) && ((!Room.HasC4 && Info11.BombEnum.HasFlag(BombFlag.Start)) || (Room.HasC4 && IsDefuse)))
                        {
                            Room.HasC4 = !Room.HasC4;
                            Info11.Bomb |= 2;
                            SendMatchInfo.SendBombSync(Room, Player, Info11.BombEnum.HasFlag(BombFlag.Defuse) ? 1 : 0, Info11.BombId);
                        }
                    }
                }
            }
            MissionData.WriteInfo(S, Info11);
            Info11 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.DropWeapon))
        {
            Event += 4194304;
            DropWeaponInfo DropWpn = DropWeapon.ReadInfo(Action, C, GenLog: false);
            if (!Room.BotMode)
            {
                Room.DropCounter++;
                if (Room.DropCounter > ConfigLoader.MaxDropWpnCount)
                {
                    Room.DropCounter = 0;
                }
                if (Player != null && Player.Equip != null)
                {
                    int Static = ComDiv.GetIdStatics(DropWpn.WeaponId, 1);
                    if (Static == 1)
                    {
                        Player.Equip.WpnPrimary = 0;
                    }
                    if (Static == 2)
                    {
                        Player.Equip.WpnSecondary = 0;
                    }
                }
            }
            DropWeapon.WriteInfo(S, DropWpn, Room.DropCounter);
            DropWpn = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.GetWeaponForClient))
        {
            Event += 16777216;
            WeaponClient GetWpn = GetWeaponForClient.ReadInfo(Action, C, GenLog: false);
            if (!Room.BotMode && Player != null && Player.Equip != null)
            {
                int Static2 = ComDiv.GetIdStatics(GetWpn.WeaponId, 1);
                if (Static2 == 1)
                {
                    Player.Equip.WpnPrimary = GetWpn.WeaponId;
                }
                if (Static2 == 2)
                {
                    Player.Equip.WpnSecondary = GetWpn.WeaponId;
                }
            }
            GetWeaponForClient.WriteInfo(S, GetWpn);
            GetWpn = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.FireData))
        {
            Event += 33554432;
            FireDataInfo Info12 = FireData.ReadInfo(Action, C, GenLog: false);
            FireData.WriteInfo(S, Info12);
            Info12 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.CharaFireNHitData))
        {
            Event += 1024;
            CharaFireNHitDataInfo Info13 = CharaFireNHitData.ReadInfo(Action, C, GenLog: false);
            CharaFireNHitData.WriteInfo(S, Info13);
            Info13 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.HitData))
        {
            Event += 131072;
            List<HitDataInfo> Hits2 = HitData.ReadInfo(Action, C, genLog: false);
            List<DeathServerData> Deaths2 = new List<DeathServerData>();
            int BasicInfo2 = 0;
            if (Player != null)
            {
                for (int k = 0; k < Hits2.Count; k++)
                {
                    HitDataInfo Hit2 = Hits2[k];
                    if (Hit2.HitEnum == Server.Match.Data.Enums.HitType.HelmetProtection || Hit2.HitEnum == Server.Match.Data.Enums.HitType.HeadshotProtection)
                    {
                        continue;
                    }
                    int RawDamage = AllUtils.GetHitDamageNormal(Hit2.HitIndex);
                    if (AllUtils.ValidateHitData(RawDamage, Hit2, out var Damage2))
                    {
                        int ObjId = Hit2.ObjectId;
                        CharaHitPart HitPart2 = AllUtils.GetHitPart(Hit2.HitIndex);
                        CharaDeath DeathType2 = CharaDeath.DEFAULT;
                        BasicInfo2 = Hit2.WeaponId;
                        ObjectType ObjHitType = AllUtils.GetHitType(Hit2.HitIndex);
                        switch (ObjHitType)
                        {
                            case ObjectType.Object:
                                {
                                    ObjectInfo Obj4 = Room.GetObject(ObjId);
                                    ObjectModel ObjM = Obj4?.Model;
                                    if (ObjM != null && ObjM.Destroyable)
                                    {
                                        if (Obj4.Life > 0)
                                        {
                                            Obj4.Life -= Damage2;
                                            if (Obj4.Life <= 0)
                                            {
                                                Obj4.Life = 0;
                                                DamageManager.BoomDeath(Room, Player, BasicInfo2, Deaths2, Objs, Hit2.BoomPlayers);
                                            }
                                            Obj4.DestroyState = ObjM.CheckDestroyState(Obj4.Life);
                                            DamageManager.SabotageDestroy(Room, Player, ObjM, Obj4, Damage2);
                                            float SyncingTime = AllUtils.GetDuration(Obj4.UseDate);
                                            if (Obj4.Animation != null && Obj4.Animation.Duration > 0f && SyncingTime >= Obj4.Animation.Duration)
                                            {
                                                Obj4.Model.GetAnim(Obj4.Animation.NextAnim, SyncingTime, Obj4.Animation.Duration, Obj4);
                                            }
                                            ObjectHitInfo ObjH2 = new ObjectHitInfo(ObjM.UpdateId)
                                            {
                                                ObjId = Obj4.Id,
                                                ObjLife = Obj4.Life,
                                                KillerId = Action.Slot,
                                                ObjSyncId = (ObjM.NeedSync ? 1 : 0),
                                                SpecialUse = SyncingTime,
                                                AnimId1 = ObjM.Animation,
                                                AnimId2 = ((Obj4.Animation != null) ? Obj4.Animation.Id : 255),
                                                DestroyState = Obj4.DestroyState
                                            };
                                            Objs.Add(ObjH2);
                                        }
                                    }
                                    else if (ConfigLoader.SendFailMsg && ObjM == null)
                                    {
                                        CLogger.Print($"Fire Obj: {ObjId} Map: {Room.MapId} Invalid Object.", LoggerType.Warning);
                                        Player.LogPlayerPos(Hit2.EndBullet);
                                    }
                                    break;
                                }
                            case ObjectType.User:
                                {
                                    if (Room.GetPlayer(ObjId, out var User) && Player.RespawnIsValid() && !Player.Dead && !User.Dead && !User.Immortal)
                                    {
                                        if (HitPart2 == CharaHitPart.HEAD)
                                        {
                                            DeathType2 = CharaDeath.HEADSHOT;
                                        }
                                        if (Room.RoomType == RoomCondition.DeathMatch && Room.Rule == MapRules.HeadHunter && DeathType2 != CharaDeath.HEADSHOT)
                                        {
                                            Damage2 = 1;
                                        }
                                        else if (Room.RoomType == RoomCondition.Boss && DeathType2 == CharaDeath.HEADSHOT)
                                        {
                                            if ((Room.LastRound == 1 && User.Team == 0) || (Room.LastRound == 2 && User.Team == 1))
                                            {
                                                Damage2 /= 10;
                                            }
                                        }
                                        else if (Room.RoomType == RoomCondition.DeathMatch && Room.Rule == MapRules.Chaos)
                                        {
                                            Damage2 = 200;
                                        }
                                        if (ConfigLoader.UseHitMarker)
                                        {
                                            SendMatchInfo.SendHitMarkerSync(Room, Player, DeathType2, Hit2.HitEnum, Damage2);
                                        }
                                        DamageManager.SimpleDeath(Room, Deaths2, Objs, Player, User, Damage2, BasicInfo2, HitPart2, DeathType2);
                                    }
                                    else
                                    {
                                        AllUtils.RemoveHit(Hits2, k--);
                                    }
                                    break;
                                }
                            case ObjectType.UserObject:
                                {
                                    int OwnerSlot = ObjId >> 4;
                                    int GrenadeMapId = ObjId & 0xF;
                                    break;
                                }
                            default:
                                CLogger.Print($"HitType: ({ObjHitType}/{(int)ObjHitType}) Slot: {Action.Slot}", LoggerType.Warning);
                                CLogger.Print($"BoomPlayers: {Hit2.BoomInfo} {Hit2.BoomPlayers.Count}", LoggerType.Warning);
                                break;
                        }
                    }
                    else
                    {
                        AllUtils.RemoveHit(Hits2, k--);
                    }
                }
                if (Deaths2.Count > 0)
                {
                    SendMatchInfo.SendDeathSync(Room, Player, 255, BasicInfo2, Deaths2);
                }
            }
            else
            {
                Hits2 = new List<HitDataInfo>();
            }
            HitData.WriteInfo(S, Hits2);
            Deaths2 = null;
            Hits2 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.GrenadeHit))
        {
            Event += 268435456;
            List<GrenadeHitInfo> Hits3 = GrenadeHit.ReadInfo(Action, C, GenLog: false);
            List<DeathServerData> Deaths3 = new List<DeathServerData>();
            int BasicInfo3 = 0;
            if (Player != null)
            {
                int PlayerSlot = -1;
                for (int l = 0; l < Hits3.Count; l++)
                {
                    GrenadeHitInfo Hit3 = Hits3[l];
                    int RawDamage2 = AllUtils.GetHitDamageNormal(Hit3.HitInfo);
                    if (AllUtils.ValidateGrenadeHit(RawDamage2, Hit3, out var Damage3))
                    {
                        int ObjId2 = Hit3.ObjectId;
                        CharaHitPart HitPart3 = AllUtils.GetHitPart(Hit3.HitInfo);
                        BasicInfo3 = Hit3.WeaponId;
                        ObjectType HitType = AllUtils.GetHitType(Hit3.HitInfo);
                        switch (HitType)
                        {
                            case ObjectType.Object:
                                {
                                    ObjectInfo Obj6 = Room.GetObject(ObjId2);
                                    ObjectModel ObjM2 = Obj6?.Model;
                                    if (ObjM2 != null && ObjM2.Destroyable && Obj6.Life > 0)
                                    {
                                        Obj6.Life -= Damage3;
                                        if (Obj6.Life <= 0)
                                        {
                                            Obj6.Life = 0;
                                        }
                                        Obj6.DestroyState = ObjM2.CheckDestroyState(Obj6.Life);
                                        DamageManager.SabotageDestroy(Room, Player, ObjM2, Obj6, Damage3);
                                        if (Damage3 > 0)
                                        {
                                            ObjectHitInfo ObjH3 = new ObjectHitInfo(ObjM2.UpdateId)
                                            {
                                                ObjId = Obj6.Id,
                                                ObjLife = Obj6.Life,
                                                KillerId = Action.Slot,
                                                ObjSyncId = (ObjM2.NeedSync ? 1 : 0),
                                                AnimId1 = ObjM2.Animation,
                                                AnimId2 = ((Obj6.Animation != null) ? Obj6.Animation.Id : 255),
                                                DestroyState = Obj6.DestroyState,
                                                SpecialUse = AllUtils.GetDuration(Obj6.UseDate)
                                            };
                                            Objs.Add(ObjH3);
                                        }
                                    }
                                    else if (ConfigLoader.SendFailMsg && ObjM2 == null)
                                    {
                                        CLogger.Print($"Boom Obj: {ObjId2} Map: {Room.MapId} Invalid Object.", LoggerType.Warning);
                                        Player.LogPlayerPos(Hit3.HitPos);
                                    }
                                    break;
                                }
                            case ObjectType.User:
                                {
                                    PlayerSlot++;
                                    if (Damage3 > 0 && Room.GetPlayer(ObjId2, out var User2) && Player.RespawnIsValid() && !User2.Dead && !User2.Immortal)
                                    {
                                        if (Hit3.DeathType == CharaDeath.MEDICAL_KIT)
                                        {
                                            User2.Life += Damage3;
                                            User2.CheckLifeValue();
                                        }
                                        else if (Hit3.DeathType == CharaDeath.BOOM && ClassType.Dino != Hit3.WeaponClass && PlayerSlot % 2 == 0)
                                        {
                                            int Value = Damage3;
                                            Damage3 = (int)Math.Ceiling((double)Damage3 / 2.7);
                                            User2.Life -= Damage3;
                                            AllUtils.BaseAssistLogic(Room, Player, User2, Damage3);
                                            if (User2.Life <= 0)
                                            {
                                                DamageManager.SetDeath(Deaths3, User2, Player, Hit3.DeathType);
                                            }
                                            else
                                            {
                                                DamageManager.SetHitEffect(Objs, User2, Player, Hit3.DeathType, HitPart3);
                                            }
                                        }
                                        else
                                        {
                                            User2.Life -= Damage3;
                                            AllUtils.BaseAssistLogic(Room, Player, User2, Damage3);
                                            if (User2.Life <= 0)
                                            {
                                                DamageManager.SetDeath(Deaths3, User2, Player, Hit3.DeathType);
                                            }
                                            else
                                            {
                                                DamageManager.SetHitEffect(Objs, User2, Player, Hit3.DeathType, HitPart3);
                                            }
                                        }
                                        if (Damage3 > 0)
                                        {
                                            if (ConfigLoader.UseHitMarker)
                                            {
                                                SendMatchInfo.SendHitMarkerSync(Room, Player, Hit3.DeathType, Hit3.HitEnum, Damage3);
                                            }
                                            ObjectHitInfo Obj5 = new ObjectHitInfo(2)
                                            {
                                                ObjId = User2.Slot,
                                                ObjLife = User2.Life,
                                                DeathType = Hit3.DeathType,
                                                WeaponId = BasicInfo3,
                                                HitPart = HitPart3
                                            };
                                            Objs.Add(Obj5);
                                        }
                                    }
                                    else
                                    {
                                        AllUtils.RemoveHit(Hits3, l--);
                                    }
                                    break;
                                }
                            case ObjectType.UserObject:
                                {
                                    int OwnerSlot2 = ObjId2 >> 4;
                                    int GrenadeMapId2 = ObjId2 & 0xF;
                                    break;
                                }
                            default:
                                CLogger.Print($"Grenade Boom, HitType: ({HitType}/{(int)HitType})", LoggerType.Warning);
                                break;
                        }
                    }
                    else
                    {
                        AllUtils.RemoveHit(Hits3, l--);
                    }
                }
                if (Deaths3.Count > 0)
                {
                    SendMatchInfo.SendDeathSync(Room, Player, 255, BasicInfo3, Deaths3);
                }
            }
            else
            {
                Hits3 = new List<GrenadeHitInfo>();
            }
            GrenadeHit.WriteInfo(S, Hits3);
            Deaths3 = null;
            Hits3 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.GetWeaponForHost))
        {
            Event += 512;
            WeaponHost Info14 = GetWeaponForHost.ReadInfo(Action, C, GenLog: false);
            GetWeaponForHost.WriteInfo(S, Info14);
            Info14 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.FireDataOnObject))
        {
            Event += 1073741824;
            FireDataObjectInfo Info15 = FireDataOnObject.ReadInfo(Action, C, GenLog: false);
            FireDataOnObject.WriteInfo(S, Info15);
            Info15 = null;
        }
        if (Action.Flag.HasFlag(UdpGameEvent.FireNHitDataOnObject))
        {
            Event += 8192;
            FireNHitDataObjectInfo Info16 = FireNHitDataOnObject.ReadInfo(Action, C, GenLog: false);
            if (Player != null && !Player.Dead)
            {
                SendMatchInfo.SendPortalPass(Room, Player, Info16.Portal);
            }
            FireNHitDataOnObject.WriteInfo(S, Info16);
            Info16 = null;
        }
        EventsData = S.ToArray();
        if (Event != (uint)Action.Flag)
        {
            CLogger.Print(Bitwise.ToHexData($"UDP EVENT FLAGS: '{(uint)Action.Flag}' | '{(uint)(Action.Flag - Event)}'", Data), LoggerType.Opcode);
        }
        return Objs;
    }

    public byte[] WritePlayerActionData(byte[] Data, RoomModel Room, float Time, PacketModel Packet)
    {
        SyncClientPacket C = new SyncClientPacket(Data);
        List<ObjectHitInfo> Objs = new List<ObjectHitInfo>();
        SyncServerPacket S = new SyncServerPacket();
        for (int i = 0; i < 16; i++)
        {
            ActionModel Action = new ActionModel();
            try
            {
                Action.Length = C.ReadUH(out var Exception);
                if (Exception)
                {
                    break;
                }
                Action.Slot = C.ReadUH();
                Action.SubHead = (UdpSubHead)C.ReadC();
                if (Action.SubHead != (UdpSubHead)255)
                {
                    S.WriteH(Action.Length);
                    S.WriteH(Action.Slot);
                    S.WriteC((byte)Action.SubHead);
                    if (Action.SubHead == UdpSubHead.Grenade)
                    {
                        GrenadeInfo Info = GrenadeSync.ReadInfo(C, GenLog: false);
                        GrenadeSync.WriteInfo(S, Info);
                        Info = null;
                        continue;
                    }
                    if (Action.SubHead == UdpSubHead.DroppedWeapon)
                    {
                        DropedWeaponInfo Info2 = DropedWeapon.ReadInfo(C, GenLog: false);
                        DropedWeapon.WriteInfo(S, Info2);
                        Info2 = null;
                        continue;
                    }
                    if (Action.SubHead == UdpSubHead.ObjectStatic)
                    {
                        ObjectStaticInfo Info3 = ObjectStatic.ReadInfo(C, GenLog: false);
                        ObjectStatic.WriteInfo(S, Info3);
                        Info3 = null;
                        continue;
                    }
                    if (Action.SubHead == UdpSubHead.ObjectAnim)
                    {
                        ObjectAnimInfo Info4 = ObjectAnim.ReadInfo(C, GenLog: false);
                        ObjectAnim.WriteInfo(S, Info4);
                        Info4 = null;
                        continue;
                    }
                    if (Action.SubHead == UdpSubHead.StageInfoObjectStatic)
                    {
                        StageStaticInfo Info5 = StageInfoObjStatic.ReadInfo(C, GenLog: false);
                        StageInfoObjStatic.WriteInfo(S, Info5);
                        Info5 = null;
                        continue;
                    }
                    if (Action.SubHead == UdpSubHead.StageInfoObjectAnim)
                    {
                        StageAnimInfo Info6 = StageInfoObjAnim.ReadInfo(C, GenLog: false);
                        StageInfoObjAnim.WriteInfo(S, Info6);
                        Info6 = null;
                        continue;
                    }
                    if (Action.SubHead == UdpSubHead.StageInfoObjectControl)
                    {
                        StageControlInfo Info7 = StageInfoObjControl.ReadInfo(C, GenLog: false);
                        StageInfoObjControl.WriteInfo(S, Info7);
                        Info7 = null;
                        continue;
                    }
                    if (Action.SubHead != UdpSubHead.User && Action.SubHead != UdpSubHead.StageInfoChara)
                    {
                        CLogger.Print(Bitwise.ToHexData($"UDP SUB HEAD: '{Action.SubHead}' or '{(int)Action.SubHead}'", Data), LoggerType.Opcode);
                        continue;
                    }
                    Action.Flag = (UdpGameEvent)C.ReadUD();
                    Action.Data = C.ReadB(Action.Length - 9);
                    AllUtils.CheckDataFlags(Action, Packet);
                    Objs.AddRange(GetPlayerActionData(Action, Room, Time, out var Result));
                    S.GoBack(5);
                    S.WriteH((ushort)(Result.Length + 9));
                    S.WriteH(Action.Slot);
                    S.WriteC((byte)Action.SubHead);
                    S.WriteD((uint)Action.Flag);
                    S.WriteB(Result);
                    if (Action.Data.Length != 0 || Action.Length - 9 == 0)
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print($"UDP WriteActionBytes - Buffer (Length: {Data.Length}): | {ex.Message}", LoggerType.Error, ex);
                Objs = new List<ObjectHitInfo>();
            }
            break;
        }
        if (Objs.Count > 0)
        {
            S.WriteB(PROTOCOL_EVENTS_ACTION.GET_CODE(Objs));
        }
        Objs = null;
        return S.ToArray();
    }

    private void SendPacket(byte[] Data, IPEndPoint Address)
    {
        MainSocket.SendTo(Data, 0, Data.Length, SocketFlags.None, Address);
    }
}
