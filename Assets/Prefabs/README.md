# Prefabs

This folder contains reusable prefabs currently committed for player and environment objects.

## Current Layout

- `Player/Player.prefab` — the player object and its attached gameplay components.
- `Environment/` — platform prefabs and the ruin-pillar background prefab.
- Enemy prefabs are organised under `Assets/Enemies/`, rather than in `Assets/Prefabs/Enemies/`.

Unity `.meta` entries for `Collectibles`, `Enemies`, and `UI` remain in this folder, but those paths do not currently contain tracked prefab assets.

## Working with Prefabs

- Preserve the existing prefab and asset GUIDs when updating referenced assets.
- Prefer shared prefabs for objects that must behave consistently across levels.
- Test changes in the relevant scene, not only in prefab isolation; gameplay behaviour may depend on colliders, scripts, and scene references.
