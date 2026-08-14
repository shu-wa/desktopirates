# Live chart, harbor UI, salvos, and ranked perks

## Infinite world and chart contract

- Every new voyage receives a random 32-bit world seed. A saved seed recreates the same world without storing terrain or landmark rows.
- `WorldGenerator` derives each 18-unit chunk independently from `(seed, chunkX, chunkY)`, so generation remains random, deterministic, and effectively unbounded in every direction.
- The chart and physical world use the same generated event IDs. Resolved IDs are removed immediately; loaded moving enemies use their live transform instead of their original spawn point.
- The ship marker stays at chart center. The chart texture and all landmark markers move around it while sailing.
- Each of the four bosses has a guaranteed introductory chunk. Every distant 12x12 sector also owns one deterministic boss anchor so boss farming never runs out.

## Readability pass

- HUD cards have separated brass frames, larger numeric safe areas, and bar meters for hull and load.
- The permanent decorative stern wake was removed. Only movement-driven water feedback remains.
- Inventory uses the cargo frame and a centered scrolling list.
- Harbor services use authored repair, food, water, propulsion, and shipyard icons with short value labels.

## Cannon salvo contract

- All installed, crewed cannons whose firing arcs contain the target launch on the same frame.
- One crew member is required per firing cannon.
- Damage is calculated per cannon, summed into one salvo, and followed by one shared reload cooldown.

## Perk ranks and boss drops

| Rank | Effect weight | Base status chance | Share among perk drops |
|---|---:|---:|---:|
| R1 | 1.00x | 20% | 72% |
| R2 | 1.45x | 38% | 20% |
| R3 | 2.05x | 62% | 7% |
| R4 | 2.85x | 86% | 1% |

The boss first rolls the existing 42% chance to drop a perk, then rolls this rank table. Duplicate ranked perks still stack within the crew slots assigned to that role. Poisoned crew performance reduces the effective status proc chance as well as numeric perk benefits.
