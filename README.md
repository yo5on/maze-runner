# Maze Runner

Maze Runner is a 2D side-scrolling platform game built with Unity. The repository currently contains three gameplay levels, menu and customization scenes, player movement and combat systems, enemies, checkpoints, level goals, and supporting UI and audio.

## Project status

The project is an active college project prototype. Its current source includes implemented game systems and authored Unity scenes; the repository does not include a release build or a complete testing record. Descriptions of implemented systems below are based on the current source, scene, prefab, and project configuration files. A script existing in the project does not by itself guarantee it is used in every scene.

Recent history (September 2026) records work on enemy animations, level audio, enemy fall death, a one-respawn game-over flow, and a three-heart HUD. Older root notes describe an earlier phase and conflict with the current scenes and packages, so they should not be treated as the present project specification.

## Requirements

- Unity **6000.5.1f1** (recorded in `ProjectSettings/ProjectVersion.txt`)
- Unity Package Manager dependencies as recorded in `Packages/manifest.json`
- A desktop platform supported by the installed Unity Editor

The project uses Universal Render Pipeline (URP) with 2D renderer assets and the New Input System. Cinemachine is not listed in the current package manifest; the project contains its own `CameraFollow` script.

## Open the project

1. Install or open Unity Hub and add this repository as a project.
2. Select Unity **6000.5.1f1** when prompted.
3. Allow Unity Package Manager to resolve the packages in `Packages/manifest.json` and the editor to import assets.
4. Open `Assets/Scenes/MainMenu.unity` to explore the menu, or open one of the gameplay scenes under `Assets/Scenes/Levels/`.
5. Use the Unity Editor Play control to run the selected scene. The repository has no checked-in standalone build instructions or prebuilt executable.

All six intended application scenes are enabled in `ProjectSettings/EditorBuildSettings.asset`: `Level1`, `Level2`, `Level3`, `LevelSelect`, `MainMenu`, and `Customization`. `SampleScene` and the URP template scene are also present as auxiliary scenes but are not in the build scene list.

## Current scenes

| Scene | Role indicated by its contents |
|---|---|
| `Assets/Scenes/MainMenu.unity` | Main menu with play/level-select, customization, settings, and quit controls |
| `Assets/Scenes/LevelSelect.unity` | Selection controls for Levels 1–3 |
| `Assets/Scenes/Customization.unity` | Character option/category UI |
| `Assets/Scenes/Levels/Level1.unity` | Gameplay scene with player, goal, checkpoint, and gameplay UI |
| `Assets/Scenes/Levels/Level2.unity` | Gameplay scene with player, goal, checkpoint, enemy, and gameplay UI |
| `Assets/Scenes/Levels/Level3.unity` | Gameplay scene with player, goal, checkpoint, enemy, and gameplay UI |

`Assets/Scenes/SampleScene.unity` is an auxiliary scene. `Assets/Settings/Scenes/URP2DSceneTemplate.unity` is a render-pipeline template.

![Level 1 scene layout in the Unity Editor, showing the player, platforms, goal, and checkpoint.](pics/09_level_scene.png)

*Level 1 layout in the Unity Editor. This is an editor scene view, not a captured gameplay run.*

## Implemented systems

- Rigidbody2D player movement using the New Input System, with acceleration/deceleration, variable jump height, jump buffering, coyote time, and stronger falling gravity.
- Player health, temporary damage invulnerability, fall death, a single automatic respawn, checkpoint position updates, and a game-over state after the respawn has been used.
- Player animation state handling and enemy interaction. Player combat supports stomping the simple `Enemy` type and damaging `GoblinEnemy`; goblin sword damage is issued through animation events.
- Goblin patrol/awareness/attack behavior and a configurable chase mode; a wraith patrol and projectile attack system also exists in source.
- Trigger-based collectibles, including health pickups; checkpoints; hazards; level goals; and enemy tracking/progression scripts.
- Main menu, level selection, customization, pause, game-over, health HUD, and level-complete UI controllers.
- Character customization categories/options, a renderer, and PlayerPrefs serialization for customization selections.
- Audio playback helpers for gameplay events and UI, plus a configurable camera-follow component.

See [Gameplay Systems](docs/GAMEPLAY_SYSTEMS.md) for the code-level behavior and limits.

![Player character and configured movement and jump values in the Unity Inspector.](pics/01_player_controller.png)

*Player setup in the Inspector, including movement, jump, and ground-detection settings.*

![Customization UI controller and its assigned category, option, and action controls in the Unity Inspector.](pics/05_customization_ui.png)

*Customization controller setup with references to the category and option navigation controls.*

![LevelGoal trigger collider and completion color settings in the Unity Inspector.](pics/06_level_goal.png)

*The level goal is configured as a trigger; its script also requires the enemy tracker and progression manager for completion.*

![Checkpoint trigger and active/inactive visual configuration in the Unity Inspector.](pics/07_checkpoint.png)

*Checkpoint setup with its trigger collider and inactive/active visual colors.*

## Repository map

```text
Assets/
  Scenes/                 Menus, customization, levels, sample/template scenes
  Scripts/                Gameplay and UI C# scripts
  Prefabs/                Player and platform prefabs
  ScriptableObjects/      Character customization data
  Enemies/, Vector Parts/ Character/enemy part sprites and related art
  Audio/                  Music and sound effect clips
  Settings/               URP renderer and scene-template settings
  TextMesh Pro/           TextMesh Pro package resources and examples
Packages/                 Unity package manifest and lock data
ProjectSettings/          Unity editor, input, render, physics, and build settings
docs/                     Project documentation
```

## Documentation

- [Setup](docs/SETUP.md)
- [Gameplay systems](docs/GAMEPLAY_SYSTEMS.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Development notes](docs/DEVELOPMENT.md)

## Scope and future work

The repository contains code for several reusable systems beyond what is currently represented in the three level scenes. For example, the wraith and collectible scripts exist, but the current gameplay scenes inspected do not establish that those features are placed and wired into the levels. Treat adding and balancing content, confirming scene wiring, and end-to-end playtesting as ongoing development work. No unimplemented future feature is described here as complete.
