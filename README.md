<div align="center">
<samp><b>MAZE RUNNER</b></samp>

<samp>unity · c# · 2d platformer · game development</samp>

</div>

---

<div align="center"><samp>Maze Runner is a 2D side-scrolling platform game built with Unity. The repository currently contains three gameplay levels, menu and customization scenes, player movement and combat systems, enemies, checkpoints, level goals, and supporting UI and audio.</samp></div>

---

<div align="center"><samp><b>▶ <a href="https://yo5on.github.io/maze-runner/">Play Online</a></b></samp></div>

<div align="center"><samp>Preview page: <a href="https://yo5on.github.io/maze-runner/preview/">yo5on.github.io/maze-runner/preview</a></samp></div>

<div align="center"><samp>Runs in desktop and mobile browsers via Unity's Web (WebGL) platform, hosted on GitHub Pages. About 16 MB to download; first load takes a few seconds.</samp></div>

---

<div align="center"><samp><b>Controls</b></samp></div>

| <samp>Action</samp> | <samp>Desktop</samp> | <samp>Phone / tablet</samp> |
|---|---|---|
| <samp>Move</samp> | <samp><kbd>A</kbd>/<kbd>D</kbd> or <kbd>←</kbd>/<kbd>→</kbd></samp> | <samp>Joystick (bottom-left): drag left/right</samp> |
| <samp>Jump (hold for a higher jump)</samp> | <samp><kbd>Space</kbd></samp> | <samp>JUMP button (bottom-right)</samp> |
| <samp>Pause / resume</samp> | <samp><kbd>Esc</kbd> or the pause button (top-right)</samp> | <samp>Pause button (top-right)</samp> |
| <samp>Fullscreen</samp> | <samp><kbd>F</kbd> or the fullscreen button</samp> | <samp>Fullscreen button (Android); Home Screen on iPhone</samp> |
| <samp>Menus</samp> | <samp>Mouse click</samp> | <samp>Tap</samp> |

<samp>Enemies are defeated by landing on them. The joystick and buttons appear only on touch devices and can be used at the same time (run and jump). Landscape gives the largest view.</samp>

<samp><b>Characters:</b> Main Menu → Customization lets you pick one of six complete characters (Silver Angel, Hooded Angel and Crowned Angel from the Fallen Angels pack; Dark Oracle 2, Dark Oracle 3 and Valkyrie 1, each with its own skeleton and animations) with Previous / Next and a preview, then Apply. The choice is saved in the browser (PlayerPrefs) and used in Levels 1–3, after restarts and on later visits; movement, physics, health and combat are the same for every character.</samp>

<samp><b>Goblins:</b> regular goblins patrol their platform with their swords drawn, turn at ledges, walls and the ends of their patrol, and stop to slash when you are in reach. When one of two goblins sharing a platform dies, the other patrols the whole platform. The boss stands guard until it spots you, then runs at you.</samp>

<samp><b>More enemies (Level 1):</b> Villager Zombie 1 and 3, Skeleton Warrior 2 and 3, Bloody Alchemist 1, Skeleton Crusader 1, Wraith 3 and an Ogre patrol the platforms between the goblins. They use the goblins' AI (patrol, notice, slash, hurt, die) with their own speed and health; stomp them like goblins. Placement: <code>Assets/Resources/LevelEnemies/Level1.asset</code>.</samp>

<samp><b>Full screen on iPhone:</b> Safari on iPhone does not let web pages enter full screen, so the fullscreen button shows how to add the game to the Home Screen instead (Share → Add to Home Screen). Opened from the Home Screen, the game runs without Safari's toolbars. Android Chrome and desktop browsers use real full screen.</samp>

---

<div align="center"><samp><b>Gameplay Preview</b></samp></div>

<samp>A recording of Level 1 being played in the Unity Editor’s Game view: platforming between ledges, goblin enemies, the heart health display taking damage, and the level-complete menu. It was captured with Unity 6.5 (<b>6000.5.1f1</b>); the Editor’s menu bar and toolbar are visible at the top of the frame.</samp>

<samp><a href="media/gameplay-level-1.mp4">Watch the Level 1 gameplay recording</a> (<code>media/gameplay-level-1.mp4</code>, MP4, 97 MB, stored with Git LFS)</samp>

---

<div align="center"><samp><b>Project Status</b></samp></div>

<samp>The project is an active college project prototype. Its current source includes implemented game systems and authored Unity scenes; the repository does not include a release build or a complete testing record. Descriptions of implemented systems below are based on the current source, scene, prefab, and project configuration files. A script existing in the project does not by itself guarantee it is used in every scene.</samp>

<samp>Recent history (September 2026) records work on enemy animations, level audio, enemy fall death, a one-respawn game-over flow, and a heart-based health HUD that shows one heart per point of the player’s maximum health (seven in Level 1, three in Levels 2 and 3). Older root notes describe an earlier phase and conflict with the current scenes and packages, so they should not be treated as the present project specification.</samp>

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

<div align="center"><samp><b>Web Build and Deployment</b></samp></div>

<samp>The Web build needs the <b>Web Build Support</b> module for your Unity Editor (Unity Hub → Installs → ⚙ → Add modules) and an activated Unity license.</samp>

1. <samp>In the Editor choose <b>Maze Runner → Build Web (GitHub Pages)</b>. The build is written to <code>Builds/WebGL</code> (git-ignored). Or build from the command line:</samp>

```bash
Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod MazeRunner.EditorTools.WebBuild.BuildFromCommandLine -logFile build.log
```

2. <samp>Test locally (browsers block the game when it is opened as a <code>file://</code> page):</samp>

```bash
python -m http.server 8000 --directory Builds/WebGL
```

3. <samp>Publish to GitHub Pages. The script commits the build to the <code>gh-pages</code> branch only (never <code>main</code>) and pushes without force:</samp>

```bash
powershell -ExecutionPolicy Bypass -File tools/deploy-gh-pages.ps1
```

<samp>GitHub Pages serves the <code>gh-pages</code> branch root (Settings → Pages → Deploy from a branch → <code>gh-pages</code> / <code>/ (root)</code>).</samp>

<samp>Web setup lives in <code>Assets/Editor/WebBuild.cs</code> and <code>Assets/WebGLTemplates/MazeRunner/</code>:</samp>

- <samp>Scene order for the Web build starts at <code>MainMenu</code>; the Build Settings scene list is unchanged.</samp>
- <samp>Gzip compression with the decompression fallback, because GitHub Pages cannot send <code>Content-Encoding</code> headers for pre-compressed files. Size-optimized IL2CPP code.</samp>
- <samp>The game fills the browser window: it renders 1080 px tall (the UI's design height) at the window's aspect ratio, between 16:9 and 2.4:1. All cameras are orthographic with a fixed vertical size, so wider windows show more of the level sideways instead of stretching anything; only windows outside that range get thin bars. The layout updates on resize, rotation and fullscreen changes.</samp>
- <samp>The page's pause button drives the original Unity pause menu through <code>Assets/Scripts/UI/WebBridge.cs</code> and <code>Assets/Plugins/WebGL/MazeRunnerBridge.jslib</code>: Unity reports the pause state to the page (<code>none</code>, <code>playing</code>, <code>paused</code>, <code>blocked</code>) and the button calls <code>PauseMenuController.Pause()</code>/<code>Resume()</code>, so it never opens a second overlay and always shows the real state.</samp>
- <samp>The touch joystick and buttons send the same keyboard key transitions the game already reads (one key-down when a control engages, one key-up when it releases), so gameplay code, physics, and input actions are unchanged. Pinch-zoom, double-tap zoom, and page scrolling are blocked while playing.</samp>
- <samp>The page around the game uses the portfolio palette (midnight navy <code>#1b1e4a</code>, stone grey <code>#afaea2</code>, crimson <code>#d21319</code>) and typefaces; the in-game Unity UI keeps its original design. The game starts loading as soon as the page opens and shows the Unity main menu; there is no separate landing page.</samp>
- <samp><code>Assets/WebGLTemplates/MazeRunner/preview/</code> is a lightweight preview page (screenshot and Play Now) published at <code>/maze-runner/preview/</code>. It loads no Unity files; Play Now checks the game is reachable, then opens the game at <code>/maze-runner/</code>, which loads straight into the main menu. Unity copies it into every Web build, so it deploys with the game.</samp>
- <samp>The page includes a web app manifest and icons so it can be installed to a phone's Home Screen and launched full screen.</samp>
- <samp><code>Assets/Scripts/UI/MenuPresentation.cs</code> leaves the original Unity menus' design unchanged. In Web builds it enlarges the Constant Pixel Size UI by one uniform factor (1.4×) so menus keep the same proportions on desktop and mobile and are easy to tap, and it adds a Game Over panel matching Level 1's original one to Levels 2 and 3, which previously froze on game over.</samp>

<samp><b>Known limitations of the Web version:</b></samp>

- <samp>The live build was produced with Unity <b>6000.6.5f1</b> (the project targets <b>6000.5.1f1</b>); the version upgrade was not committed.</samp>
- <samp><b>Quit</b> does nothing in a browser; close the tab instead. <b>Settings</b> is not implemented yet (same as desktop).</samp>
- <samp>Progress and customization are saved in the browser's storage (IndexedDB) and are lost if site data is cleared or in private windows.</samp>
- <samp>In portrait on phones the game is small because the levels are 16:9; rotate to landscape.</samp>
- <samp>Progress saved in the Home Screen version of the game is stored separately from progress saved in Safari.</samp>
- <samp>Unity's Web player officially targets desktop browsers; mobile browsers work but performance depends on the device.</samp>

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
