// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Data.Commands.Server.Game.Data.Commands.HelpCommand
using System;
using System.Collections.Generic;
using System.Linq;
using Server.Game.Data.Commands;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;

public class HelpCommand : ICommand
{
    private const int MAX_COMMANDS_PER_PAGE = 3;

    public string Command => "help";

    public string Description => "Mostra todos os comandos disponíveis";

    public string Permission => "helpcommand";

    public string Args => "[página]";

    public string Execute(string command, string[] args, Account player)
    {
        if (!player.HavePermission(Permission))
        {
            string error = "Você não tem permissão para usar este comando!";
            player.Connection?.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(error));
            return error;
        }
        int page = 1;
        if (args.Length != 0 && int.TryParse(args[0], out var requestedPage) && requestedPage > 0)
        {
            page = requestedPage;
        }
        List<ICommand> commands = CommandManager.GetCommandsForPlayer(player).ToList();
        int totalPages = (int)Math.Ceiling((double)commands.Count / 3.0);
        totalPages = Math.Max(1, totalPages);
        if (page > totalPages)
        {
            string errorMsg = $"Página inválida. Total de páginas: {totalPages}";
            player.Connection?.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(errorMsg));
            return errorMsg;
        }
        IEnumerable<ICommand> pageCommands = commands.Skip((page - 1) * 3).Take(3);
        string result = $"=== COMANDOS DISPONÍVEIS (Página {page}/{totalPages}) ===\n";
        foreach (ICommand cmd in pageCommands)
        {
            result = result + "#" + cmd.Command + " " + cmd.Args + " - " + cmd.Description + "\n";
        }
        if (totalPages > 1)
        {
            result += $"Use #help [página] para navegar entre as páginas (1-{totalPages})";
        }
        player.Connection?.SendPacket(new PROTOCOL_SERVER_MESSAGE_ANNOUNCE_ACK(result));
        return result;
    }
}
