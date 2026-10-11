using Fleck;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.Data.Managers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.RconConnect.Packets
{
    public class SendShopToClient : RconReceive
    {
        private IWebSocketConnection client;
        private DataTable shopTable;

        public override void Run()
        {
            string tableName = PopString();
            if (string.IsNullOrEmpty(tableName))
            {
                CLogger.Print("Invalid TableName specified for SendShopToClient", LoggerType.Warning);
                return;
            }

            shopTable = AccountManager.getAllShopClient(tableName);

            //converter o DataTable para json

            string jsonShop = DataTableJson(shopTable);


            //enviar reposta para o cliente
            if (client != null)
            {
                client.Send(jsonShop);
            }
            else
            {
                CLogger.Print("No client connection available to send data.", LoggerType.Warning);
            }
        }

        public void setClientConnection(IWebSocketConnection client)
        {
            this.client = client;
        }

        private string DataTableJson(DataTable table)
        {
            return JsonConvert.SerializeObject(table, Formatting.Indented);
        }
    }
}
