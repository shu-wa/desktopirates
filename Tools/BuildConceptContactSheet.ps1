param(
    [string]$SourceDirectory = "ArtSource/ConceptArt/2026-08-21-complete-set",
    [string]$OutputPath = "outputs/concept-art-2026-08-21-contact-sheet.png"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $projectRoot $SourceDirectory
$destination = Join-Path $projectRoot $OutputPath
$destinationDirectory = Split-Path -Parent $destination
New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null

$files = Get-ChildItem -LiteralPath $sourceRoot -Filter "*.png" | Sort-Object Name
$columns = 4
$cellWidth = 360
$cellHeight = 260
$headerHeight = 70
$rows = [Math]::Ceiling($files.Count / $columns)
$sheet = [System.Drawing.Bitmap]::new($columns * $cellWidth, $headerHeight + $rows * $cellHeight)
$graphics = [System.Drawing.Graphics]::FromImage($sheet)
$titleFont = [System.Drawing.Font]::new("Consolas", 28, [System.Drawing.FontStyle]::Bold)
$labelFont = [System.Drawing.Font]::new("Consolas", 13, [System.Drawing.FontStyle]::Bold)
$titleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 245, 211, 145))
$labelBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 225, 232, 222))
$cellBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 6, 20, 28))

try {
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 2, 9, 14))
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawString("desktopirates — complete scene concept set", $titleFont, $titleBrush, 24, 17)

    for ($index = 0; $index -lt $files.Count; $index++) {
        $column = $index % $columns
        $row = [Math]::Floor($index / $columns)
        $cellX = $column * $cellWidth
        $cellY = $headerHeight + $row * $cellHeight
        $graphics.FillRectangle($cellBrush, $cellX + 5, $cellY + 5, $cellWidth - 10, $cellHeight - 10)

        $image = [System.Drawing.Image]::FromFile($files[$index].FullName)
        try {
            $availableWidth = $cellWidth - 18
            $availableHeight = $cellHeight - 48
            $scale = [Math]::Min($availableWidth / $image.Width, $availableHeight / $image.Height)
            $drawWidth = [int]($image.Width * $scale)
            $drawHeight = [int]($image.Height * $scale)
            $drawX = $cellX + [int](($cellWidth - $drawWidth) / 2)
            $drawY = $cellY + 9 + [int](($availableHeight - $drawHeight) / 2)
            $graphics.DrawImage($image, $drawX, $drawY, $drawWidth, $drawHeight)
        }
        finally { $image.Dispose() }

        $label = [System.IO.Path]::GetFileNameWithoutExtension($files[$index].Name)
        $graphics.DrawString($label, $labelFont, $labelBrush, $cellX + 12, $cellY + $cellHeight - 34)
    }

    $sheet.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output $destination
}
finally {
    $cellBrush.Dispose()
    $labelBrush.Dispose()
    $titleBrush.Dispose()
    $labelFont.Dispose()
    $titleFont.Dispose()
    $graphics.Dispose()
    $sheet.Dispose()
}
