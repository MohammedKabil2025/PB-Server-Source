// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Data.Managers.CommandManager
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Server.Game.Data.Commands;
using Server.Game.Data.Models;
using Server.Game.Network.ServerPacket;

public static class CommandManager
{
    private static readonly Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

    public static void Load()
    {
        Commands.Clear();
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in from t in assembly.GetTypes()
                              where typeof(ICommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract
                              select t)
        {
            if (Activator.CreateInstance(type) is ICommand command)
            {
                Commands[command.Command] = command;
            }
        }
    }

    public static bool TryParse(string text, Account player)
    {
        if (string.IsNullOrWhiteSpace(text) || player == null)
        {
            return false;
        }
        text = text.Trim();
        if (text.StartsWith("#"))
        {
            string input = text.Substring(1).Trim();
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            string[] parts = input.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string commandName = parts[0];
            string[] args = ((parts.Length > 1) ? parts.Skip(1).ToArray() : Array.Empty<string>());
            if (Commands.TryGetValue(commandName, out var command) && player.HavePermission(command.Permission))
            {
                string result = command.Execute(commandName, args, player);
                player.Connection?.SendPacket(new PROTOCOL_LOBBY_CHATTING_ACK("EROS PB", 0, 5, GMColor: false, result));
                return true;
            }
        }
        return false;
    }

    public static IEnumerable<ICommand> GetCommandsForPlayer(Account player)
    {
        if (player == null)
        {
            return Enumerable.Empty<ICommand>();
        }
        return from cmd in Commands.Values
               where player.HavePermission(cmd.Permission)
               orderby cmd.Command
               select cmd;
    }

    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int chunkSize)
    {
        while (source.Any())
        {
            yield return source.Take(chunkSize);
            source = source.Skip(chunkSize);
        }
    }
}
