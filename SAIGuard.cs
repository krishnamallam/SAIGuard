// ============================================================================
//  SAI Guard - Keep your Windows machine awake
//  https://github.com/krishnamallam/SAIGuard
//
//  Click to run. Sits in your system tray. Auto-starts on boot.
//  Uses the Windows SetThreadExecutionState API - no fake keystrokes,
//  no mouse jiggling, no interference with automation or testing.
//
//  BUILD (no SDK needed):
//    build.bat
//
//  Or manually:
//    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
//      /out:SAIGuard.exe /target:winexe /optimize+ ^
//      /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
//      /win32icon:SAIGuard.ico SAIGuard.cs
//
//  License: MIT
//  Copyright (c) 2026 Medialogic AI - https://medialogicai.it
// ============================================================================

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// Assembly metadata — these populate the EXE's Windows file properties
// (right-click SAIGuard.exe -> Properties -> Details) and are the single
// source of truth for the version shown in the tray menu and About box.
// CI (.github/workflows/release.yml) rewrites the version here from the git tag.
[assembly: AssemblyTitle("SAI Guard")]
[assembly: AssemblyProduct("SAI Guard")]
[assembly: AssemblyCompany("Medialogic AI")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Medialogic AI")]
[assembly: AssemblyDescription("Keep your Windows machine awake.")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]
[assembly: AssemblyInformationalVersion("2.0.0")]

namespace SAIGuard
{
    static class Program
    {
        // ── Win32 API ──────────────────────────────────────────────────
        const uint ES_CONTINUOUS       = 0x80000000;
        const uint ES_SYSTEM_REQUIRED  = 0x00000001;
        const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint SetThreadExecutionState(uint esFlags);

        // ── State ──────────────────────────────────────────────────────
        static readonly uint FLAGS = ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED;
        static volatile bool guarding = true;
        static volatile bool running  = true;
        static NotifyIcon trayIcon;
        static MenuItem toggleItem;
        static MenuItem startupItem;
        static string logPath;
        static readonly string AppName = "SAIGuard";
        static readonly string Version = GetVersion();

        // ── Registry key for auto-start ────────────────────────────────
        static readonly string StartupRegKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        // Reads the version baked into the assembly (the [assembly: AssemblyVersion]
        // attribute above) and formats it as Major.Minor.Build, e.g. "2.0.0".
        // One source of truth: the tray, About box, and file properties never drift.
        static string GetVersion()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        // ── Entry point ────────────────────────────────────────────────
        [STAThread]
        static void Main(string[] args)
        {
            // Single instance check
            bool created;
            using (var mutex = new Mutex(true, "Global\\SAIGuard_SingleInstance", out created))
            {
                if (!created)
                {
                    // Already running
                    MessageBox.Show(
                        "SAI Guard is already running.\nCheck your system tray.",
                        "SAI Guard",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                SetupLogging();
                SetupTray();

                // Auto-register on first run
                if (!IsInStartup())
                {
                    SetStartup(true);
                    Log("First run - registered to start with Windows.");
                }

                // Start the guard thread
                var guardThread = new Thread(GuardLoop);
                guardThread.IsBackground = true;
                guardThread.Start();

                Log("════════════════════════════════════════════════════");
                Log("SAI Guard v" + Version + " started");
                Log("Host      : " + Environment.MachineName);
                Log("OS        : " + Environment.OSVersion);
                Log("Display   : kept on");
                Log("Startup   : " + (IsInStartup() ? "enabled" : "disabled"));
                Log("════════════════════════════════════════════════════");

                Application.Run();

                // Cleanup on exit
                running = false;
                guarding = false;
                SetThreadExecutionState(ES_CONTINUOUS);
                Log("SAI Guard stopped - execution state released.");
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
            }
        }

        // ── Guard loop ─────────────────────────────────────────────────
        static void GuardLoop()
        {
            int beats = 0;
            while (running)
            {
                if (guarding)
                {
                    uint result = SetThreadExecutionState(FLAGS);
                    beats++;

                    // Log a heartbeat every 10 minutes (20 × 30s)
                    if (beats % 20 == 0)
                    {
                        string status = result != 0 ? "OK" : "FAILED";
                        int mins = beats * 30 / 60;
                        Log("Heartbeat #" + beats + " - " + status + " - uptime " + mins + " min");
                    }
                }
                Thread.Sleep(30000);
            }
        }

        // ── System tray ────────────────────────────────────────────────
        static void SetupTray()
        {
            toggleItem = new MenuItem("Guarding: ON", OnToggleGuard);
            toggleItem.DefaultItem = true;

            startupItem = new MenuItem("Start with Windows", OnToggleStartup);
            startupItem.Checked = IsInStartup();

            var menu = new ContextMenu(new MenuItem[]
            {
                new MenuItem("SAI Guard v" + Version) { Enabled = false },
                new MenuItem(Environment.MachineName) { Enabled = false },
                new MenuItem("-"),
                toggleItem,
                startupItem,
                new MenuItem("-"),
                new MenuItem("Open log file", OnOpenLog),
                new MenuItem("About", OnAbout),
                new MenuItem("-"),
                new MenuItem("Exit", OnExit),
            });

            trayIcon = new NotifyIcon
            {
                Icon = CreateTrayIcon(true),
                Text = "SAI Guard - keeping this machine awake",
                ContextMenu = menu,
                Visible = true,
            };

            trayIcon.DoubleClick += (s, e) => OnToggleGuard(s, e);
        }

        // ── Tray icon (generated, no external file needed) ─────────────
        static Icon CreateTrayIcon(bool active)
        {
            int size = 64;
            using (var bmp = new Bitmap(size, size))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Shield shape
                Color shieldColor = active
                    ? Color.FromArgb(34, 197, 94)    // green = active
                    : Color.FromArgb(156, 163, 175);  // gray = paused

                // Shield body
                var path = new GraphicsPath();
                path.AddArc(4, 4, 20, 20, 180, 90);   // top-left
                path.AddArc(40, 4, 20, 20, 270, 90);   // top-right
                path.AddLine(60, 40, 32, 60);           // right side to bottom point
                path.AddLine(32, 60, 4, 40);            // bottom point to left side
                path.CloseFigure();

                using (var brush = new SolidBrush(shieldColor))
                    g.FillPath(brush, path);

                // "S" letter
                using (var font = new Font("Segoe UI", 28, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("S", font, textBrush, new RectangleF(0, -2, size, size), sf);
                }

                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        // ── Event handlers ─────────────────────────────────────────────
        static void OnToggleGuard(object sender, EventArgs e)
        {
            guarding = !guarding;

            if (guarding)
            {
                SetThreadExecutionState(FLAGS);
                toggleItem.Text = "Guarding: ON";
                trayIcon.Icon = CreateTrayIcon(true);
                trayIcon.Text = "SAI Guard - keeping this machine awake";
                Log("Guard RESUMED by user.");
            }
            else
            {
                SetThreadExecutionState(ES_CONTINUOUS); // release
                toggleItem.Text = "Guarding: OFF (paused)";
                trayIcon.Icon = CreateTrayIcon(false);
                trayIcon.Text = "SAI Guard - PAUSED";
                Log("Guard PAUSED by user.");
            }
        }

        static void OnToggleStartup(object sender, EventArgs e)
        {
            bool current = IsInStartup();
            SetStartup(!current);
            startupItem.Checked = !current;
            Log("Start with Windows: " + (!current ? "enabled" : "disabled"));
        }

        static void OnOpenLog(object sender, EventArgs e)
        {
            if (File.Exists(logPath))
            {
                try { System.Diagnostics.Process.Start("notepad.exe", logPath); }
                catch { }
            }
            else
            {
                MessageBox.Show("No log file yet.", "SAI Guard");
            }
        }

        static void OnAbout(object sender, EventArgs e)
        {
            MessageBox.Show(
                "SAI Guard v" + Version + "\n\n" +
                "Keeps your Windows machine awake using the Windows\n" +
                "SetThreadExecutionState API - no simulated input.\n\n" +
                
                "Source: github.com/krishnamallam/SAIGuard\n" +
                "License: MIT\n\n" +
                "© 2026 Medialogic AI",
                "About SAI Guard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        static void OnExit(object sender, EventArgs e)
        {
            running = false;
            guarding = false;
            SetThreadExecutionState(ES_CONTINUOUS);
            Log("SAI Guard exited by user.");
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            Application.Exit();
        }

        // ── Startup (registry) ─────────────────────────────────────────
        static string GetExePath()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        static bool IsInStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, false))
                {
                    if (key == null) return false;
                    var val = key.GetValue(AppName) as string;
                    return val != null;
                }
            }
            catch { return false; }
        }

        static void SetStartup(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, true))
                {
                    if (key == null) return;
                    if (enable)
                        key.SetValue(AppName, "\"" + GetExePath() + "\"");
                    else
                        key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                Log("Startup registry error: " + ex.Message);
            }
        }

        // ── Logging ────────────────────────────────────────────────────
        static void SetupLogging()
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logDir = Path.Combine(exeDir, "logs");
            try { Directory.CreateDirectory(logDir); } catch { }
            logPath = Path.Combine(logDir, "sai_guard.log");

            // Rotate if > 5 MB
            try
            {
                if (File.Exists(logPath) && new FileInfo(logPath).Length > 5 * 1024 * 1024)
                {
                    string backup = Path.Combine(logDir, "sai_guard.prev.log");
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(logPath, backup);
                }
            }
            catch { }
        }

        static void Log(string msg)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + msg;
            try
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch { }
        }
    }
}
