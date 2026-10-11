// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Network.ClientPacket.PROTOCOL_CHAR_CHANGE_EQUIP_REQ
using System;
using System.Collections.Generic;
using System.Linq;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.XML;
using Server.Game;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;
using Server.Game.Network;
using Server.Game.Network.ServerPacket;

public class PROTOCOL_CHAR_CHANGE_EQUIP_REQ : GameClientPacket
{
    private readonly int[] EquipmentList = new int[14];

    private int AccessoryId;

    private int CharaPos;

    private int FR_SLOT;

    private int CT_SLOT;

    private bool AccesoryChange;

    private bool EquipmentChange;

    private bool ItemChange;

    private readonly SortedList<int, int> Items = new SortedList<int, int>();

    private int ItemEnable;

    private int ItemDisable;

    public PROTOCOL_CHAR_CHANGE_EQUIP_REQ(GameClient Client, byte[] Buffer)
    {
        Makeme(Client, Buffer);
    }

    public override void Read()
    {
        Account Player = Client.Player;
        AccessoryId = ReadD();
        ReadUD();
        AccesoryChange = ReadC() == 1;
        ItemDisable = ReadC();
        for (int index = 0; index < ItemDisable; index++)
        {
            ReadAndUpdateItemDisable(Player);
        }
        ReadC();
        ReadC();
        ItemEnable = ReadC();
        for (int i = 0; i < ItemEnable; i++)
        {
            ReadAndUpdateItemEnable(Player);
        }
        ReadC();
        ReadC();
        ReadC();
        for (int j = 0; j < 5; j++)
        {
            EquipmentList[j] = ReadD();
            ReadUD();
        }
        CharaPos = ReadD();
        ReadUD();
        for (int k = 5; k < 14; k++)
        {
            EquipmentList[k] = ReadD();
            ReadUD();
        }
        EquipmentChange = ReadC() == 1;
        int ItemCount = ReadC();
        for (int l = 0; l < ItemCount; l++)
        {
            int ItemId = ReadD();
            ReadUD();
            Items.Add(l, ItemId);
        }
        ItemChange = ReadC() == 1;
        ReadC();
        FR_SLOT = ReadC();
        CT_SLOT = ReadC();
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
            int CharacterCount = Player.Character.Characters.Count;
            if (CharacterCount > 0)
            {
                if (AccesoryChange)
                {
                    AllUtils.ValidateAccesoryEquipment(Player, AccessoryId);
                }
                if (EquipmentChange)
                {
                    AllUtils.ValidateCharacterEquipment(Player, Player.Equipment, EquipmentList, CharaPos, FR_SLOT, CT_SLOT);
                }
                if (ItemChange)
                {
                    AllUtils.ValidateItemEquipment(Player, Items);
                }
                AllUtils.ValidateCharacterSlot(Player, Player.Equipment, FR_SLOT, CT_SLOT);
            }
            RoomModel Room = Player.Room;
            if (Room != null)
            {
                AllUtils.UpdateSlotEquips(Player, Room);
            }
            Client.SendPacket(new PROTOCOL_CHAR_CHANGE_EQUIP_ACK(0u));
        }
        catch (Exception ex)
        {
            CLogger.Print(ex.Message, LoggerType.Error, ex);
        }
    }

    private void ReadAndUpdateItemDisable(Account Player)
    {
        int CouponId = ReadD();
        ItemsModel itemsModel = Player.Inventory.Items.FirstOrDefault((ItemsModel x) => x.Id == CouponId);
        if (itemsModel != null)
        {
            CouponFlag couponEffect = CouponEffectManager.getCouponEffect(itemsModel.Id);
            if (couponEffect != null && couponEffect.EffectFlag > (CouponEffects)0 && Player.Effects.HasFlag(couponEffect.EffectFlag))
            {
                Player.Effects -= (int)couponEffect.EffectFlag;
                DaoManagerSQL.UpdateCouponEffect(Player.PlayerId, Player.Effects);
            }
        }
    }

    private void ReadAndUpdateItemEnable(Account Player)
    {
        int CouponId = ReadD();
        ItemsModel itemsModel = Player.Inventory.Items.FirstOrDefault((ItemsModel x) => x.Id == CouponId);
        if (itemsModel != null)
        {
            bool flag = Player.Bonus.AddBonuses(itemsModel.Id);
            CouponFlag couponEffect = CouponEffectManager.getCouponEffect(itemsModel.Id);
            if (couponEffect != null && couponEffect.EffectFlag > (CouponEffects)0 && !Player.Effects.HasFlag(couponEffect.EffectFlag))
            {
                Player.Effects |= couponEffect.EffectFlag;
                DaoManagerSQL.UpdateCouponEffect(Player.PlayerId, Player.Effects);
            }
            if (flag)
            {
                DaoManagerSQL.UpdatePlayerBonus(Player.PlayerId, Player.Bonus.Bonuses, Player.Bonus.FreePass);
            }
        }
    }
}
