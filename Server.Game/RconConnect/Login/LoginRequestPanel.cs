using Plugin.Core.Models;
using Server.Game.Data.Managers;
using Server.Game.Data.Models;

namespace Server.Game.RconConnect.Login
{
    public class LoginRequestPanel : RconReceive
    {
        public override void Run()
        {
            string Username = PopString();
            string Password = PopString();

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                SendJsonError("O nome de usuário ou a senha não podem estar vazios.");
                return;
            }

            Account account = AccountManager.GetAccountDB(Username, 0, 0);

            if (account == null)
            {
                SendJsonError("Conta não encontrada.");
                return;
            }

            if (account.Password != Password)
            {
                SendJsonError("Login ou Senha incorreta.");
                return;
            }

            // ✅ Cria um objeto anônimo com os dados do usuário
            var response = new
            {
                status = "success",
                message = $"Login bem-sucedido. Bem-vindo {account.Nickname}!",
                data = new
                {
                    username = account.Username,
                    nickname = account.Nickname,
                    access_level = (int)account.Access,
                    rank = account.Rank
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            SendMessageToClient(json);
        }

        private void SendJsonError(string message)
        {
            var response = new
            {
                status = "error",
                message = message
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            SendMessageToClient(json);
        }

    }
}
