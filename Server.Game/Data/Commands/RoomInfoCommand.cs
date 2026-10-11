// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Data.Commands.RoomInfoCommand
using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Server.Game.Data.Commands;
using Server.Game.Data.Models;
using Server.Game.Data.Utils;

public class RoomInfoCommand : ICommand
{
    public string Command => "roominfo";

    public string Description => "Alterar opções de quarto";

    public string Permission => "helpcommand";

    public string Args => "%options% %value%";

    public string Execute(string Command, string[] Args, Account Player)
    {
        RoomModel Room = Player.Room;
        if (Room == null)
        {
            return "É necessária uma sala para executar o comando.";
        }
        string Options = Args[0].ToLower();
        string Value = Args[1].ToLower();
        string Result = "";
        if (Options.Equals("time"))
        {
            int KillTime = int.Parse(Value) * 6;
            if (KillTime < 0)
            {
                return "Ops! O mapa 'index' não deveria ser: " + Value + ". Tente um valor mais alto.";
            }
            if (Room.IsPreparing() || AllUtils.PlayerIsBattle(Player))
            {
                return "Ops! Você não pode alterar o \"horário\" enquanto a sala já iniciou a partida.";
            }
            Room.KillTime = KillTime;
            Room.UpdateRoomInfo();
            Result = $"{Room.GetTimeByMask() % 60} Minutes";
        }
        else if (Options.Equals("flags"))
        {
            RoomWeaponsFlag WeaponsFlag = (Room.WeaponsFlag = (RoomWeaponsFlag)int.Parse(Value));
            Room.UpdateRoomInfo();
            Result = $"{WeaponsFlag}";
        }
        return ComDiv.ToTitleCase(Options) + " Changed To " + Result + ".";
    }
}
