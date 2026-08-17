"""Derive frameless port glyphs from the authored v02 icons without repainting them."""

from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/Desktopirates/Resources/Textures/UI/ConceptV02/Port"
OUTPUT = ROOT / "Assets/Desktopirates/Resources/Textures/UI/ConceptV02/PortFrameless"

# The medallions use two radii. Masking outside these measured inner edges removes
# the ring and presentation artifacts while retaining the central pictogram.
GLYPH_RADIUS = {
    "repair": 32,
    "food": 42,
    "water": 42,
    "propulsion": 32,
    "shipyard": 34,
    "sail": 33,
    "hire_crew": 33,
}


def is_flood_background(pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, alpha = pixel
    dark_navy = red < 46 and green < 82 and blue < 112
    return alpha < 12 or dark_navy


def clear_connected_background(image: Image.Image) -> Image.Image:
    pixels = image.load()
    width, height = image.size
    queue: deque[tuple[int, int]] = deque()
    visited: set[tuple[int, int]] = set()
    for x in range(width):
        queue.append((x, 0)); queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y)); queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        if x < 0 or y < 0 or x >= width or y >= height or (x, y) in visited:
            continue
        visited.add((x, y))
        if not is_flood_background(pixels[x, y]):
            continue
        red, green, blue, _ = pixels[x, y]
        pixels[x, y] = (red, green, blue, 0)
        queue.extend(((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))
    return image


def build_icon(name: str, radius: int) -> None:
    source_path = SOURCE / f"{name}_v02.png"
    image = Image.open(source_path).convert("RGBA")
    pixels = image.load()
    center_x = (image.width - 1) * 0.5
    center_y = (image.height - 1) * 0.5
    for y in range(image.height):
        for x in range(image.width):
            if (x - center_x) ** 2 + (y - center_y) ** 2 > radius ** 2:
                red, green, blue, _ = pixels[x, y]
                pixels[x, y] = (red, green, blue, 0)
    image = clear_connected_background(image)
    bounds = image.getbbox()
    if bounds is None:
        raise RuntimeError(f"Frameless extraction removed all pixels from {name}")
    glyph = image.crop(bounds)
    scale = min(108 / glyph.width, 108 / glyph.height)
    resized = glyph.resize((max(1, round(glyph.width * scale)), max(1, round(glyph.height * scale))), Image.Resampling.NEAREST)
    output = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    output.alpha_composite(resized, ((128 - resized.width) // 2, (128 - resized.height) // 2))
    output.save(OUTPUT / f"{name}_v02.png", optimize=True)


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, radius in GLYPH_RADIUS.items():
        build_icon(name, radius)
    print(f"Built {len(GLYPH_RADIUS)} frameless port icons in {OUTPUT}")


if __name__ == "__main__":
    main()
