"""Reproducibly slice approved ImageGen UI atlases into Unity-ready alpha PNGs."""

from collections import deque
from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "Generated" / "UI" / "v02"
OUTPUT = ROOT / "Assets" / "Desktopirates" / "Resources" / "Textures" / "UI" / "ConceptV02"


def entry(name: str, width: int, height: int):
    return name, (width, height)


ATLASES = (
    (
        "ui_chrome_atlas_alpha_v02.png", 4, 4, "Chrome", True,
        (
            entry("menu_circle_frame", 128, 128), entry("cargo_panel_frame", 384, 384),
            entry("port_panel_frame", 384, 384), entry("shipyard_panel_frame", 512, 512),
            entry("map_panel_frame", 512, 512), entry("hud_dashboard_frame", 512, 128),
            entry("notification_pill", 512, 80), entry("service_button", 512, 112),
            entry("item_slot_frame", 128, 128), entry("cannon_hardpoint_frame", 128, 128),
            entry("tab_frame", 320, 80), entry("slider_rail", 512, 64),
            entry("slider_knob", 64, 64), entry("compass_arc", 512, 192),
            entry("radial_hub", 160, 160), entry("tooltip_card", 384, 256),
        ),
    ),
    (
        "ui_icons_atlas_alpha_v02.png", 5, 4, "Icons", False,
        tuple(entry(name, 128, 128) for name in (
            "marker_enemy", "marker_wreck", "marker_treasure", "marker_port", "marker_player",
            "menu_map", "menu_inventory", "menu_save", "menu_exit", "menu_back",
            "menu_volume", "menu_size", "item_timber", "item_canvas", "item_iron",
            "item_gear", "item_chart", "item_relic", "camera_rotate", "auto_dock",
        )),
    ),
    (
        "ui_port_systems_atlas_alpha_v02.png", 5, 4, "Port", False,
        tuple(entry(name, 128, 128) for name in (
            "repair", "supplies", "propulsion", "shipyard", "sail",
            "capacity", "engine_upgrade", "armor", "turning", "gun_upgrade",
            "hire_crew", "hardpoint_empty", "hardpoint_cannon", "hull_schematic", "gun_deck",
            "systems", "telegraph_stop", "telegraph_slow", "telegraph_half", "telegraph_full",
        )),
    ),
    (
        "ui_navigation_atlas_alpha_v02.png", 4, 3, "Navigation", False,
        (
            entry("time_morning", 128, 128), entry("time_noon", 128, 128),
            entry("time_evening", 128, 128), entry("time_night", 128, 128),
            entry("speed_inactive", 64, 64), entry("speed_active", 64, 64),
            entry("speed_needle", 64, 128), entry("compass_north", 128, 128),
            entry("status_hull", 96, 96), entry("status_gold", 96, 96),
            entry("status_crew", 96, 96), entry("status_load", 96, 96),
        ),
    ),
)


def remove_near_white(image: Image.Image) -> Image.Image:
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            border = x < 8 or y < 8 or x >= image.width - 8 or y >= image.height - 8
            near_white = min(red, green, blue) >= 220 and max(red, green, blue) - min(red, green, blue) <= 12
            if alpha and (border or near_white):
                pixels[x, y] = (red, green, blue, 0)
    return image


def keep_largest_component(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    width, height = image.size
    visible = bytearray(1 if value > 24 else 0 for value in alpha.getdata())
    visited = bytearray(width * height)
    largest = []
    for start in range(width * height):
        if not visible[start] or visited[start]:
            continue
        component = []
        queue = deque((start,))
        visited[start] = 1
        while queue:
            index = queue.popleft()
            component.append(index)
            x, y = index % width, index // width
            for neighbor in (index - 1, index + 1, index - width, index + width):
                if neighbor < 0 or neighbor >= width * height or visited[neighbor] or not visible[neighbor]:
                    continue
                nx, ny = neighbor % width, neighbor // width
                if abs(nx - x) + abs(ny - y) != 1:
                    continue
                visited[neighbor] = 1
                queue.append(neighbor)
        if len(component) > len(largest):
            largest = component
    keep = bytearray(width * height)
    for index in largest:
        keep[index] = 1
    pixels = image.load()
    for index, allowed in enumerate(keep):
        if not allowed:
            x, y = index % width, index // width
            red, green, blue, _ = pixels[x, y]
            pixels[x, y] = (red, green, blue, 0)
    return image


def trim_and_fit(image: Image.Image, size: tuple[int, int], padding: int = 4, center_fifth: bool = False) -> Image.Image:
    alpha = image.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise RuntimeError("Generated atlas cell contains no visible pixels")
    image = image.crop(bounds)
    if center_fifth:
        fifth = image.width / 5
        image = image.crop((round(fifth * 2), 0, round(fifth * 3), image.height))
    target_width, target_height = size
    inner_width = max(1, target_width - padding * 2)
    inner_height = max(1, target_height - padding * 2)
    scale = min(inner_width / image.width, inner_height / image.height)
    fitted = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.NEAREST,
    )
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(fitted, ((target_width - fitted.width) // 2, (target_height - fitted.height) // 2))
    return canvas


def slice_atlas(source_name, columns, rows, folder, clean_white, entries):
    source = Image.open(SOURCE / source_name).convert("RGBA")
    destination = OUTPUT / folder
    destination.mkdir(parents=True, exist_ok=True)
    if len(entries) != columns * rows:
        raise ValueError(f"{source_name}: grid and entry count differ")
    for index, (name, size) in enumerate(entries):
        column, row = index % columns, index // columns
        left = round(column * source.width / columns)
        right = round((column + 1) * source.width / columns)
        top = round(row * source.height / rows)
        bottom = round((row + 1) * source.height / rows)
        cell = source.crop((left, top, right, bottom))
        if clean_white:
            cell = keep_largest_component(remove_near_white(cell))
        result = trim_and_fit(cell, size, center_fifth=name in ("speed_inactive", "speed_active"))
        result.save(destination / f"{name}_v02.png", optimize=True)


def main():
    for atlas in ATLASES:
        slice_atlas(*atlas)
    surface_path = ROOT / "Assets" / "Desktopirates" / "Resources" / "Textures" / "UI" / "Surfaces" / "hud_chartwood_navy_v01.png"
    surface = Image.open(surface_path).convert("RGBA").resize((512, 512), Image.Resampling.NEAREST)
    mask = Image.new("L", (512, 512), 0)
    ImageDraw.Draw(mask).ellipse((8, 8, 503, 503), fill=255)
    surface.putalpha(mask)
    (OUTPUT / "Chrome").mkdir(parents=True, exist_ok=True)
    surface.save(OUTPUT / "Chrome" / "panel_fill_v02.png", optimize=True)
    button = Image.open(surface_path).convert("RGBA").resize((512, 112), Image.Resampling.NEAREST)
    button_mask = Image.new("L", button.size, 0)
    ImageDraw.Draw(button_mask).rounded_rectangle((3, 3, 508, 108), radius=30, fill=255)
    button.putalpha(button_mask)
    button.save(OUTPUT / "Chrome" / "button_fill_v02.png", optimize=True)
    print(f"Wrote {2 + sum(len(atlas[-1]) for atlas in ATLASES)} UI textures to {OUTPUT}")


if __name__ == "__main__":
    main()
