// Server.Game, Version=1.1.24355.2, Culture=neutral, PublicKeyToken=null
// Server.Game.Data.Commands.ICommand
using Server.Game.Data.Models;

public interface ICommand
{
    string Command { get; }

    string Description { get; }

    string Permission { get; }

    string Args { get; }

    string Execute(string Command, string[] Args, Account Player);
}
