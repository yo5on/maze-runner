# Gameplay Systems

This document describes behavior present in the current C# source and scene files. Script availability and scene placement are distinguished where they differ.

## Player movement and animation

`PlayerController` uses a `Rigidbody2D` and Unity's New Input System. It reads `Move` and `Jump` from `PlayerInput`, accelerates toward horizontal input, and uses jump force with early-release jump cutting. Ground detection uses a configurable `Physics2D.OverlapBox`. Coyote time and jump buffering are implemented with timers; falling uses a stronger gravity scale. Ground and velocity gizmos support editor inspection.

`PlayerAnimationController` maps player movement/health state to an Animator controller, flips the character based on horizontal movement, and exposes hurt/death/reset behavior. The recent history references integration of Fallen Angels character animations. Animation clips/controllers and animation-event wiring should be inspected in Unity when troubleshooting; these docs do not claim to validate runtime presentation.

## Health, death, and respawn

`PlayerHealth` starts with three health by default. Damage is ignored during the configured invincibility window, reduces health, triggers the hurt animation/audio, and emits a health-changed event. Falling below the configured death height also calls the death flow.

The current implementation permits one automatic respawn per level run. The player returns to the last checkpoint position (initially the starting position), restores full health, and receives temporary invulnerability. A later death opens the game-over state, pauses time, and disables `PlayerController`. Restarting reloads the active scene.

## Combat and enemies

`PlayerCombat` detects contacts on its configured enemy layer. A downward collision from above stomps a basic `Enemy` or deals one damage to a `GoblinEnemy`, then bounces the player. Its source explicitly leaves body-contact damage disabled; goblin damage to the player is handled by sword attacks rather than ordinary contact. Goblin `DealDamage()` uses an overlap circle and is intended to be invoked by an animation event.

`Enemy` is a simple Rigidbody2D patrol implementation with a `Defeat()` method. `GoblinEnemy` has patrol behavior, player awareness and attack behavior, health, death/fall handling, and a configurable boss chase mode. `WraithEnemy` patrols and can spawn a `Projectile` toward the player within range. Projectiles move without gravity, damage a tagged player on trigger contact, and are destroyed on ground contact or after a timeout.

The inspected Levels 2 and 3 contain an object named `Enemy_01`; the simple and goblin enemy implementations are in source. A wraith implementation also exists, but its placement in the current gameplay scenes was not established by this inspection. Do not infer that every enemy type appears in playable levels.

## Checkpoints, hazards, goals, and progression

`Checkpoint` is a trigger that activates once, updates the player's respawn position, changes its configured sprite color, and plays checkpoint audio. `Hazard` provides a player hazard/death interaction. `LevelGoal` only completes when its trigger receives the player, an `EnemyTracker` exists, the tracker's remaining count is zero, and a `LevelProgressionManager` exists. `LevelCompleteController` supplies replay, next-level, and main-menu UI behavior. `EnemyTracker` and `LevelProgressionManager` provide code for enemy counting and level progression.

The three gameplay scenes contain goal and checkpoint objects. The goal script has an enemy-clear gate, so its successful path depends on the `EnemyTracker` being present and correctly configured in the scene. Confirm tracker references/counting and level progression behavior in Unity when changing level composition.

## Collectibles

`Collectible` supports `Coin`, `Gem`, and `Health` types. It triggers on a tagged player, heals for the configured value for health pickups, raises a static collection event, and destroys the pickup. The current level scenes inspected do not demonstrate collectible placements, so the type support is implemented in source but its use as level content is unconfirmed.

## Menus and UI

The project has menu controllers for the main menu, level select, character customization, pause, game over, and level completion. Gameplay scenes include health display and pause/game-over/level-complete UI objects. `HealthDisplay` and `HUDController` connect player/level state to UI. `UIButtonSound` supports click feedback.

The main menu scene contains play/level-select, customization, settings, and quit controls. The settings control's existence is confirmed by the scene; this document does not claim a complete settings screen or persistence behavior without further implementation evidence.

## Character customization

Customization is represented by category and option ScriptableObjects, `CharacterCustomizationManager`, `CharacterRenderer`, and `CustomizationUIController`. Data assets include body, eyes, hair, hat, shirt, pants, shoes, and accessory categories (not every category has a default option asset). The manager supports applying/cycling options and saving/loading selected option IDs through PlayerPrefs. The Customization scene contains category and option controls.

This system is a player appearance configurator; it should not be confused with a progression/unlock economy. No unlock system is established by the inspected code/assets.

## Audio and camera

`AudioManager` exposes playback helpers for jump, collection, damage, enemy defeat, checkpoint, level completion, button click, sword hit, and respawn; it can pause/resume music and adjust music/SFX volume. Eight MP3 files are present in `Assets/Audio/Music/`, with filenames for level music and several effects. Filenames alone do not guarantee every clip is assigned to an AudioSource in every scene.

`CameraFollow` is a configurable smooth-follow MonoBehaviour with axis toggles, offset, optional bounds, optional look-ahead, and target/snap methods. It provides a project-owned alternative to Cinemachine. Scene-level camera component assignments should be checked in Unity before relying on a specific follow configuration.
