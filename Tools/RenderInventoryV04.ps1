param(
    [string]$OutputDirectory = "outputs"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$textureRoot = Join-Path $projectRoot "Assets/Desktopirates/Resources/Textures/UI/ConceptV04/Inventory"
$referencePath = Join-Path $projectRoot "ArtSource/Generated/UI/v04-inventory/inventory_reference_v04.png"
$outputRoot = Join-Path $projectRoot $OutputDirectory
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

$canvas = [System.Drawing.Bitmap]::new(720, 760, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$fontName = "Consolas"
$items = @(
    @("food", "FOOD", "COMMON", "Keeps the crew fed at sea.", "x28", "#ADB8BA"),
    @("water", "WATER", "COMMON", "Keeps the crew hydrated.", "x19", "#ADB8BA"),
    @("supplies", "SUPPLIES", "UNCOMMON", "General stores for the voyage.", "x14", "#6ED134"),
    @("spare_cannon", "SPARE CANNONS", "UNCOMMON", "Unmounted cannon ready for a hardpoint.", "x3", "#6ED134"),
    @("timber", "TIMBER", "RARE", "Salt-worn planks for hull repairs.", "x26", "#14C7F0"),
    @("canvas", "CANVAS", "RARE", "Aged canvas for sails and rigging.", "x12", "#14C7F0"),
    @("iron", "IRON", "EPIC", "Fittings for reinforced ship parts.", "x9", "#DE40E0"),
    @("gear", "GEAR", "EPIC", "Mechanisms for engines and workshops.", "x6", "#DE40E0"),
    @("chart", "CHART", "LEGENDARY", "Fragments that reveal nearby waters.", "x4", "#FF9110"),
    @("relic", "RELIC", "LEGENDARY", "A rare keepsake from a lost voyage.", "x2", "#FF9110")
)

function Draw-Texture([System.Drawing.Graphics]$g, [string]$name, [System.Drawing.Rectangle]$destination) {
    $image = [System.Drawing.Image]::FromFile((Join-Path $textureRoot $name))
    try {
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $g.DrawImage($image, $destination)
    }
    finally { $image.Dispose() }
}

function Draw-Label([System.Drawing.Graphics]$g, [string]$value, [float]$size, [System.Drawing.Color]$color, [float]$x, [float]$y, [float]$width, [float]$height, [System.Drawing.StringAlignment]$alignment) {
    $font = [System.Drawing.Font]::new($fontName, $size, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = [System.Drawing.SolidBrush]::new($color)
    $format = [System.Drawing.StringFormat]::new()
    try {
        $format.Alignment = $alignment
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $format.Trimming = [System.Drawing.StringTrimming]::EllipsisCharacter
        $g.DrawString($value, $font, $brush, [System.Drawing.RectangleF]::new($x, $y, $width, $height), $format)
    }
    finally { $format.Dispose(); $brush.Dispose(); $font.Dispose() }
}

try {
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 4, 17, 25))
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit

    # Exact 720x760 reference-canvas geometry used by InventoryController.
    $panel = [System.Drawing.Rectangle]::new(102, 240, 516, 510)
    $fillBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 4, 18, 28))
    try { $graphics.FillRectangle($fillBrush, [System.Drawing.Rectangle]::new(116, 254, 488, 482)) } finally { $fillBrush.Dispose() }

    Draw-Texture $graphics "inventory_title_v04.png" ([System.Drawing.Rectangle]::new(200, 237, 320, 64))
    Draw-Texture $graphics "inventory_title_v04.png" ([System.Drawing.Rectangle]::new(111, 255, 76, 28))
    Draw-Label $graphics "INVENTORY" 24 ([System.Drawing.Color]::FromArgb(255, 255, 219, 145)) 216 245 288 48 ([System.Drawing.StringAlignment]::Center)
    Draw-Label $graphics "BACK" 11 ([System.Drawing.Color]::FromArgb(255, 235, 163, 56)) 116 258 66 22 ([System.Drawing.StringAlignment]::Center)
    Draw-Label $graphics "10 TYPES   •   123 ITEMS" 12 ([System.Drawing.Color]::FromArgb(255, 171, 191, 191)) 268 303 300 22 ([System.Drawing.StringAlignment]::Far)

    $viewport = [System.Drawing.Rectangle]::new(126, 336, 452, 411)
    $viewportBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 4, 12, 18))
    try { $graphics.FillRectangle($viewportBrush, $viewport) } finally { $viewportBrush.Dispose() }
    $oldClip = $graphics.Clip
    $graphics.SetClip($viewport)
    for ($index = 0; $index -lt $items.Count; $index++) {
        $rowTop = 344 + $index * 66
        $rowColor = if (($index % 2) -eq 0) { [System.Drawing.Color]::FromArgb(255, 6, 20, 27) } else { [System.Drawing.Color]::FromArgb(255, 8, 24, 31) }
        $rowBrush = [System.Drawing.SolidBrush]::new($rowColor)
        try { $graphics.FillRectangle($rowBrush, [System.Drawing.Rectangle]::new(138, $rowTop, 428, 62)) } finally { $rowBrush.Dispose() }
        $rarityColor = [System.Drawing.ColorTranslator]::FromHtml($items[$index][5])
        $rarityBrush = [System.Drawing.SolidBrush]::new($rarityColor)
        try { $graphics.FillRectangle($rarityBrush, [System.Drawing.Rectangle]::new(139, $rowTop + 3, 4, 56)) } finally { $rarityBrush.Dispose() }
        Draw-Texture $graphics ("inventory_{0}_v04.png" -f $items[$index][0]) ([System.Drawing.Rectangle]::new(151, $rowTop + 4, 54, 54))
        Draw-Label $graphics $items[$index][1] 16 ([System.Drawing.Color]::FromArgb(255, 240, 250, 245)) 216 ($rowTop + 3) 270 22 ([System.Drawing.StringAlignment]::Near)
        Draw-Label $graphics $items[$index][2] 10 $rarityColor 216 ($rowTop + 23) 270 16 ([System.Drawing.StringAlignment]::Near)
        Draw-Label $graphics $items[$index][3] 11 ([System.Drawing.Color]::FromArgb(255, 204, 217, 204)) 216 ($rowTop + 39) 270 18 ([System.Drawing.StringAlignment]::Near)
        Draw-Label $graphics $items[$index][4] 17 ([System.Drawing.Color]::FromArgb(255, 240, 250, 245)) 498 ($rowTop + 4) 62 24 ([System.Drawing.StringAlignment]::Far)
    }
    $graphics.Clip = $oldClip
    $oldClip.Dispose()

    Draw-Texture $graphics "inventory_scrollbar_track_v04.png" ([System.Drawing.Rectangle]::new(587, 336, 10, 411))
    Draw-Texture $graphics "inventory_scrollbar_handle_v04.png" ([System.Drawing.Rectangle]::new(588, 340, 8, 310))
    Draw-Texture $graphics "inventory_frame_v04.png" $panel

    $layoutPath = Join-Path $outputRoot "inventory-v04-layout-proof.png"
    $canvas.Save($layoutPath, [System.Drawing.Imaging.ImageFormat]::Png)

    $comparison = [System.Drawing.Bitmap]::new(1440, 760, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $comparisonGraphics = [System.Drawing.Graphics]::FromImage($comparison)
    try {
        $comparisonGraphics.Clear([System.Drawing.Color]::FromArgb(255, 3, 12, 18))
        $comparisonGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $reference = [System.Drawing.Image]::FromFile($referencePath)
        try { $comparisonGraphics.DrawImage($reference, [System.Drawing.Rectangle]::new(0, 20, 720, 720)) } finally { $reference.Dispose() }
        $comparisonGraphics.DrawImage($canvas, [System.Drawing.Rectangle]::new(720, 0, 720, 760))
        $comparisonPath = Join-Path $outputRoot "inventory-v04-reference-comparison.png"
        $comparison.Save($comparisonPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $comparisonGraphics.Dispose(); $comparison.Dispose() }

    Write-Output $layoutPath
    Write-Output $comparisonPath
}
finally { $graphics.Dispose(); $canvas.Dispose() }
