using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Managers.Events;
using Plugin.Core.Models;
using Plugin.Core.SQL;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Game.Data.Commands;
using Server.Game.Data.Models;
using Server.Game.Data.Sync.Server;
using Server.Game.Network.ServerPacket;
using System;
using System.Linq;

public class SystemCommand : ICommand
{
    public string Command => "sys";

    public string Description => "change server settings";

    public string Permission => "helpcommand";

    public string Args => "%options% %value%";

    //public string Execute2(Account Player)
    //{
    //    RoomModel Room = Player.Room;
    //    int HargaBattlepassPremium = 100000;
    //    if (Player.Cash < HargaBattlepassPremium)
    //    {
    //        return "Your cash is not enough to buy battlepass premium.";
    //    }
    //    Player.Cash -= HargaBattlepassPremium;
    //    DaoManagerSQL.UpdateBattlepassPremium(Player.PlayerId, 1);
    //    Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(0u));
    //    Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Player));
    //    Player.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0u, Player));
    //    SendItemInfo.LoadGoldCash(Player);
    //    return "Success buy premium battlepass.";
    //}

    public string Execute2(Account Player)
    {
        RoomModel Room = Player.Room;
        int HargaBattlepassPremium = 100000; // price in cash

        if (Player.Cash < HargaBattlepassPremium)
        {
            return "Your cash is not enough to buy battlepass premium.";
        }

        // Deduct price
        Player.Cash -= HargaBattlepassPremium;

        // Update DB field battlepass_premium = 1
        DaoManagerSQL.UpdateBattlepassPremium(Player.PlayerId, 1);

        // Get season dates from CardData
        var season = CardData.LoadSeason().FirstOrDefault();
        int remainDays = 0;

        if (season != null && season.EndDate > 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            remainDays = (int)((season.EndDate - now) / 86400); // seconds → days
            if (remainDays < 0) remainDays = 0;
        }

        // Send success response
        Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_BUY_SEASON_PASS_ACK(
            error: 0,                  // success
            passType: 1,               // 1 = premium pass
            price: HargaBattlepassPremium,
            currency: 1,               // 1 = cash (0 = gold, if needed)
            remainDays: remainDays
        ));

        Player.SendPacket(new PROTOCOL_SEASON_CHALLENGE_INFO_ACK(Player));
        Player.SendPacket(new PROTOCOL_AUTH_GET_POINT_CASH_ACK(0u, Player));
        SendItemInfo.LoadGoldCash(Player);

        return "Success buy premium battlepass.";
    }



    public string Execute3(Account Player)
    {
        if (Player.Title.OwnerId == 0L)
        {
            DaoManagerSQL.CreatePlayerTitlesDB(Player.PlayerId);
            Player.Title = new PlayerTitles()
            {
                OwnerId = Player.PlayerId
            };
        }
        PlayerTitles titles = Player.Title;
        int num = 0;
        for (int titleId = 1; titleId <= 44; ++titleId)
        {
            TitleModel title = TitleSystemXML.GetTitle(titleId);
            if (title != null && !titles.Contains(title.Flag))
            {
                ++num;
                titles.Add(title.Flag);
                if (titles.Slots < title.Slot)
                    titles.Slots = title.Slot;
            }
        }
        if (num <= 0)
            return "Você já liberou seus titulos";
        ComDiv.UpdateDB("player_titles", "slots", titles.Slots, "owner_id", Player.PlayerId);
        DaoManagerSQL.UpdatePlayerTitlesFlags(Player.PlayerId, titles.Flags);
        Player.SendPacket(new PROTOCOL_BASE_USER_TITLE_INFO_ACK(Player));
        return "Titulos liberado [Relogue]";
    }

    public string Execute(string Command, string[] Args, Account Player)
    {
        string Options = Args[0].ToLower();
        string Value = Args[1].ToLower();
        string Result = "";
        if (Options.Equals("udp"))
        {
            int CurrentUDP = (int)ConfigLoader.UdpType;
            int ChangeValue = int.Parse(Value);
            if (ChangeValue.Equals(CurrentUDP))
            {
                return $"UDP State Already Changed To: {CurrentUDP}";
            }
            if (ChangeValue < 1 || ChangeValue > 4)
            {
                return $"Cannot Change UDP State To: {ChangeValue}";
            }
            switch (ChangeValue)
            {
                case 1:
                    ConfigLoader.UdpType = (UdpState)ChangeValue;
                    Result = $"State Changed ({ChangeValue} - {ConfigLoader.UdpType})";
                    break;
                case 2:
                    ConfigLoader.UdpType = (UdpState)ChangeValue;
                    Result = $"State Changed ({ChangeValue} - {ConfigLoader.UdpType})";
                    break;
                case 3:
                    ConfigLoader.UdpType = (UdpState)ChangeValue;
                    Result = $"State Changed ({ChangeValue} - {ConfigLoader.UdpType})";
                    break;
                case 4:
                    ConfigLoader.UdpType = (UdpState)ChangeValue;
                    Result = $"State Changed ({ChangeValue} - {ConfigLoader.UdpType})";
                    break;
                default:
                    ConfigLoader.UdpType = UdpState.RELAY;
                    Result = $"State Changed (3 - {ConfigLoader.UdpType}). Wrong Value";
                    break;
            }
        }
        else if (Options.Equals("debug"))
        {
            bool ChangeValue2 = int.Parse(Value).Equals(1);
            if (ChangeValue2.Equals(ConfigLoader.DebugMode))
            {
                return $"Debug Mode Already Changed To: {ChangeValue2}";
            }
            if (ChangeValue2)
            {
                ConfigLoader.DebugMode = ChangeValue2;
                Result = "Mode '" + (ChangeValue2 ? "Enabled" : "Disabled") + "'";
            }
            else
            {
                ConfigLoader.DebugMode = ChangeValue2;
                Result = "Mode '" + (ChangeValue2 ? "Enabled" : "Disabled") + "'";
            }
        }
        else if (Options.Equals("buybattlepass"))
        {
            bool ChangeValue3 = int.Parse(Value).Equals(1);
            string V1 = Execute2(Player);
            Player.Connection.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK("Server", 0, 5, GMColor: false, V1));
        }
        else if (Options.Equals("test"))
        {
            bool ChangeValue4 = int.Parse(Value).Equals(1);
            if (ChangeValue4.Equals(ConfigLoader.IsTestMode))
            {
                return $"Test Mode Already Changed To: {ChangeValue4}";
            }
            if (ChangeValue4)
            {
                ConfigLoader.IsTestMode = ChangeValue4;
                Result = "Mode '" + (ChangeValue4 ? "Enabled" : "Disabled") + "'";
            }
            else
            {
                ConfigLoader.IsTestMode = ChangeValue4;
                Result = "Mode '" + (ChangeValue4 ? "Enabled" : "Disabled") + "'";
            }
        }
        else if (Options.Equals("ping"))
        {
            bool ChangeValue5 = int.Parse(Value).Equals(1);
            if (ChangeValue5.Equals(ConfigLoader.IsDebugPing))
            {
                return $"Ping Mode Already Changed To: {ChangeValue5}";
            }
            if (ChangeValue5)
            {
                ConfigLoader.IsDebugPing = ChangeValue5;
                Result = "Mode '" + (ChangeValue5 ? "Enabled" : "Disabled") + "'";
            }
            else
            {
                ConfigLoader.IsDebugPing = ChangeValue5;
                Result = "Mode '" + (ChangeValue5 ? "Enabled" : "Disabled") + "'";
            }
        }
        else if (Options.Equals("title"))
        {
            if (Value.Equals("all"))
            {
                if (Player.Title.OwnerId == 0)
                {
                    DaoManagerSQL.CreatePlayerTitlesDB(Player.PlayerId);
                    Player.Title = new PlayerTitles
                    {
                        OwnerId = Player.PlayerId
                    };
                }
                PlayerTitles Titles = Player.Title;
                int Count = 0;
                for (int i = 1; i <= 44; i++)
                {
                    TitleModel Title = TitleSystemXML.GetTitle(i);
                    if (Title != null && !Titles.Contains(Title.Flag))
                    {
                        Count++;
                        Titles.Add(Title.Flag);
                        if (Titles.Slots < Title.Slot)
                        {
                            Titles.Slots = Title.Slot;
                        }
                    }
                }
                if (Count > 0)
                {
                    ComDiv.UpdateDB("player_titles", "slots", Titles.Slots, "owner_id", Player.PlayerId);
                    DaoManagerSQL.UpdatePlayerTitlesFlags(Player.PlayerId, Titles.Flags);
                    Player.SendPacket(new PROTOCOL_BASE_USER_TITLE_INFO_ACK(Player));
                }
                Result = "Successfully Opened!";
            }
            else
            {
                Result = "Arguments was not valid!";
            }
        }
        return (Options.Equals("udp") ? "UDP" : ComDiv.ToTitleCase(Options)) + " " + Result;
    }
}
