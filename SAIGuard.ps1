# SAI Guard - PowerShell version
# Run with: powershell -ExecutionPolicy Bypass -File SAIGuard.ps1
# No build needed. Works on any Windows machine.

$code = @"
    using System;
    using System.Runtime.InteropServices;
    public class SAI {
        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint f);
    }
"@

Add-Type -TypeDefinition $code

# ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED
$flags = [uint32]2147483651

Write-Host ""
Write-Host "  SAI Guard (PowerShell) - keeping this machine awake" -ForegroundColor Green
Write-Host "  Host: $env:COMPUTERNAME"
Write-Host "  Press Ctrl+C to stop."
Write-Host ""

while ($true) {
    try {
        [SAI]::SetThreadExecutionState($flags) | Out-Null
        $ts = Get-Date -Format "HH:mm:ss"
        Write-Host "$ts | heartbeat OK" -ForegroundColor DarkGray
    } catch {
        Write-Host "Warning: SetThreadExecutionState failed" -ForegroundColor Yellow
    }
    Start-Sleep -Seconds 30
}
