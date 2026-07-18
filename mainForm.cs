using System;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GdidGuard
{
    public class MainForm : Form
    {
        private CheckBox chkGdid;
        private CheckBox chkCdp;
        private CheckBox chkDo;
        private Button btnApply;
        private TextBox txtLog;
        private System.Windows.Forms.Timer gdidTimer;
        private NotifyIcon trayIcon;
        private bool reallyClose = false;

        public MainForm()
        {
            BuildUi();
            SetupTray();
        }

        
        private static readonly Color OutlineRed = Color.Red;

        private void BuildUi()
        {
            Text = "GDID Guard";
            Width = 540;
            Height = 460;
            StartPosition = FormStartPosition.CenterScreen;
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;


            string bgPath = Path.Combine(Application.StartupPath, "bg.png");
            if (File.Exists(bgPath))
            {
                BackgroundImage = Image.FromFile(bgPath);
                BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                BackColor = Color.Black; 
            }

            chkGdid = new CheckBox
            {
                Text = "Auto-wipe GDID (Global Device ID) every 60 seconds",
                Left = 20,
                Top = 20,
                Width = 480,
                Checked = true
            };
            StyleRedOutline(chkGdid);

            chkCdp = new CheckBox
            {
                Text = "Disable Connected Devices Platform / DiagTrack telemetry services",
                Left = 20,
                Top = 55,
                Width = 480
            };
            StyleRedOutline(chkCdp);

            chkDo = new CheckBox
            {
                Text = "Disable Delivery Optimization P2P sharing",
                Left = 20,
                Top = 90,
                Width = 480
            };
            StyleRedOutline(chkDo);

            btnApply = new Button
            {
                Text = "Apply Selected",
                Left = 20,
                Top = 130,
                Width = 160,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            btnApply.FlatAppearance.BorderColor = OutlineRed;
            btnApply.FlatAppearance.BorderSize = 2;
            btnApply.Click += BtnApply_Click;


            var logBorder = new Panel
            {
                Left = 20,
                Top = 175,
                Width = 484,
                Height = 234,
                BackColor = OutlineRed,
                Padding = new Padding(2),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            logBorder.Controls.Add(txtLog);

            Controls.Add(chkGdid);
            Controls.Add(chkCdp);
            Controls.Add(chkDo);
            Controls.Add(btnApply);
            Controls.Add(logBorder);

            gdidTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            gdidTimer.Tick += (s, e) => WipeGdid();

            Log("Ready. Check the boxes for what you want protected, then click Apply Selected.");
            if (!IsAdministrator())
            {
                Log("NOTE: Not running as Administrator yet. Service-related toggles need admin rights to work.");
            }
        }


        private void StyleRedOutline(CheckBox chk)
        {
            chk.Appearance = Appearance.Normal;
            chk.FlatStyle = FlatStyle.Flat;
            chk.ForeColor = Color.White;
            chk.BackColor = Color.Black;
            chk.Padding = new Padding(4);
            chk.FlatAppearance.BorderColor = OutlineRed;
            chk.FlatAppearance.BorderSize = 1;
        }

        private void SetupTray()
        {
            trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Shield,
                Text = "GDID Guard",
                Visible = false
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Show", null, (s, e) => RestoreFromTray());
            menu.Items.Add("Exit", null, (s, e) => { reallyClose = true; trayIcon.Visible = false; Close(); });
            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(1500, "GDID Guard", "Still running quietly in the background.", ToolTipIcon.Info);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            if (!reallyClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
            }
        }

        private void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(line)));
            }
            else
            {
                AppendLog(line);
            }
        }

        private void AppendLog(string line)
        {
            txtLog.AppendText(line + Environment.NewLine);
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (!IsAdministrator())
            {
                Log("WARNING: Not running as Administrator. Service changes below will likely fail.");
                Log("Close this, right-click GdidGuard.exe, choose 'Run as administrator', then try again.");
            }

            if (chkGdid.Checked)
            {
                WipeGdid();
                gdidTimer.Start();
                Log("GDID auto-wipe timer running (checks every 60 seconds).");
            }
            else
            {
                gdidTimer.Stop();
                Log("GDID auto-wipe timer stopped.");
            }

            if (chkCdp.Checked)
            {
                DisableService("DiagTrack");
                DisableService("CDPSvc");
                DisableCdpUserServices();
            }

            if (chkDo.Checked)
            {
                DisableDeliveryOptimization();
            }

            Log("Apply finished.");
        }

        private void WipeGdid()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\IdentityCRL\ExtendedProperties", true))
                {
                    if (key != null && key.GetValue("LID") != null)
                    {
                        key.DeleteValue("LID", false);
                        Log("Deleted GDID (LID) registry value.");
                    }
                    else
                    {
                        Log("LID value not present right now.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error wiping GDID: {ex.Message}");
            }
        }

        private void DisableService(string serviceName)
        {
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
                SetServiceStartupDisabled(serviceName);
                Log($"Stopped and disabled service: {serviceName}");
            }
            catch (Exception ex)
            {
                Log($"Could not disable {serviceName}: {ex.Message}");
            }
        }

        private void DisableCdpUserServices()
        {
            try
            {
                foreach (var sc in ServiceController.GetServices())
                {
                    if (sc.ServiceName.StartsWith("CDPUserSvc", StringComparison.OrdinalIgnoreCase))
                    {
                        DisableService(sc.ServiceName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Could not enumerate CDPUserSvc instances: {ex.Message}");
            }
        }

        private void SetServiceStartupDisabled(string serviceName)
        {
            RunHidden("sc.exe", $"config \"{serviceName}\" start= disabled");
        }

        private void DisableDeliveryOptimization()
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config"))
                {
                    key.SetValue("DODownloadMode", 0, RegistryValueKind.DWord);
                }
                Log("Delivery Optimization P2P sharing disabled.");
            }
            catch (Exception ex)
            {
                Log($"Could not disable Delivery Optimization: {ex.Message}");
            }
        }

        private void RunHidden(string fileName, string arguments)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                p.WaitForExit();
            }
        }

        private bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
