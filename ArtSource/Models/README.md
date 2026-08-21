# Model source layout

- `Blender/Ships`: editable `.blend` sources for the six player tiers and enemy base hull.
- `Blender/World`: editable `.blend` sources for harbor, wreck, and treasure landmarks.
- `Blender/Bosses`: editable `.blend` sources for the gang admiral, ghost ship, kraken, and Poseidon.
- Unity-ready FBX exports live under `Assets/Desktopirates/Resources/Models` with the same `Ships` and `World` split.
- Runtime textures live under `Assets/Desktopirates/Resources/Textures/Models/v02` and are applied by object role after prefab loading.
- Regenerate all source and FBX files with `Tools/Blender/GenerateLowPolyWorld.py`; no Blender add-on is required.

The geometry intentionally stays simple. Readability, silhouette, authored scale, and pixel-textured surfaces provide most of the final visual character.
