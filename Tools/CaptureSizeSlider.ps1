param(
    [string]$Executable = (Join-Path $PSScriptRoot '..\Builds\Windows\desktopirates.exe'),
    [string]$BeforeOutput = (Join-Path $PSScriptRoot '..\SizeSlider-BeforeRelease.png'),
    [string]$AfterOutput = (Join-Path $PSScriptRoot '..\SizeSlider-AfterRelease.png')
)

Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class DesktopiratesSizeTestNative {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
'@

[DesktopiratesSizeTestNative]::SetProcessDPIAware() | Out-Null

function Save-Window([IntPtr]$Handle, [string]$Path) {
    $rect = New-Object DesktopiratesSizeTestNative+RECT
    [DesktopiratesSizeTestNative]::GetWindowRect($Handle, [ref]$rect) | Out-Null
    $bitmap = New-Object Drawing.Bitmap ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $hdc = $graphics.GetHdc()
        try { [DesktopiratesSizeTestNative]::PrintWindow($Handle, $hdc, 0) | Out-Null }
        finally { $graphics.ReleaseHdc($hdc) }
        $bitmap.Save([IO.Path]::GetFullPath($Path), [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}

function Click([int]$X, [int]$Y) {
    [DesktopiratesSizeTestNative]::SetCursorPos($X, $Y) | Out-Null
    [DesktopiratesSizeTestNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 100
    [DesktopiratesSizeTestNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}

$game = Start-Process ([IO.Path]::GetFullPath($Executable)) -PassThru
try {
    for ($i = 0; $i -lt 60 -and $game.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 200; $game.Refresh() }
    if ($game.MainWindowHandle -eq 0) { throw 'desktopirates window did not appear.' }
    [DesktopiratesSizeTestNative]::SetForegroundWindow($game.MainWindowHandle) | Out-Null
    Start-Sleep -Seconds 4

    $rect = New-Object DesktopiratesSizeTestNative+RECT
    [DesktopiratesSizeTestNative]::GetWindowRect($game.MainWindowHandle, [ref]$rect) | Out-Null
    $oldWidth = $rect.Right - $rect.Left
    $oldHeight = $rect.Bottom - $rect.Top
    $orbX = $rect.Left + [int]($oldWidth * 0.5)
    $orbY = $rect.Top + [int]($oldHeight * (66.0 / 760.0))
    Click $orbX $orbY
    Start-Sleep -Milliseconds 700

    [DesktopiratesSizeTestNative]::GetWindowRect($game.MainWindowHandle, [ref]$rect) | Out-Null
    $barStartX = $rect.Left + [int](($rect.Right - $rect.Left) * 0.302)
    $barY = $rect.Top + [int](($rect.Bottom - $rect.Top) * (191.0 / 760.0))
    $barEndX = $barStartX
    [DesktopiratesSizeTestNative]::SetCursorPos($barStartX, $barY) | Out-Null
    [DesktopiratesSizeTestNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [DesktopiratesSizeTestNative]::SetCursorPos($barEndX, $barY) | Out-Null
    Start-Sleep -Milliseconds 600
    Save-Window $game.MainWindowHandle $BeforeOutput

    $during = New-Object DesktopiratesSizeTestNative+RECT
    [DesktopiratesSizeTestNative]::GetWindowRect($game.MainWindowHandle, [ref]$during) | Out-Null
    [DesktopiratesSizeTestNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Seconds 2
    $after = New-Object DesktopiratesSizeTestNative+RECT
    [DesktopiratesSizeTestNative]::GetWindowRect($game.MainWindowHandle, [ref]$after) | Out-Null
    Save-Window $game.MainWindowHandle $AfterOutput

    [pscustomobject]@{
        BeforeWidth = $during.Right - $during.Left
        BeforeHeight = $during.Bottom - $during.Top
        BeforeOrbX = $during.Left + [int](($during.Right - $during.Left) * 0.5)
        BeforeOrbY = $during.Top + [int](($during.Bottom - $during.Top) * (66.0 / 760.0))
        AfterWidth = $after.Right - $after.Left
        AfterHeight = $after.Bottom - $after.Top
        AfterOrbX = $after.Left + [int](($after.Right - $after.Left) * 0.5)
        AfterOrbY = $after.Top + [int](($after.Bottom - $after.Top) * (66.0 / 760.0))
    } | Format-List
}
finally {
    if ($game -and -not $game.HasExited) { $game.CloseMainWindow() | Out-Null; Start-Sleep -Milliseconds 500; if (-not $game.HasExited) { $game.Kill() } }
}
