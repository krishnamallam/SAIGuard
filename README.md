# SAI Guard

**Keep your Windows machine awake. Click to run. That's it.**

<p align="center">
  <img src="assets/screenshot.png" alt="SAI Guard in the system tray" width="320">
</p>

SAI Guard prevents your Windows PC from going to sleep, locking the screen, or activating the screensaver - without simulating keystrokes or mouse movements.

Perfect for:
- 🖥️ **Virtual machines** running unattended automation (RPA, CI/CD agents, test runners)
- 📺 **Presentations** and demos
- ⬇️ **Long downloads** or uploads
- 🔧 **Remote sessions** that you don't want to disconnect
- 🤖 **Any machine** that needs to stay awake

## Download

➡️ **[Download SAIGuard.exe](https://github.com/MedialogicAI/SAIGuard/releases/latest)** - single file, no installer, no dependencies.

Just double-click it. A green shield appears in your system tray. Done.

## How it works

SAI Guard calls the Windows [`SetThreadExecutionState`](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate) API every 30 seconds with these flags:

| Flag | What it does |
|------|-------------|
| `ES_CONTINUOUS` | Keeps the state until explicitly cleared |
| `ES_SYSTEM_REQUIRED` | Prevents sleep and hibernate |
| `ES_DISPLAY_REQUIRED` | Keeps the display on, prevents screen lock |

**No fake keystrokes. No mouse jiggling. No interference with automation or testing.**

This is the same proven API used by Microsoft PowerToys Awake, Caffeine, and similar tools.

## Features

- **Click to run** - double-click the `.exe`, it works immediately
- **System tray** - green shield icon, right-click menu to pause/resume
- **Auto-start** - automatically registers to start with Windows on first run
- **Single instance** - won't launch duplicates
- **Logging** - heartbeat log file to verify it's running
- **Pause/resume** - toggle from the tray without closing
- **Zero dependencies** - runs on any Windows 7/8/10/11/Server, 32 or 64 bit
- **Tiny** - under 10 KB
- **Open source** - MIT license

## System tray menu

| Option | Description |
|--------|-------------|
| **Guarding: ON** | Click to pause/resume (or double-click the icon) |
| **Start with Windows** | Toggle auto-start on boot |
| **Open log file** | View the heartbeat log |
| **About** | Version info |
| **Exit** | Stop guarding and quit |

## Building from source

No SDK or toolchain required. Windows ships with a C# compiler.

```
git clone https://github.com/MedialogicAI/SAIGuard.git
cd SAIGuard
build.bat
```

That's it. `build.bat` uses `csc.exe` from .NET Framework 4.x (built into every Windows since Vista). The output is a single `SAIGuard.exe`.

### Manual build

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
    /out:SAIGuard.exe /target:winexe /optimize+ ^
    /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
    SAIGuard.cs
```

## Deploying to VMs at scale

For unattended VMs (RPA, CI agents, etc.):

1. Copy `SAIGuard.exe` to the VM
2. Double-click it once - it auto-registers for startup
3. That's it. It survives reboots.

Or deploy silently:

```cmd
copy SAIGuard.exe C:\SAIGuard\
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v SAIGuard /d "\"C:\SAIGuard\SAIGuard.exe\"" /f
start "" "C:\SAIGuard\SAIGuard.exe"
```

### Verify it's running

```cmd
tasklist | findstr SAIGuard
type "%~dp0logs\sai_guard.log"
```

## FAQ

**Does it work with RDP?**
Yes. The API call works regardless of how you're connected.

**Does it interfere with AutoMate / Selenium / Playwright?**
No. It uses a Windows API flag - no simulated input. Completely safe for any automation.

**Will antivirus flag it?**
Possibly, since it's a small unsigned executable that modifies startup. Add an exception or sign it with your org certificate.

**Can I run it as a Windows Service instead?**
The `.exe` is a GUI app (system tray). For service mode, wrap it with [NSSM](https://nssm.cc) or use Task Scheduler with "Run whether user is logged on or not".

**What's the difference vs PowerToys Awake?**
SAI Guard is a single 10KB file with no installer and no dependencies. PowerToys is a 200MB+ suite. If all you need is keep-awake, SAI Guard is simpler.

## License

MIT - see [LICENSE](LICENSE)

## Credits

Built by [Medialogic AI](https://medialogic.ai). Inspired by [kyleleong/caffeine](https://github.com/kyleleong/caffeine).
