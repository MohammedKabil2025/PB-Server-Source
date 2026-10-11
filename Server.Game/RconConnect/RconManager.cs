using Fleck;
using Newtonsoft.Json.Linq;
using Plugin.Core;
using Plugin.Core.Enums;
using Server.Game.RconConnect.Login;
using Server.Game.RconConnect.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.RconConnect
{
    public class RconManager
    {
        private static RconManager manager;
        public static RconManager Instance()
            => manager ?? (manager = new RconManager());

        private WebSocketServer server;

        private RconManager()
        {
            try
            {
                FleckLog.Level = LogLevel.Error;

                server = new WebSocketServer($"ws://{ConfigLoader.RconIp}:{ConfigLoader.RconPort}");
                server.Start(client =>
                {
                    client.OnMessage = message => Receive(message, client);
                });

                CLogger.Print($"RconManager started. [ws://{ConfigLoader.RconIp}:{ConfigLoader.RconPort}]", LoggerType.Warning);
            }
            catch (Exception ex)
            {
                CLogger.Print("RconManager start: " + ex.ToString(), LoggerType.Warning);
            }
        }

        private void Receive(string message, IWebSocketConnection client)
        {
            try
            {
                if (!ConfigLoader.RconValidIps.Contains(client.ConnectionInfo.ClientIpAddress))
                {
                    if (ConfigLoader.RconPrintNotValidIp)
                        CLogger.Print("Received message from unlisted ip: " + client.ConnectionInfo.ClientIpAddress, LoggerType.Warning);
                    return;
                }

                string[] parts = null;

                int opcode = 0; // declarar uma vez aqui
                if (message.StartsWith("{") && message.EndsWith("}"))
                {
                    // Tenta interpretar como JSON
                    try
                    {
                        JObject obj = JObject.Parse(message);

                        opcode = (int)obj["opcode"];
                        string password = (string)obj["authcode"];

                        if (password != ConfigLoader.RconPassword)
                        {
                            CLogger.Print("RconManager received request with wrong password: " + password, LoggerType.Warning);
                            return;
                        }

                        // Monta array de partes simulando o formato esperado pelo Init
                        var partsList = new List<string>
                {
                    opcode.ToString(),
                    password
                };

                        // Adiciona os demais campos, ignorando opcode e authcode
                        foreach (var prop in obj.Properties())
                        {
                            if (prop.Name != "opcode" && prop.Name != "authcode")
                            {
                                partsList.Add((string)prop.Value);
                            }
                        }

                        parts = partsList.ToArray();
                    }
                    catch (Exception jsonEx)
                    {
                        CLogger.Print("Erro ao parsear JSON Rcon: " + jsonEx.ToString(), LoggerType.Warning);
                        return;
                    }
                }
                else if (message.Contains("|") && message.Split('|').Length >= 2)
                {
                    parts = message.Split('|');

                    if (parts.Length < 2)
                    {
                        CLogger.Print("RconManager received request with insufficient parts in pipe message.", LoggerType.Warning);
                        return;
                    }

                    string password = parts[1];
                    if (password != ConfigLoader.RconPassword)
                    {
                        CLogger.Print("RconManager received request with wrong password: " + password, LoggerType.Warning);
                        return;
                    }
                }
                else
                {
                    CLogger.Print("RconManager received request with wrong message format: " + message, LoggerType.Warning);
                    return;
                }

                // Agora parts está definido, segue com seu código normal
                opcode = int.Parse(parts[0]);

                RconReceive receiver = null;
                switch (opcode)
                {
                    case 1: receiver = new RconSendMoney(); break;
                    case 2: receiver = new RconSendPoints(); break;
                    case 3:
                        receiver = new SendPlayerAccounts();
                        ((SendPlayerAccounts)receiver).SetClientConnection(client);
                        break;
                    case 4:
                        receiver = new SendShopToClient();
                        ((SendShopToClient)receiver).setClientConnection(client);
                        break;
                    case 5: receiver = new SendItemPlayer(); break;
                    case 6:
                        receiver = new LoginRequestPanel();
                        receiver.SetClientConnection(client);
                        break;
                    case 7:
                        receiver = new RconSetVip();
                        receiver.SetClientConnection(client);
                        break;
                    default:
                        CLogger.Print("RconManager received request with opcode: " + opcode, LoggerType.Error);
                        return;
                }

                if (receiver != null)
                {
                    receiver.Init(parts);
                    receiver.Run();
                }
            }
            catch (Exception ex)
            {
                CLogger.Print("RconManager receive: " + ex.ToString(), LoggerType.Warning);
            }
        }
    }
}
