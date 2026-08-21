# Authored low-poly model pipeline

## Runtime presentation

- Camera orthographic size is `6.00`, so the 12.5-unit ocean disc fills the vertical play area.
- Player ships, enemies, wrecks, and treasure use explicit readability multipliers from `WorldPresentationMetrics`.
- The harbor uses a smaller multiplier because its authored FBX footprint is already several times larger than a ship.
- Models are loaded from `Resources/Models`; the original procedural builders remain as a safe fallback only.

## Directory ownership

- `ArtSource/Models/Blender/Ships`: editable Blender sources for six player tiers and the enemy base ship.
- `ArtSource/Models/Blender/World`: editable Blender sources for harbor, wreck, and treasure.
- `ArtSource/Models/Blender/Bosses`: editable Blender sources for all four bosses.
- `Assets/Desktopirates/Resources/Models/Ships`: Unity-ready FBX ship models.
- `Assets/Desktopirates/Resources/Models/World`: Unity-ready FBX landmark models.
- `Assets/Desktopirates/Resources/Textures/Models/v02`: generated pixel-material textures applied by semantic part names.
- `Tools/Blender/GenerateLowPolyWorld.py`: deterministic regeneration and FBX export script.

No Blender add-on is required. Blender 5.0 creates simple flat-shaded geometry, UV unwraps it, saves compressed `.blend` sources, and exports FBX with animation, cameras, lights, and embedded materials disabled in Unity.

## Verification

- Unity EditMode verifies all ten FBX resources exist.
- Runtime-instantiation tests create the player, enemy, and harbor models and confirm they contain renderers.
- Texture tests require point filtering and repeat wrapping.
- Windows build and internal runtime captures cover docking, the enemy gallery, and the large-ship tier.
