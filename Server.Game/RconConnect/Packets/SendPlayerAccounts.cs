using Fleck;
using Newtonsoft.Json;
using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Managers;
using System.Data;

namespace Server.Game.RconConnect.Packets
{
    public class SendPlayerAccounts : RconReceive
    {
        private IWebSocketConnection client;
        private DataTable accountsTable;

        public override void Run()
        {
            string tableName = PopString(); // Aqui você usa o PopString para obter o nome da tabela
            if (string.IsNullOrEmpty(tableName))
            {
                CLogger.Print("Invalid tableName specified for SendPlayerAccounts.", LoggerType.Warning);
                return;
            }

            accountsTable = AccountManager.getAllAccounts(tableName);

            // Converter o DataTable para JSON
            string jsonAccounts = DataTableToJson(accountsTable);

            // Enviar resposta para o cliente
            if (client != null)
            {
                client.Send(jsonAccounts);
            }
            else
            {
                CLogger.Print("No client connection available to send data.", LoggerType.Error);
            }
        }

        public void SetClientConnection(IWebSocketConnection client)
        {
            this.client = client;
        }

        private string DataTableToJson(DataTable table)
        {
            return JsonConvert.SerializeObject(table, Formatting.Indented);
        }
    }
}
