using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Executable;
using Executable.Forms;
using Executable.UDP.Server;
using Executable.Utility;
using Plugin.Core;
using Plugin.Core.Colorful;
using Plugin.Core.Enums;
using Plugin.Core.Filters;
using Plugin.Core.JSON;
using Plugin.Core.Managers;
using Plugin.Core.Managers.Events;
using Plugin.Core.Models;
using Plugin.Core.RAW;
using Plugin.Core.Utility;
using Plugin.Core.XML;
using Server.Auth;
using Server.Auth.Data.Utils;
using Server.Auth.Data.XML;
using Server.Game;
using Server.Game.Data.Managers;
using Server.Game.Data.XML;
using Server.Game.RconConnect;
using Server.Match;
using Server.Match.Data.XML;

namespace Executable
{
    public class Program
    {
        private delegate bool EventHandler(ControlType signal);

        private enum ControlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private const string VERSION = "V3.68";

        private const int CONSOLE_WIDTH = 160;

        private const int CONSOLE_HEIGHT = 40;

        private const int TASK_DELAY = 1000;

        private static string _titleArgument = "";

        private static Mutex _applicationMutex = null;

        private static readonly FileInfo ExecutableFile = new FileInfo(Assembly.GetExecutingAssembly().Location);

        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        [STAThread]
        private static void Main(string[] arguments)
        {
            SetupExceptionHandlers();
            try
            {
                InitializeConsole();
                KillExistingProcesses();
                if (CheckSingleInstance())
                {
                    SetupFirewallHandling();
                    DisplayApplicationBanner();
                    bool useEpicMode = CheckArgumentsForEpicMode(arguments);
                    if (useEpicMode)
                    {
                        StartMonitorFormThread();
                    }
                    string fileVersion = GetFileVersion();
                    LoadAllServerComponents(useEpicMode ? 1 : 0, fileVersion);
                }
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
            }
            finally
            {
                CleanupResources();
            }
        }

        private static void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Plugin.Core.Colorful.Console.CancelKeyPress += CancelKeyPressEvent;
        }

        private static void InitializeConsole()
        {
            Plugin.Core.Colorful.Console.SetWindowSize(160, 40);
            Plugin.Core.Colorful.Console.CursorVisible = false;
            Plugin.Core.Colorful.Console.TreatControlCAsInput = false;
            WindowUtility.MoveWindowToCenter();
            string fileVersion = GetFileVersion();
            Plugin.Core.Colorful.Console.Title = "Point Blank (RUS-V3.68) Server " + fileVersion;
            CLogger.StartedFor = "Server";
            CLogger.CheckDirectorys();
            Plugin.Core.Colorful.Console.Clear();
        }

        private static void KillExistingProcesses()
        {
            Process[] processesByName = Process.GetProcessesByName("PointBlank");
            foreach (Process process in processesByName)
            {
                process.Kill();
            }
        }

        private static bool CheckSingleInstance()
        {
            _applicationMutex = new Mutex(initiallyOwned: true, ExecutableFile.Name, out var isNewInstance);
            if (!isNewInstance)
            {
                CLogger.Print("O servidor já está em execução! Saindo...", LoggerType.Warning);
                MessageBox.Show("O servidor já está em execução!", "Aviso!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                Thread.Sleep(1000);
                WindowUtility.KillProcessAndChildren(CurrentProcessId);
                return false;
            }
            return true;
        }

        private static void SetupFirewallHandling()
        {
            if (MemoryUtility.IsAdministrator())
            {
                SetConsoleCtrlHandler(FirewallHandler, add: true);
                FirewallUtil.AddFirewallRule(ExecutableFile.FullName);
                _titleArgument = "ADMINISTRATOR";
            }
            else
            {
                _titleArgument = "SUPERUSER";
            }
        }

        private static void DisplayApplicationBanner()
        {
            DateTime buildDate = ExecutableFile.LastWriteTime;
            StringBuilder banner = new StringBuilder();
            banner.AppendLine("Contribuidores{0} {1}");
            banner.AppendLine("");
            banner.AppendLine(FormatRightAligned("MoMz Games"));
            banner.AppendLine(FormatRightAligned(string.Format("{0} +Build: {1:yyMMddHHmmss}", "V3.68", buildDate)));
            banner.AppendLine(FormatRightAligned("Copyright 2025 Zepetto Co. All right reserved"));
            Formatter[] titleFormatters = new Formatter[2]
            {
            new Formatter(":", ColorUtil.Yellow),
            new Formatter("", ColorUtil.Cyan)
            };
            Plugin.Core.Colorful.Console.WriteLineFormatted(banner.ToString(), ColorUtil.White, titleFormatters);
        }

        private static string GetFileVersion()
        {
            return FileVersionInfo.GetVersionInfo(ExecutableFile.Name).FileVersion;
        }

        private static bool CheckArgumentsForEpicMode(string[] arguments)
        {
            return GetFirstArgument(arguments).Equals("-supc");
        }

        private static void StartMonitorFormThread()
        {
            new Thread(LoadMonitorForm).Start();
        }

        private static void LoadAllServerComponents(int titleType, string version)
        {
            LoadConfiguration();
            LoadEventData();
            LoadShopData();
            LoadMissionCards();
            LoadServerData();
            LoadPlugins();
            bool socketValidation = ValidateSocketsAndDatabase();
            ValidateAndStartSockets(socketValidation, titleType, version);
        }

        private static void LoadConfiguration()
        {
            PrintSectionHeader("Config", "INIT");
            ConfigLoader.LoadTimeline();
            ConfigLoader.Load();
            ServerConfigJSON.Load();
            PrintSectionHeader("Config", "END");
        }

        private static void LoadEventData()
        {
            PrintSectionHeader("Events Data", "INIT");
            EventLoginSync.Load();
            EventMapSync.Load();
            EventPlaytimeSync.Load();
            EventQuestSync.Load();
            EventRankUpSync.Load();
            EventVisitSync.Load();
            EventXmasSync.Load();
            PrintSectionHeader("Events Data", "END");
        }

        private static void LoadShopData()
        {
            PrintSectionHeader("Shop Data", "INIT");
            ShopManager.Load(1);
            ShopManager.Load(2);
            PrintSectionHeader("Shop Data", "END");
        }

        private static void LoadMissionCards()
        {
            PrintSectionHeader("MissionCards", "INIT");
            MissionCardRAW.LoadBasicCards(1);
            MissionCardRAW.LoadBasicCards(2);
            PrintSectionHeader("MissionCards", "END");
        }

        private static void LoadServerData()
        {
            PrintSectionHeader("Server Data", "INIT");
            TemplatePackXML.Load();
            TitleSystemXML.Load();
            TitleAwardXML.Load();
            MissionAwardXML.Load();
            MissionConfigXML.Load();
            SChannelXML.Load();
            SynchronizeXML.Load();
            SystemMapXML.Load();
            ClanRankXML.Load();
            PlayerRankXML.Load();
            CouponEffectManager.LoadCouponFlags();
            PermissionXML.Load();
            RandomBoxXML.Load();
            GameRuleXML.Load();
            DirectLibraryXML.Load();
            InternetCafeXML.Load();
            RedeemCodeXML.Load();
            SeasonPass.LoadSeasonPass();
            CommandHelperJSON.Load();
            HalfUtil.Load();
            NickFilter.Load();
            Translation.Load();
            CardData.LoadSeason();
           // AllUtils.LoadPlayerFriend(this, true); //x

            //  CardData.LoadCards();
            //CardData.LoadSeason();
            if (ConfigLoader.RconEnable)
            {
                RconManager.Instance();
            }
            PrintSectionHeader("Server Data", "END");
        }

        private static void LoadPlugins()
        {
            Thread.Sleep(250);
            PrintSectionHeader("Status do plug-in", "INIT");
            CLogger.Print("All server plugins were loaded successfully", LoggerType.Info);
            PrintSectionHeader("Status do plug-in", "END");
        }

        private static bool ValidateSocketsAndDatabase()
        {
            return ComDiv.ValidateAllPlayersAccount() && InitializeAllSockets();
        }

        private static bool InitializeAllSockets()
        {
            try
            {
                InitializeAuthServer();
                InitializeGameServer();
                InitializeBattleServer();
                return true;
            }
            catch (Exception ex)
            {
                CLogger.Print(ex.Message, LoggerType.Error, ex);
                return false;
            }
        }

        private static void InitializeAuthServer()
        {
            PrintSectionHeader("Authentication Server", "INIT");
            Server.Auth.Data.XML.ChannelsXML.Load();
            AuthXender.GetPlugin(ConfigLoader.Ip4Address, ConfigLoader.DEFAULT_PORT[0]);
            PrintSectionHeader("Authentication Server", "END");
        }

        private static void InitializeGameServer()
        {
            PrintSectionHeader("Game Server", "INIT");
            Server.Game.Data.XML.ChannelsXML.Load();
            ClanManager.Load();
            CommandManager.Load();
            foreach (SChannelModel server in SChannelXML.Servers)
            {
                if (server.Id >= 1 && server.Port >= ConfigLoader.DEFAULT_PORT[1])
                {
                    GameXender.GetPlugin(server.Id, ConfigLoader.Ip4Address, server.Port);
                }
            }
            PrintSectionHeader("Battle Server", "END");
        }

        private static void InitializeBattleServer()
        {
            PrintSectionHeader("Battle Server", "INIT");
            MapStructureXML.Load();
            CharaStructureXML.Load();
            ItemStatisticXML.Load();
            MatchXender.GetPlugin(ConfigLoader.Ip4Address, ConfigLoader.DEFAULT_PORT[2]);
            PrintSectionHeader("Battle Server", "END");
        }

        private static void ValidateAndStartSockets(bool socketActive, int titleType, string version)
        {
            StringUtility.ServerVersionL = version;
            StringUtility.ServerStatusL = (socketActive ? "SERVER ONLINE" : "SERVER OFFLINE");
            PrintSectionHeader("server status", "INIT");
            if (socketActive)
            {
                CLogger.Print("Initialization Successful, Server Runtime " + DateTimeUtil.Now("yyyy"), LoggerType.Info);
                Plugin.Core.Colorful.Console.WriteLine("");
                StartTitleInformationTask(titleType, version);
            }
            else
            {
                CLogger.Print("Unsuccessful startup, Server runtime " + DateTimeUtil.Now("yyyy"), LoggerType.Warning);
                Plugin.Core.Colorful.Console.WriteLine("");
            }
            PrintSectionHeader("Server Status", "END");
        }

        private static async void StartTitleInformationTask(int titleType, string version)
        {
            try
            {
                while (true)
                {
                    UpdateFormInformation();
                    UpdateConsoleTitle(titleType, version);
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Exception ex2 = ex;
                CLogger.Print(ex2.Message, LoggerType.Error, ex2);
            }
        }

        private static void UpdateConsoleTitle(int titleType, string version)
        {
            double memoryUsage = MemoryUtility.GetMemoryUsage();
            double memoryUsagePercent = MemoryUtility.GetMemoryUsagePercent();
            int totalUsers = ComDiv.CountDB("SELECT COUNT(*) FROM accounts");
            int onlineUsers = ComDiv.CountDB($"SELECT COUNT(*) FROM accounts WHERE online = {true}");
            string titleInfo = ((titleType == 1) ? $"RAM Usages: {memoryUsage:0.0} MB)" : $"Users: {totalUsers}; Online: {onlineUsers}; RAM Usages: {memoryUsage:0.0} MB ({memoryUsagePercent:0.0}%)");
            Plugin.Core.Colorful.Console.Title = "Point Blank (EROS PB-V3.68) Server " + version + " </> " + titleInfo + " -" + _titleArgument + " </> Timeline: " + DateTimeUtil.Now("dddd, MMMM dd, yyyy - HH:mm:ss");
        }

        private static void UpdateFormInformation()
        {
            StringUtility.MemoryValueVPB = $"{Convert.ToInt32(MemoryUtility.GetMemoryUsage())}";
            StringUtility.MemoryUsageL = $"{MemoryUtility.GetMemoryUsagePercent():0.0}%";
            StringUtility.LogFileSize = $"{(double)MemoryUtility.GetDirectorySize(new DirectoryInfo($"{ExecutableFile.Directory}/Logs"), IncludeSubDir: true) / 1048576.0:N2}MB";
            StringUtility.RegisteredUserL = string.Format("{0}", ComDiv.CountDB("SELECT COUNT(*) FROM accounts"));
            StringUtility.OnlineUserL = $"{ComDiv.CountDB($"SELECT COUNT(*) FROM accounts WHERE online = '{true}'")}";
            StringUtility.TotalClansL = string.Format("{0}", ComDiv.CountDB("SELECT COUNT(*) FROM system_clan"));
            StringUtility.VipUserL = string.Format("{0}", ComDiv.CountDB("SELECT COUNT(*) FROM accounts WHERE pc_cafe = '2' OR pc_cafe = '1'"));
            StringUtility.UnknownUserL = string.Format("{0}", ComDiv.CountDB("SELECT COUNT(*) FROM accounts WHERE nickname = ''"));
            StringUtility.BannedPlayers = string.Format("{0}", ComDiv.CountDB("SELECT COUNT(*) FROM base_auto_ban"));
            StringUtility.RegShopItems = string.Format("{0}", ComDiv.CountDB("SELECT COUNT(*) FROM system_shop") + ComDiv.CountDB("SELECT COUNT(*) FROM system_shop_effects") + ComDiv.CountDB("SELECT COUNT(*) FROM system_shop_sets"));
            StringUtility.ShopCafeItems = $"{ComDiv.CountDB($"SELECT COUNT(*) FROM system_shop WHERE item_visible = '{false}'") + ComDiv.CountDB($"SELECT COUNT(*) FROM system_shop_effects WHERE coupon_visible = '{false}'")}";
            StringUtility.RepairableItems = string.Format("{0}", ComDiv.CountDB("SELECT COUNT(*) FROM system_shop_repair"));
            StringUtility.ForGameVersionL = "V3.68";
            StringUtility.ForGameRegionL = GetGameLocale();
            StringUtility.LocalAddressL = GetLocalIPAddress();
            StringUtility.ServerTimelineL = DateTimeUtil.Now("yyyy");
            StringUtility.SelectedServerConfigL = $"{ConfigLoader.ConfigId}";
            StringUtility.SelectedGameRuleL = $"{ConfigLoader.RuleId}";
            StringUtility.InternetCafeL = (ConfigLoader.ICafeSystem ? "Enabled" : "Disabled");
            StringUtility.EnableAutoAccountL = (ConfigLoader.AutoAccount ? "Enabled" : "Disabled");
            StringUtility.AutoBanPlayerL = (ConfigLoader.AutoBan ? "Enabled" : "Disabled");
        }

        private static string FormatRightAligned(string text)
        {
            StringBuilder stringBuilder = new StringBuilder(80);
            if (text != null)
            {
                stringBuilder.Append(" ");
            }
            int paddingCount = 79 - text.Length;
            while (stringBuilder.Length != paddingCount)
            {
                stringBuilder.Append(" ");
            }
            stringBuilder.Append(text);
            return stringBuilder.ToString();
        }

        private static void PrintSectionHeader(string name, string type)
        {
            StringBuilder stringBuilder = new StringBuilder(80);
            if (name != null)
            {
                stringBuilder.Append("---[").Append(name).Append(']');
            }
            string ending = (string.IsNullOrEmpty(type) ? "" : ("[" + type + "]---"));
            int paddingCount = 79 - ending.Length;
            while (stringBuilder.Length != paddingCount)
            {
                stringBuilder.Append('-');
            }
            stringBuilder.Append(ending);
            Plugin.Core.Colorful.Console.WriteLine((type.Equals("END") ? $"{stringBuilder}\n" : $"\n{stringBuilder}") ?? "");
        }

        private static void LoadMonitorForm()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            MainForm monitor = new MainForm(CurrentProcessId, new DirectoryInfo($"{ExecutableFile.Directory}/Logs"))
            {
                TopMost = true
            };
            Application.Run(monitor);
        }

        private static string GetLocalIPAddress()
        {
            string address = "";
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress[] addressList = hostEntry.AddressList;
                foreach (IPAddress ipAddress in addressList)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = ipAddress.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Aviso!\r\n", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return address;
        }

        private static string GetGameLocale()
        {
            foreach (ClientLocale region in ConfigLoader.GameLocales)
            {
                if (region == ClientLocale.Russia)
                {
                    return region.ToString();
                }
            }
            return "Fora!\r\n";
        }

        private static string GetFirstArgument(string[] arguments)
        {
            return (arguments.Length != 0) ? arguments[0] : "";
        }

        public static DateTime GetCurrentDateFromWeb()
        {
            try
            {
                WebResponse response = WebRequest.Create("http://www.google.com").GetResponse();
                return DateTime.ParseExact(response.Headers["date"], "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal).ToUniversalTime();
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static bool FirewallHandler(ControlType signal)
        {
            switch (signal)
            {
                case ControlType.CTRL_CLOSE_EVENT:
                case ControlType.CTRL_LOGOFF_EVENT:
                case ControlType.CTRL_SHUTDOWN_EVENT:
                    FirewallUtil.RemoveFirewallRule(ExecutableFile.FullName);
                    return true;
                default:
                    return false;
            }
        }

        private static void CancelKeyPressEvent(object sender, ConsoleCancelEventArgs e)
        {
            ConfigLoader.Load();
            CLogger.Print("Configurações do aplicativo recarregadas.", LoggerType.Info);
            SendMessage.FromServer("Bem-vindo ao servidor!");
            e.Cancel = true;
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CLogger.Print($"Remetente de exceção do identificador do aplicativo:\r\n {sender} Terminando:\r\n {e.IsTerminating} {(Exception)e.ExceptionObject}", LoggerType.Error);
        }

        private static void CleanupResources()
        {
            _applicationMutex?.ReleaseMutex();
            Process.GetCurrentProcess().WaitForExit();
        }
    }
}
