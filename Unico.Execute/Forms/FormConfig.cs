using Executable.Utility;
using Plugin.Core;
using Plugin.Core.Enums;
using Plugin.Core.Settings;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Executable.Forms
{
    public partial class FormConfig : Form
    {
        private readonly DirectoryInfo ServerDir;
        public FormConfig(DirectoryInfo ServerDir)
        {
            InitializeComponent();
            this.ServerDir = ServerDir;
        }
        #region Attachments
        private void MoreInfoStatus(bool High, bool Default)
        {
            HighRB.Checked = High;
            DefaultRB.Checked = Default;
        }
        #endregion Attachments
        private void FormConfig_Load(object sender, EventArgs e)
        {
            float OpacityValue = float.Parse("10") / 100;
            BackgroundImage = ImageUtility.GetInstance().ChangeOpacity(Properties.Resources.PBLOGO_256, OpacityValue);
            BackgroundImageLayout = ImageLayout.Center;
            if (ConfigLoader.ShowMoreInfo)
            {
                MoreInfoStatus(true, false);
            }
            else
            {
                MoreInfoStatus(false, true);
            }
        }
        private void OpenConfigBTN_Click(object sender, EventArgs e)
        {
            new Thread(() => MemoryUtility.ShellMode(@"Config/Settings.ini", "notepad.exe", "open")).Start();
        }
        private void ChangeLogBTN_Click(object sender, EventArgs e)
        {
            ConfigEngine CFG = new ConfigEngine("Config/Settings.ini");
            if (!ConfigLoader.ShowMoreInfo)
            {
                ConfigLoader.ShowMoreInfo = true;
                MoreInfoStatus(true, false);
            }
            else
            {
                ConfigLoader.ShowMoreInfo = false;
                MoreInfoStatus(false, true);
            }
            new Thread(() =>
            {
                string Key = "MoreInfo", Section = "Server";
                if (!CFG.KeyExists(Key, Section))
                {
                    MessageBox.Show($"Key: '{Key}' on Section '{Section}' doesn't exist!", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                CFG.WriteX(Key, ConfigLoader.ShowMoreInfo, Section);
            }).Start();
            MessageBox.Show($"Logs mode changed to {(ConfigLoader.ShowMoreInfo ? "High." : "Default.")}", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ClearLogBTN_Click(object sender, EventArgs e)
        {
            new Thread(() => MemoryUtility.ClearCache(ServerDir)).Start();
            MessageBox.Show("Logs has been cleared!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Reload1BTN_Click(object sender, EventArgs e)
        {
            Reload1BTN.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            string originalText = Reload1BTN.Text;
            Reload1BTN.Text = "Recarregando...";
            Application.DoEvents();
            new Thread((ThreadStart)delegate
            {
                try
                {
                    bool success = ReloaderUtil.ReloadConfig();
                    Invoke((MethodInvoker)delegate
                    {
                        Reload1BTN.Enabled = true;
                        Reload1BTN.Text = originalText;
                        Cursor.Current = Cursors.Default;
                        if (success)
                        {
                            MessageBox.Show("Todas as configurações foram recarregadas:\n\n• Configurações do servidor\n• Filtros de nickname\n• Sistema de traduções\n• Configurações JSON\n", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        }
                        else
                        {
                            MessageBox.Show("O recarregamento completou com falhas parciais", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Exception ex2 = ex;
                    Exception ex3 = ex2;
                    Invoke((MethodInvoker)delegate
                    {
                        Reload1BTN.Enabled = true;
                        Reload1BTN.Text = originalText;
                        Cursor.Current = Cursors.Default;
                        MessageBox.Show("Falha crítica no recarregamento:\n" + ex3.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        CLogger.Print("Falha no ReloadConfig: " + ex3.Message, LoggerType.Error, ex3);
                    });
                }
            }).Start();
        }
    }
}
