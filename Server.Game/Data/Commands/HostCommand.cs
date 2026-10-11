// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Data.Commands.HostCommand
using Plugin.Core.Enums;
using Plugin.Core.JSON;
using Plugin.Core.Models;
using Plugin.Core.Utility;
using Server.Game.Data.Commands;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;

public class HostCommand : ICommand
{
    public string Command => "host";

    public string Description => "Alterar opções de quarto (somente AI)";

    public string Permission => "helpcommand";

    public string Args => "%options% %value%";

    public string Execute(string Command, string[] Args, Account Player)
    {
        if (Args.Length < 2)
        {
            Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("Uso: :host <wpn/time> <valor>"));
            return "Argumentos insuficientes";
        }
        RoomModel Room = Player.Room;
        if (Room == null)
        {
            Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("Você precisa estar em uma sala!"));
            return "É necessária uma sala para executar o comando.";
        }
        if (!Room.GetLeader(out var Host) && Host == Player)
        {
            Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("Apenas o líder da sala pode usar este comando!"));
            return "Comando válido somente para o host (líder da sala).";
        }
        if (!Room.IsBotMode())
        {
            Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("Este comando só funciona no modo Practice (Bot)!"));
            return "Comando válido somente para o modo Bot (Practice).";
        }
        string option = Args[0].ToLower();
        string value = Args[1].ToLower();
        string result = "";
        string text = option;
        string text2 = text;
        if (!(text2 == "wpn"))
        {
            if (!(text2 == "time"))
            {
                result = "Opção inválida. Use 'wpn' ou 'time'";
                Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(result));
                return result;
            }
            result = ProcessTimeChange(Room, value);
        }
        else
        {
            result = ProcessWeaponChange(Room, value);
        }
        Room.UpdateRoomInfo();
        Player.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK("Configuração alterada: " + result));
        return (option.Equals("wpn") ? "Weapon" : ComDiv.ToTitleCase(option)) + " alterado para " + result + ".";
    }

    private string ProcessWeaponChange(RoomModel room, string value)
    {
        CommandHelper helper = CommandHelperJSON.GetTag("WeaponsFlag");
        switch (value)
        {
            case "ar":
                room.WeaponsFlag = (RoomWeaponsFlag)helper.AssaultRifle;
                return " Rifle de Assalto (Somente)";
            case "smg":
                room.WeaponsFlag = (RoomWeaponsFlag)helper.SubMachineGun;
                return " Submetralhadora (Somente)";
            case "sr":
                room.WeaponsFlag = (RoomWeaponsFlag)helper.SniperRifle;
                return " Sniper Rifle (Somente)";
            case "sg":
                room.WeaponsFlag = (RoomWeaponsFlag)helper.ShotGun;
                return " Shotgun (Somente)";
            case "mg":
                room.WeaponsFlag = (RoomWeaponsFlag)helper.MachineGun;
                return " Metralhadora (Somente)";
            case "rpg":
                room.WeaponsFlag = (RoomWeaponsFlag)helper.RPG7;
                return " RPG-7 (Somente)";
            case "all":
                room.WeaponsFlag = (RoomWeaponsFlag)helper.AllWeapons;
                return " TODAS Armas (AR, SMG, SR, SG, MG)";
            default:
                room.WeaponsFlag = (RoomWeaponsFlag)helper.AllWeapons;
                return " Padrão (valor não encontrado)";
        }
    }

    private string ProcessTimeChange(RoomModel room, string value)
    {
        if (!int.TryParse(value, out var minutes))
        {
            return " Valor de tempo inválido!";
        }
        CommandHelper helper = CommandHelperJSON.GetTag("PlayTime");
        string result;
        switch (minutes)
        {
            case 5:
                room.KillTime = helper.Minutes05;
                result = "⏱ 5 Minutos";
                break;
            case 10:
                room.KillTime = helper.Minutes10;
                result = "⏱ 10 Minutos";
                break;
            case 15:
                room.KillTime = helper.Minutes15;
                result = "⏱ 15 Minutos";
                break;
            case 20:
                room.KillTime = helper.Minutes20;
                result = "⏱ 20 Minutos";
                break;
            case 25:
                room.KillTime = helper.Minutes25;
                result = "⏱ 25 Minutos";
                break;
            case 30:
                room.KillTime = helper.Minutes30;
                result = "⏱ 30 Minutos";
                break;
            default:
                return "⏱ Duração inválida! Use 5, 10, 15, 20, 25 ou 30";
        }
        return result;
    }
}
