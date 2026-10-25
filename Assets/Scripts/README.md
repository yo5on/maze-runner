# Scripts

Gameplay and interface code is organised by system. The main runtime systems currently tracked here are:

- **Audio/** — audio manager and per-channel volume sources.
- **Camera/** — camera-follow behaviour.
- **Collectibles/** — collectible interaction.
- **Customization/** — character definitions, rendering, and player-skin configuration.
- **Enemies/** — enemy state and combat support, with specialised AI under **Enemies/Behaviours/**. This includes regular enemy behaviours and the Ogre Warlord, Plague Alchemist, and Goblin Warchief boss behaviours.
- **Levels/** — checkpoints, hazards, goals, enemy spawning/tracking, moving platforms, sky/moon behaviour, and level progression.
- **Player/** — movement, animation, combat, and health.
- **SaveSystem/** — serialisable customization data.
- **Settings/** — saved audio and mobile-control preferences.
- **Testing/** — development-only AI test probes.
- **UI/** — menus, HUD, health display, pause/settings, level-complete and game-over flows, and the WebGL bridge.

## Conventions

- Use PascalCase for C# types and filenames.
- Keep each main class in a matching `.cs` file.
- Keep Inspector-exposed implementation details private and mark them with `[SerializeField]` when needed.
- Preserve Unity `.meta` files and GUID references when moving assets.
- Update scene/prefab wiring and the root README when a code change alters player-facing behaviour or setup instructions.
