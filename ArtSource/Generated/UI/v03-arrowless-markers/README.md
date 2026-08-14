# Arrowless landmark markers v03

These four marker sources were edited from the approved ConceptV02 markers with the built-in image generation tool. The only requested design change was to remove the outward triangular pointer and close the colored rim into a centered circle.

Prompt constraints shared by all four edits:

- preserve the central subject, navy interior, palette, lighting and chunky pixel-art identity
- remove every arrow, pointer, tab, tail and ribbon
- use a closed circular rim on a flat `#ff00ff` chroma-key background
- no text, shadow or watermark

The `_alpha` images were produced with the imagegen skill's `remove_chroma_key.py` helper. Run `Tools/PrepareArrowlessMarkers.py` with Pillow to crop, nearest-neighbor resize and install the four 128 px runtime textures over their explicitly replaced ConceptV02 counterparts.
