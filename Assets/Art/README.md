# Art Assets

This folder holds project-specific visual assets used by Maze Runner. The current tracked files are concentrated in the environment and health-UI areas.

## Current Assets

- `Environment/Moon.png` — the moon artwork used by the sky setup.
- `Environment/SkyGradient.png` — the sky background gradient.
- `Environment/Spikes.png` — the spike hazard sprite.
- `Environment/Blender/MazeRunner_RuinPillar.png` — the ruin-pillar background artwork.
- `UI/Hearts/` — the heart sprite sheet and reusable heart prefab for the health display.

Player and enemy art is organised elsewhere under `Assets/`, including `Assets/Enemies/`; the raw source-pack material is kept separately in `ArtSource/`.

## Asset Handling

- Keep art original or properly licensed, and retain any source-pack licence or attribution requirements.
- Preserve the Unity `.meta` files so asset GUID references remain stable.
- Check import settings and sprite pivots before changing an asset used by scenes or prefabs.
- The `Characters.meta` and `Enemies.meta` entries under this folder do not currently have tracked child assets; the active player/enemy art is not stored in those paths.
