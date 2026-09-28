<div align="center">

<img src="https://raw.githubusercontent.com/yo5on/yo5on/main/hd-projects.svg" width="620" alt="projects"/>

<samp><b>MAZE RUNNER</b></samp>

<samp>unity · c# · 2d platformer · game development</samp>

</div>

---

<div align="center"><samp>Maze Runner is a 2D side-scrolling platform game built with Unity. The repository currently contains three gameplay levels, menu and customization scenes, player movement and combat systems, enemies, checkpoints, level goals, and supporting UI and audio.</samp></div>

---

<div align="center"><samp><b>Project Status</b></samp></div>

<samp>The project is an active college project prototype. Its current source includes implemented game systems and authored Unity scenes; the repository does not include a release build or a complete testing record. Descriptions of implemented systems below are based on the current source, scene, prefab, and project configuration files. A script existing in the project does not by itself guarantee it is used in every scene.</samp>

<samp>Recent history (September 2026) records work on enemy animations, level audio, enemy fall death, a one-respawn game-over flow, and a three-heart HUD. Older root notes describe an earlier phase and conflict with the current scenes and packages, so they should not be treated as the present project specification.</samp>

---

<div align="center"><samp><b>Requirements</b></samp></div>

- <samp>Unity <b>6000.5.1f1</b></samp>
- <samp>Unity Package Manager dependencies in <code>Packages/manifest.json</code></samp>
- <samp>A desktop platform supported by the installed Unity Editor</samp>

<samp>The project uses Universal Render Pipeline with a 2D renderer and the New Input System. Cinemachine is not listed in the current package manifest; the project contains its own <code>CameraFollow</code> script.</samp>

---

<div align="center"><samp><b>Open the Project</b></samp></div>

1. <samp>Install or open Unity Hub and add this repository as a project.</samp>
2. <samp>Select Unity <b>6000.5.1f1</b> when prompted.</samp>
3. <samp>Allow Unity Package Manager to resolve the packages and import assets.</samp>
4. <samp>Open <code>Assets/Scenes/MainMenu.unity</code> or one of the gameplay scenes under <code>Assets/Scenes/Levels/</code>.</samp>
5. <samp>Use the Unity Editor Play control to run the selected scene.</samp>

<samp>The six intended application scenes enabled in the build settings are <code>Level1</code>, <code>Level2</code>, <code>Level3</code>, <code>LevelSelect</code>, <code>MainMenu</code>, and <code>Customization</code>.</samp>

---

<div align="center"><samp><b>Current Scenes</b></samp></div>

| <samp>Scene</samp> | <samp>Role</samp> |
|---|---|
| <samp><code>MainMenu.unity</code></samp> | <samp>Main menu with play, level selection, customization, settings, and quit controls</samp> |
| <samp><code>LevelSelect.unity</code></samp> | <samp>Selection controls for Levels 1–3</samp> |
| <samp><code>Customization.unity</code></samp> | <samp>Character option and category UI</samp> |
| <samp><code>Levels/Level1.unity</code></samp> | <samp>Gameplay scene with player, goal, checkpoint, and gameplay UI</samp> |
| <samp><code>Levels/Level2.unity</code></samp> | <samp>Gameplay scene with player, goal, checkpoint, enemy, and gameplay UI</samp> |
| <samp><code>Levels/Level3.unity</code></samp> | <samp>Gameplay scene with player, goal, checkpoint, enemy, and gameplay UI</samp> |

![Level 1 scene layout in the Unity Editor, showing the player, platforms, goal, and checkpoint.](pics/09_level_scene.png)

<div align="center"><samp><i>Level 1 layout in the Unity Editor.</i></samp></div>

---

<div align="center"><samp><b>Implemented Systems</b></samp></div>

- <samp>Rigidbody2D player movement using the New Input System, with acceleration/deceleration, variable jump height, jump buffering, coyote time, and stronger falling gravity.</samp>
- <samp>Player health, temporary damage invulnerability, fall death, a single automatic respawn, checkpoint position updates, and a game-over state after the respawn has been used.</samp>
- <samp>Player animation state handling and enemy interaction. Player combat supports stomping the simple <code>Enemy</code> type and damaging <code>GoblinEnemy</code>.</samp>
- <samp>Goblin patrol, awareness, attack behavior, and configurable chase mode; a wraith patrol and projectile attack system also exists in source.</samp>
- <samp>Trigger-based collectibles, health pickups, checkpoints, hazards, level goals, and enemy tracking/progression scripts.</samp>
- <samp>Main menu, level selection, customization, pause, game-over, health HUD, and level-complete UI controllers.</samp>
- <samp>Character customization categories/options, renderer, and PlayerPrefs serialization for customization selections.</samp>
- <samp>Audio playback helpers and configurable camera-follow behavior.</samp>

![Player character and configured movement and jump values in the Unity Inspector.](pics/01_player_controller.png)

<div align="center"><samp><i>Player setup in the Inspector.</i></samp></div>

![Customization UI controller and its assigned category, option, and action controls in the Unity Inspector.](pics/05_customization_ui.png)

<div align="center"><samp><i>Customization controller setup.</i></samp></div>

![LevelGoal trigger collider and completion color settings in the Unity Inspector.](pics/06_level_goal.png)

<div align="center"><samp><i>Level goal configuration.</i></samp></div>

![Checkpoint trigger and active/inactive visual configuration in the Unity Inspector.](pics/07_checkpoint.png)

<div align="center"><samp><i>Checkpoint configuration.</i></samp></div>

---

<div align="center"><samp><b>Repository Map</b></samp></div>

```text
Assets/
│  Scenes/                 Menus, customization, levels, sample/template scenes
│  Scripts/                Gameplay and UI C# scripts
│  Prefabs/                Player and platform prefabs
│  ScriptableObjects/      Character customization data
│  Enemies/, Vector Parts/ Character and enemy art
│  Audio/                  Music and sound effects
│  Settings/               URP renderer and scene settings
│  TextMesh Pro/           TextMesh Pro resources
Packages/                  Unity package manifest and lock data
ProjectSettings/           Unity editor and build settings
docs/                      Project documentation
```

---

<div align="center"><samp><b>Documentation</b></samp></div>

- <samp>[Setup](docs/SETUP.md)</samp>
- <samp>[Gameplay systems](docs/GAMEPLAY_SYSTEMS.md)</samp>
- <samp>[Architecture](docs/ARCHITECTURE.md)</samp>
- <samp>[Development notes](docs/DEVELOPMENT.md)</samp>

---

<div align="center"><samp><b>Scope and Future Work</b></samp></div>

<samp>The repository contains reusable systems beyond what is currently represented in the three level scenes. For example, the wraith and collectible scripts exist, but the current gameplay scenes inspected do not establish that those features are placed and wired into the levels. Adding and balancing content, confirming scene wiring, and end-to-end playtesting remain ongoing work.</samp>
