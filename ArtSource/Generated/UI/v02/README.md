# Concept UI v02 source

These four source atlases were generated from the approved circular-sea and pixel-voyage concept art. The `_alpha` files are chroma-key processed working sources.

Run `Tools/SliceGeneratedUiAtlases.py` with Pillow to rebuild the 70 Unity-ready textures under `Assets/Desktopirates/Resources/Textures/UI/ConceptV02`.

- `ui_chrome_atlas_*`: panel bezels, button frames, sliders and HUD chrome
- `ui_icons_atlas_*`: POI, menu, inventory and navigation icons
- `ui_port_systems_atlas_*`: port, upgrade, crew and gun-deck icons
- `ui_navigation_atlas_*`: time faces, telegraph parts and status icons

The final UI places live labels and values above these textures so generated pseudo-text never enters the game.
