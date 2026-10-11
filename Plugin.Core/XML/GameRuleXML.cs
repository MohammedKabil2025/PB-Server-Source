using Microsoft.Extensions.Logging;
using Plugin.Core.Enums;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Plugin.Core.XML
{
    public class GameRuleXML
    {
        public static List<int> itemscamp = new List<int>();
        public static List<int> itemscnpb = new List<int>();
        public static List<int> itemslan = new List<int>();
        public static readonly List<int> CuponsEffectsBlocked = new List<int>();
        private static readonly string path = "data/GameRules.xml";

        public static void Load()
        {
            if (!File.Exists(GameRuleXML.path))
            {
                CLogger.Print("[RegrasDeJogo] " + GameRuleXML.path + " no exists.", LoggerType.Warning);
            }
            else
            {
                GameRuleXML.LoadList();
                Console.WriteLine("---[Rules]---------------------------------------------------[Begin]---");
                CLogger.Print($"[Rule] loaded {GameRuleXML.itemscamp.Count} rules @CAMP.", LoggerType.Info);
                CLogger.Print($"[Rule] loaded {GameRuleXML.itemscnpb.Count} rules @CNPB.", LoggerType.Info);
                CLogger.Print($"[Rule] loaded {GameRuleXML.itemslan.Count} rules @LAN.", LoggerType.Info);
                CLogger.Print($"[Rule] loaded {GameRuleXML.CuponsEffectsBlocked.Count} CUPONS.", LoggerType.Info);
                Console.WriteLine("---[Rules]---------------------------------------------------[Ended]---");
            }
        }

        public static void RealodCamp()
        {
            itemscamp.Clear();
            itemscnpb.Clear();
            itemslan.Clear();
            itemslan.Clear();
            LoadList();
        }

        private static void LoadList()
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(GameRuleXML.path);
                for (XmlNode xmlNode1 = xmlDocument.FirstChild; xmlNode1 != null; xmlNode1 = xmlNode1.NextSibling)
                {
                    if ("list".Equals(xmlNode1.Name))
                    {
                        for (XmlNode xmlNode2 = xmlNode1.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
                        {
                            if ("Rule".Equals(xmlNode2.Name))
                            {
                                XmlNamedNodeMap attributes = (XmlNamedNodeMap)xmlNode2.Attributes;
                                string str = attributes.GetNamedItem("Name").Value;
                                int num = int.Parse(attributes.GetNamedItem("Id").Value);
                                if (str.Equals("@CAMP"))
                                    GameRuleXML.itemscamp.Add(num);
                                else if (str.Equals("@CNPB"))
                                    GameRuleXML.itemscnpb.Add(num);
                                else if (str.Equals("COUPON"))
                                    GameRuleXML.CuponsEffectsBlocked.Add(num);
                                else if (str.Equals("@LAN"))
                                    GameRuleXML.itemslan.Add(num);

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CLogger.Print("Ocorreu um problema ao carregar os Tournament Rules!\r\n" + ex.ToString());
            }
        }

        public static void ReGenerateList()
        {
            GameRuleXML.itemscamp.Clear();
            GameRuleXML.itemscnpb.Clear();
            GameRuleXML.itemslan.Clear();
            GameRuleXML.CuponsEffectsBlocked.Clear();
            GameRuleXML.Load();
        }

        public static bool IsBlocked(int listid, int id) => listid == id;

        public static bool CheckRoomRule(string name) => ConfigLoader.EnableRules && (name.Contains("@CAMP") || name.Contains("CAMP") || (name.Contains("@CNPB") || name.Contains("CNPB")));

        public static bool IsBlocked(int listid, int id, ref List<string> list, string category)
        {
            if (listid != id)
                return false;
            list.Add(category);
            return true;
        }
    }
}
