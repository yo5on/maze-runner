<div align="center">

<img src="https://raw.githubusercontent.com/yo5on/yo5on/main/hd-projects.svg" width="620" alt="projects"/>

<samp><b>MAZE RUNNER</b></samp>

<samp>unity · c# · 2d platformer · game development</samp>

**[Repository](https://github.com/yo5on/maze-runner)**

</div>

---

Maze Runner is a 2D side-scrolling platform game built with Unity. The repository currently contains three gameplay levels, menu and customization scenes, player movement and combat systems, enemies, checkpoints, level goals, and supporting UI and audio.

## Project Status

The project is an active college project prototype. Its current source includes implemented game systems and authored Unity scenes; the repository does not include a release build or a complete testing record. Descriptions of implemented systems below are based on the current source, scene, prefab, and project configuration files. A script existing in the project does not by itself guarantee it is used in every scene.

Recent history (September 2026) records work on enemy animations, level audio, enemy fall death, a one-respawn game-over flow, and a three-heart HUD. Older root notes describe an earlier phase and conflict with the current scenes and packages, so they should not be treated as the present project specification.

## Requirements

- Unity **6000.5.1f1**
- Unity Package Manager dependencies in `Packages/manifest.json`
- A desktop platform supported by the installed Unity Editor

The project uses Universal Render Pipeline with a 2D renderer and the New Input System. Cinemachine is not listed in the current package manifest; the project contains its own `CameraFollow` script.

## Open the Project

1. Install or open Unity Hub and add this repository as a project.
2. Select Unity **6000.5.1f1** when prompted.
3. Allow Unity Package Manager to resolve the packages and import assets.
4. Open `Assets/Scenes/MainMenu.unity` or one of the gameplay scenes under `Assets/Scenes/Levels/`.
5. Use the Unity Editor Play control to run the selected scene.

The six intended application scenes enabled in the build settings are `Level1`, `Level2`, `Level3`, `LevelSelect`, `MainMenu`, and `Customization`.

## Current Scenes

| Scene | Role |
|---|---|
| `MainMenu.unity` | Main menu with play, level selection, customization, settings, and quit controls |
| `LevelSelect.unity` | Selection controls for Levels 1–3 |
| `Customization.unity` | Character option and category UI |
| `Levels/Level1.unity` | Gameplay scene with player, goal, checkpoint, and gameplay UI |
| `Levels/Level2.unity` | Gameplay scene with player, goal, checkpoint, enemy, and gameplay UI |
| `Levels/Level3.unity` | Gameplay scene with player, goal, checkpoint, enemy, and gameplay UI |

![Level 1 scene layout in the Unity Editor, showing the player, platforms, goal, and checkpoint.](pics/09_level_scene.png)

*Level 1 layout in the Unity Editor.*

## Implemented Systems

- Rigidbody2D player movement using the New Input System, with acceleration/deceleration, variable jump height, jump buffering, coyote time, and stronger falling gravity.
- Player health, temporary damage invulnerability, fall death, a single automatic respawn, checkpoint position updates, and a game-over state after the respawn has been used.
- Player animation state handling and enemy interaction. Player combat supports stomping the simple `Enemy` type and damaging `GoblinEnemy`.
- Goblin patrol, awareness, attack behavior, and configurable chase mode; a wraith patrol and projectile attack system also exists in source.
- Trigger-based collectibles, health pickups, checkpoints, hazards, level goals, and enemy tracking/progression scripts.
- Main menu, level selection, customization, pause, game-over, health HUD, and level-complete UI controllers.
- Character customization categories/options, renderer, and PlayerPrefs serialization for customization selections.
- Audio playback helpers and configurable camera-follow behavior.

![Player character and configured movement and jump values in the Unity Inspector.](pics/01_player_controller.png)

*Player setup in the Inspector.*

![Customization UI controller and its assigned category, option, and action controls in the Unity Inspector.](pics/05_customization_ui.png)

*Customization controller setup.*

![LevelGoal trigger collider and completion color settings in the Unity Inspector.](pics/06_level_goal.png)

*Level goal configuration.*

![Checkpoint trigger and active/inactive visual configuration in the Unity Inspector.](pics/07_checkpoint.png)

*Checkpoint configuration.*

## Repository Map

```text
Assets/
  Scenes/                 Menus, customization, levels, sample/template scenes
  Scripts/                Gameplay and UI C# scripts
  Prefabs/                Player and platform prefabs
  ScriptableObjects/      Character customization data
  Enemies/, Vector Parts/ Character and enemy art
  Audio/                  Music and sound effects
  Settings/               URP renderer and scene settings
  TextMesh Pro/           TextMesh Pro resources
Packages/                 Unity package manifest and lock data
ProjectSettings/          Unity editor and build settings
docs/                     Project documentation
```

## Documentation

- [Setup](docs/SETUP.md)
- [Gameplay systems](docs/GAMEPLAY_SYSTEMS.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Development notes](docs/DEVELOPMENT.md)

## Scope and Future Work

The repository contains reusable systems beyond what is currently represented in the three level scenes. For example, the wraith and collectible scripts exist, but the current gameplay scenes inspected do not establish that those features are placed and wired into the levels. Adding and balancing content, confirming scene wiring, and end-to-end playtesting remain ongoing work.
