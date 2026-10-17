# Maze Runner — new characters and enemies (2026-10-18)

Checkpoint before this work: tag `checkpoint-before-roster-2026-10-18`.

## Imported (only the requested variants)

| Variant | Archive | How it is used |
|---|---|---|
| Dark Oracle 2 | sprite/sprite2.zip | Rig `Assets/Characters/Dark_Oracle` (13 parts + Spriter file) — playable |
| Dark Oracle 3 | sprite/sprite2.zip | Part set `Assets/Resources/Characters/Dark_Oracle_3` on the Dark Oracle rig (same skeleton) — playable |
| Valkyrie 1 | sprite/sprite3.zip | Rig `Assets/Characters/Valkyrie` (14 parts incl. Shield) — playable |
| Villager Zombie 1 / 3 | enemy/enemy3.zip | Rig `Assets/Enemies/Zombie_Villager`; Zombie 3 = part set in `Skins/Zombie_Villager_3` |
| Skeleton Warrior 2 / 3 | enemy/enemy4.zip | Rig `Assets/Enemies/Skeleton_Warrior`; Warrior 3 = part set in `Skins/Skeleton_Warrior_3` |
| Bloody Alchemist 1 | enemy/enemy5.zip | Rig `Assets/Enemies/Bloody_Alchemist` |
| Skeleton Crusader 1 | enemy/enemy6.zip | Rig `Assets/Enemies/Skeleton_Crusader` (with Shield) |
| Wraith 3 | enemy/enemy1.zip (PNG/Wraith_03) | Rig `Assets/Enemies/Wraith` |
| Ogre | enemy/enemy2.zip | Rig `Assets/Enemies/Ogre` |

Plus one idle frame per new playable character as its preview (`Assets/Resources/Characters/<id>/Preview.png`).
The packs' 900×900 PNG frame sequences were not imported: every variant ships Spriter body parts and an
`Animations.scml`, and rigs built from those are a few hundred KB instead of tens of MB of frames.

The `.scml` files live in `ArtSource/Spriter/<Rig>/` (outside `Assets`, so the Spriter2UnityDX postprocessor
and its options window never run). `Assets/Editor/RosterImport.cs` (menu *Maze Runner > Import Character
Roster*, or `-executeMethod MazeRunner.EditorTools.RosterImport.RunFromCommandLine`) rebuilds everything:
- rig prefabs with all clips, through the same Spriter2UnityDX PrefabBuilder that made the Goblin and Fallen
  Angels rigs (parts imported as single sprites, pivots from the `.scml`);
- player controllers copied from `Fallen_Angels.controller` and enemy controllers copied from
  `Goblin-backup/Goblin.controller`, each state pointed at the rig's clip of the same name;
- enemy prefab variants (`Assets/Enemies/<Rig>/<Variant>.prefab`; the Ogre is `Ogre/Ogre_Enemy.prefab`, since its rig is `Ogre.prefab`): Enemy layer, Rigidbody2D, capsule and
  attack point like the Level 1 goblins, the existing `GoblinEnemy` AI with per-enemy speed/health/reach,
  visible weapon; part-set variants add `RigSpriteSkin`;
- `Resources/PlayerRig.asset` rig list and `Resources/LevelEnemies/Level1.asset` placements.

## Missing animations and fallbacks
- Bloody Alchemist 1: no *Idle Blinking* → Idle.
- Wraith 3: clips are Idle, Idle Blinking, Moving Forward, Attack, Casting Spells, Taunt, Hurt, Dying. Walking and
  Running → Moving Forward, Slashing → Attack; jump/fall/kick/throw states → Idle (a patrolling enemy never uses them).
- The player has no attack input (enemies are defeated by stomping), so the characters' attack clips are not used.

## Code
- `PlayableCharacters` (6 characters, each with a rig id), `PlayerRigSettings` (rig list), `PlayerRigMarker`,
  `PlayerCharacterSkin` (puts the selected rig on the Player in level scenes and points the existing
  `PlayerAnimationController` at it via the new `UseRig`), `RigSpriteSkin`.
- `GoblinEnemy`: hits at `attackHitTime` (0.2 s) when the attack clip has no `DealDamage` event (the goblins'
  clips have one at 0.1 s, so goblins are unchanged); `ConfigurePatrol` for spawned enemies.
- `LevelEnemySpawnTable` spawns a scene's table at load; `EnemyTracker.RefreshEnemyCount` counts them for the goal.
- Level scenes were not edited. Level 2 and 3 are untouched (still disabled by `EnabledLevelCount = 1`).

## Level 1 placements
Zombie 1 x16 · Zombie 3 x30 · Skeleton Warrior 2 x47 · Skeleton Warrior 3 x85 · Wraith 3 x103 ·
Ogre x127.5 + Skeleton Crusader 1 x130.5 (one platform: if one dies the other patrols it all) · Bloody Alchemist 1 x148.
The checkpoint (x64) and goal (x168) platforms stay clear.

## Tests (WebGL test build with the MAZE_AI_TEST probe, in the browser pane)
- Customization lists 6 characters; Dark Oracle 2 (4/6), Dark Oracle 3 (5/6), Valkyrie 1 (6/6) show the right names and previews.
- Each new character in Level 1 gets its rig and controller: Dark Oracle 2/3 (Dark_Oracle_Player), Valkyrie 1 (Valkyrie_Player);
  Dark Oracle 3 shows its own parts. Idle, Running, Jump Start → Jump Loop → Falling Down → Idle (Dark Oracle, Valkyrie),
  Hurt and Dying (Dark Oracle) observed. Selection kept after `RestartLevel` and a page reload; switching back to a
  Fallen Angels character restores the scene's own rig.
- All 8 enemies spawn on their platforms, walk, face their direction, notice the player and play Slashing (damage
  reaches the player: Hurt), take damage (Hurt), die (Dying) and are removed. A real stomp killed Villager Zombie 1.
- No console errors in Level 1.
- Not tested: real phones; a full manual play-through of Level 1 with all enemies; Levels 2/3 (out of scope).
