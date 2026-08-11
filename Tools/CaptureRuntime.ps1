param(
    [string]$Executable = (Join-Path $PSScriptRoot '..\Builds\Windows\desktopirates.exe'),
    [string]$NightOutput = (Join-Path $PSScriptRoot '..\RuntimeCapture-Night.png'),
    [string]$DayOutput = (Join-Path $PSScriptRoot '..\RuntimeCapture-Day.png')
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class DesktopiratesCaptureNative {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

function Save-WindowCapture([IntPtr]$Handle, [string]$Path) {
    $rect = New-Object DesktopiratesCaptureNative+RECT
    [DesktopiratesCaptureNative]::GetWindowRect($Handle, [ref]$rect) | Out-Null
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = New-Object Drawing.Bitmap $width, $height
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $hdc = $graphics.GetHdc()
        try {
            if (-not [DesktopiratesCaptureNative]::PrintWindow($Handle, $hdc, 0)) {
                throw 'PrintWindow failed.'
            }
        }
        finally { $graphics.ReleaseHdc($hdc) }
        $bitmap.Save([IO.Path]::GetFullPath($Path), [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}

$backdrop = New-Object Windows.Forms.Form
$backdrop.FormBorderStyle = [Windows.Forms.FormBorderStyle]::None
$backdrop.Bounds = [Windows.Forms.Screen]::PrimaryScreen.Bounds
$backdrop.BackColor = [Drawing.Color]::FromArgb(2, 10, 24)
$backdrop.TopMost = $true
$backdrop.ShowInTaskbar = $false
$backdrop.Show()
[Windows.Forms.Application]::DoEvents()

$game = $null
try {
    $game = Start-Process ([IO.Path]::GetFullPath($Executable)) -PassThru
    for ($i = 0; $i -lt 60 -and $game.MainWindowHandle -eq 0; $i++) {
        Start-Sleep -Milliseconds 200
        $game.Refresh()
        [Windows.Forms.Application]::DoEvents()
    }
    if ($game.MainWindowHandle -eq 0) { throw 'desktopirates window did not appear.' }

    $topMost = [IntPtr](-1)
    [DesktopiratesCaptureNative]::SetWindowPos($game.MainWindowHandle, $topMost, 40, 40, 0, 0, 0x0041) | Out-Null
    [DesktopiratesCaptureNative]::SetForegroundWindow($game.MainWindowHandle) | Out-Null
    Start-Sleep -Seconds 5

    # Click the Menu Circle at the top-center of the 720 x 760 overlay.
    $rect = New-Object DesktopiratesCaptureNative+RECT
    [DesktopiratesCaptureNative]::GetWindowRect($game.MainWindowHandle, [ref]$rect) | Out-Null
    $clickX = [int](($rect.Right - $rect.Left) * 0.5)
    $clickY = [int](($rect.Bottom - $rect.Top) * (66.0 / 760.0))
    $clickPosition = [IntPtr](($clickY -shl 16) -bor $clickX)
    [DesktopiratesCaptureNative]::PostMessage($game.MainWindowHandle, 0x0201, [IntPtr]1, $clickPosition) | Out-Null
    [DesktopiratesCaptureNative]::PostMessage($game.MainWindowHandle, 0x0202, [IntPtr]0, $clickPosition) | Out-Null
    Start-Sleep -Milliseconds 700
    Save-WindowCapture $game.MainWindowHandle $NightOutput

    # F2 advances six hours. Twice moves the night verification view to day.
    1..2 | ForEach-Object {
        [DesktopiratesCaptureNative]::PostMessage($game.MainWindowHandle, 0x0100, [IntPtr]0x71, [IntPtr]0) | Out-Null
        [DesktopiratesCaptureNative]::PostMessage($game.MainWindowHandle, 0x0101, [IntPtr]0x71, [IntPtr]0) | Out-Null
        Start-Sleep -Milliseconds 250
    }
    Save-WindowCapture $game.MainWindowHandle $DayOutput
}
finally {
    if ($game -and -not $game.HasExited) { $game.CloseMainWindow() | Out-Null; Start-Sleep -Milliseconds 500; if (-not $game.HasExited) { $game.Kill() } }
    $backdrop.Close()
    $backdrop.Dispose()
}
