# Setup

## Software

- Unity Editor **6000.5.1f1**, as specified by `ProjectSettings/ProjectVersion.txt`
- Unity Hub (recommended for installing/selecting the project editor)
- Git, if obtaining the project by clone

The package manifest identifies the project dependencies. Unity Package Manager should resolve them when the project opens. Allow package resolution and asset import to finish before opening scenes or entering Play mode.

## Open the project

1. Clone or download the repository.
2. In Unity Hub, choose **Add project from disk** and select the repository root.
3. If Unity asks for an editor version, use **6000.5.1f1**.
4. Wait for package resolution and import to complete. Review the Unity Console for errors before playtesting.
5. Open `Assets/Scenes/MainMenu.unity` or a scene under `Assets/Scenes/Levels/`.

The repository includes generated Unity folders such as `Library`, but these are editor cache data and are not required as source inputs. Unity can regenerate its cache when needed.

## Scenes and build configuration

The enabled scenes in `ProjectSettings/EditorBuildSettings.asset`, in listed order, are:

1. `Assets/Scenes/Levels/Level1.unity`
2. `Assets/Scenes/Levels/Level2.unity`
3. `Assets/Scenes/Levels/Level3.unity`
4. `Assets/Scenes/LevelSelect.unity`
5. `Assets/Scenes/MainMenu.unity`
6. `Assets/Scenes/Customization.unity`

The build scene list does not establish a launch scene; review Player settings and the menu flow in Unity when preparing a build. `SampleScene.unity` and `Assets/Settings/Scenes/URP2DSceneTemplate.unity` are not listed as build scenes.

## Packages and rendering

Key direct package versions in `Packages/manifest.json`:

| Package | Version | Use indicated by package/project |
|---|---:|---|
| `com.unity.inputsystem` | 1.19.0 | New Input System |
| `com.unity.render-pipelines.universal` | 17.5.0 | Universal Render Pipeline |
| `com.unity.2d.animation` | 15.1.0 | 2D animation support |
| `com.unity.2d.aseprite` | 5.0.3 | Aseprite importer |
| `com.unity.2d.psdimporter` | 14.0.3 | PSD importer |
| `com.unity.2d.spriteshape` | 15.0.3 | Sprite Shape |
| `com.unity.2d.tilemap.extras` | 8.0.3 | Tilemap extras |
| `com.unity.ugui` | 2.5.0 | Unity UI |

Other direct dependencies are listed in the manifest, including Timeline 1.8.12, Visual Scripting 1.9.11, Test Framework 1.7.0, IDE integrations, and Unity modules. TextMesh Pro assets/resources are present under `Assets/TextMesh Pro/`; TextMesh Pro does not appear as a direct entry in the current manifest. The project contains URP 2D renderer settings. Cinemachine is not a current manifest dependency.

## Input and controls

`Assets/InputSystem_Actions.inputactions` is referenced as the active input-actions configuration from the editor build settings. `PlayerController` reads the `Move` and `Jump` actions from a `PlayerInput` component. Refer to the action asset for the authoritative bindings; older setup guides mention keyboard bindings but may not describe the current asset exactly.

## Running a scene

Open the chosen scene and enter Play mode. For gameplay, start with `Level1`. Each level scene contains a player object and gameplay UI. In case a scene behaves differently from these notes, inspect the scene's serialized component references and the Unity Console: successful script compilation alone does not guarantee every Inspector reference or animation event is configured correctly.

The repository does not document a verified target platform, build profile, or release packaging process. Configure and validate these in Unity before distributing a build.
