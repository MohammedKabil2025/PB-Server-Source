using Server.Game.Data.Managers;
using Server.Game.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Data.Commands.CommandsInGame
{
    public static class ViewUser
    {
        public static string ViewUsers(string input)
        {
            try
            {
                string nick = input.Trim(); // Remove espaços extras
                if (string.IsNullOrEmpty(nick))
                    return "Nickname inválido.";

                Account account = AccountManager.GetAccount(nick, 1, 0);
                if (account == null)
                    return "Usuário não existe.";

                return $"PlayerID: {account.PlayerId} | Nickname: {account.Nickname} | AccountIP: {account.PublicIP}";
            }
            catch (Exception ex)
            {
                return $"Erro ao buscar usuário: {ex.Message}";
            }
        }
    }
}
