using System;
using System.Collections.Generic;
using Plugin.Core.Enums;
using Plugin.Core.Managers;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Server.Game;
using Server.Game.Data.Commands;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network.ServerPacket;

public class ValuesCommand : ICommand
{
    public string Command => "player";

    public string Description => "modify value of player";

    public string Permission => "gamemastercommand";

    public string Args => "%options1% $options2% %value% %uid%";

    public string Execute(string Command, string[] Args, Account Player)
    {
        string Options = Args[0].ToLower();
        string Options2 = Args[1].ToLower();
        string Result = "";
        int Value = int.Parse(Args[2]);
        long UID;
        bool IsUIDONN = long.TryParse(Args[3], out UID);
        if (Options.Equals("gift"))
        {
            Account PT = (IsUIDONN ? AccountManager.GetAccount(UID, 0) : AccountManager.GetAccount(Args[3], 1, 0));
            switch (Options2)
            {
                case "gold":
                    if (PT == null)
                    {
                        return "Player with " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " doesn't Exist!";
                    }
                    if (DaoManagerSQL.UpdateAccountGold(PT.PlayerId, PT.Gold + Value))
                    {
                        PT.Gold += Value;
                        PT.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0u, PT));
                        SendItemInfo.LoadGoldCash(PT);
                        Result = $"{Value} Amount Of {ComDiv.ToTitleCase(Options2)} To UID: {PT.PlayerId} ({PT.Nickname})";
                    }
                    break;
                case "cash":
                    if (PT == null)
                    {
                        return "Player with " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " doesn't Exist!";
                    }
                    if (DaoManagerSQL.UpdateAccountCash(PT.PlayerId, PT.Cash + Value))
                    {
                        PT.Cash += Value;
                        PT.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0u, PT));
                        SendItemInfo.LoadGoldCash(PT);
                        Result = $"{Value} Amount Of {ComDiv.ToTitleCase(Options2)} To UID: {PT.PlayerId} ({PT.Nickname})";
                    }
                    break;
                case "item":
                    {
                        if (PT == null)
                        {
                            return "Player with " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " doesn't Exist!";
                        }
                        GoodsItem Good = ShopManager.GetGood(Value);
                        if (Good == null)
                        {
                            return $"Goods Id: {Value} not founded!";
                        }
                        List<ItemsModel> Items = new List<ItemsModel>();
                        ItemsModel Item = new ItemsModel(Good.Item);
                        if (Item == null)
                        {
                            return $"Goods Id: {Value} was error!";
                        }
                        Items.Add(Item);
                        if (Items.Count > 0)
                        {
                            PT.SendPacket(new PROTOCOL_BASE_NEW_REWARD_POPUP_ACK(Items));
                        }
                        PT.SendPacket(new PROTOCOL_INVENTORY_GET_INFO_ACK(1, PT, Item));
                        Result = $"{Item.Name} To UID: {PT.PlayerId} ({PT.Nickname})";
                        break;
                    }
            }
        }
        else if (Options.Equals("kick"))
        {
            string text = Options2;
            string text2 = text;
            if (!(text2 == "uid"))
            {
                if (text2 == "nick")
                {
                    Account PT2 = AccountManager.GetAccount(Args[3], 1, 0);
                    if (PT2 == null)
                    {
                        return "Player with Nickname: " + Args[3] + " doesn't Exist!";
                    }
                    if (PT2.Nickname == Player.Nickname)
                    {
                        return "Player by Nickname: " + Args[3] + " failed! (Can't Kick Yourself)";
                    }
                    if (PT2.Access > Player.Access)
                    {
                        return "Player by Nickname: " + Args[3] + " failed! (Can't Kick Higher Access Level Than Yours)";
                    }
                    PT2.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), OnlyInServer: false);
                    PT2.Close(TimeSpan.FromSeconds(Value).Milliseconds, kicked: true);
                    Result = $"Player by Nickname: {Args[3]} Executed in {Value} Seconds!";
                }
            }
            else
            {
                Account PT3 = AccountManager.GetAccount(UID, 0);
                if (PT3 == null)
                {
                    return $"Player with UID: {UID} doesn't Exist!";
                }
                if (PT3.PlayerId == Player.PlayerId)
                {
                    return $"Player by UID: {UID} failed! (Can't Kick Yourself)";
                }
                if (PT3.Access > Player.Access)
                {
                    return $"Player by UID: {UID} failed! (Can't Kick Higher Access Level Than Yours)";
                }
                PT3.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), OnlyInServer: false);
                PT3.Close(TimeSpan.FromSeconds(Value).Milliseconds, kicked: true);
                Result = $"Player by UID: {UID} Executed in {Value} Seconds!";
            }
        }
        else if (Options.Equals("ban"))
        {
            Account PT4 = (IsUIDONN ? AccountManager.GetAccount(UID, 0) : AccountManager.GetAccount(Args[3], 1, 0));
            string text3 = Options2;
            string text4 = text3;
            if (!(text4 == "normal"))
            {
                if (text4 == "permanent")
                {
                    if (PT4 == null)
                    {
                        return "Player with " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " doesn't Exist!";
                    }
                    if (PT4.PlayerId == Player.PlayerId)
                    {
                        return "Player by " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " failed! (Can't Kick Yourself)";
                    }
                    if (PT4.Access > Player.Access)
                    {
                        return "Player by " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " failed! (Can't Kick Higher Access Level Than Yours)";
                    }
                    AccessLevel Banned = AccessLevel.BANNED;
                    if (ComDiv.UpdateDB("accounts", "access_level", -1, "player_id", PT4.PlayerId))
                    {
                        string Message = "Player '" + PT4.Nickname + "' has been banned for '10' Years (PERMANENTLY)!";
                        using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK Packet = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(Message))
                        {
                            GameXender.Client.SendPacketToAllClients(Packet);
                        }
                        PT4.Access = Banned;
                        PT4.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), OnlyInServer: false);
                        PT4.Close(1000, kicked: true);
                        Result = (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " Success! (Permanent)";
                    }
                }
            }
            else
            {
                if (PT4 == null)
                {
                    return "Player with " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " doesn't Exist!";
                }
                if (PT4.PlayerId == Player.PlayerId)
                {
                    return "Player by " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " failed! (Can't Kick Yourself)";
                }
                if (PT4.Access > Player.Access)
                {
                    return "Player by " + (IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3])) + " failed! (Can't Kick Higher Access Level Than Yours)";
                }
                double Days = Convert.ToDouble(Value);
                BanHistory Ban = DaoManagerSQL.SaveBanHistory(Player.PlayerId, "DURATION", $"{PT4.PlayerId}", DateTimeUtil.Now().AddDays(Days));
                if (Ban != null)
                {
                    string Message2 = $"Player '{PT4.Nickname}' has been banned for {Days} Day(s)!";
                    using (PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK Packet2 = new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(Message2))
                    {
                        GameXender.Client.SendPacketToAllClients(Packet2);
                    }
                    PT4.BanObjectId = Ban.ObjectId;
                    PT4.SendPacket(new PROTOCOL_AUTH_ACCOUNT_KICK_ACK(2), OnlyInServer: false);
                    PT4.Close(1000, kicked: true);
                    Result = string.Format("{0} Success for {1} Day(s)", IsUIDONN ? $"UID: {UID}" : ("Nickname: " + Args[3]), Days);
                }
            }
        }
        return ComDiv.ToTitleCase(Options) + " " + Result;
    }
}
