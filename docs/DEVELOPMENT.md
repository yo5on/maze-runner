# Development

## Current baseline

Use Unity **6000.5.1f1**. Keep Unity-authored content (scenes, prefabs, ProjectSettings, package manifests, and `.meta` files) under Unity's control. The repository's direct packages are recorded in `Packages/manifest.json`; review that file when changing package dependencies.

The recent repository history through September 15, 2026 includes updates to goblin animations, level audio, enemy fall death, one-respawn game over, and the three-heart health HUD. This is a summary of commit subjects, not a claim that the project has passed a release QA cycle.

## Working in the project

1. Open the project using Unity 6000.5.1f1 and let packages/import finish.
2. Check the Console for compile or asset-reference errors.
3. Open the relevant scene and inspect its serialized components and references before editing behavior.
4. Use Play mode to verify the affected player/enemy/UI flow in context.
5. Save scenes/prefabs changed in the Editor and review their diffs together with related script changes.

The project has no checked-in automated gameplay test suite or documented CI validation workflow. The Test Framework package is declared, but that alone does not mean tests exist or have run.

## Current implemented scope

The repository has source and scene content for player movement, animation, health, one automatic respawn, game over, combat, goblin and basic enemies, checkpoints, hazards, level goals, menus, UI, customization, audio, and camera following. A wraith/projectile system, collectibles, and progression/enemy-tracking code are also present in source. Their presence in source should be separated from placement/configuration in the playable scenes.

The three level scenes, player prefab, and platform prefabs provide a base for iteration. Current scene inspection shows a goal and checkpoint in each gameplay level and an `Enemy_01` in Levels 2 and 3. It does not prove all combat modes, collectible types, or enemy variants are configured in those scenes.

## Verification checklist for contributors

When modifying a system, validate the affected behavior in Unity. Useful manual checks include:

- Player input, ground detection, jump buffering/coyote time, jump release, and landing
- Health HUD updates, damage invulnerability, checkpoint return, first respawn, and subsequent game over
- Enemy damage/death, stomp bounce, and animation-event sword damage
- Checkpoint and goal trigger behavior, pause/restart/menu scene transitions
- Customization selection, visual update, reset, and save/load across scene reload
- AudioSource/clip assignments and audio feedback in the relevant scene
- Scene loading and Build Settings after changing scene names or organization

This checklist is suggested development practice, not a statement that these checks have already passed.

## Areas to confirm or extend

These are follow-up opportunities inferred from repository coverage, not existing promises or complete features:

- Playtest and tune all three levels, enemy encounters, and UI flows.
- Confirm Inspector references, animation controllers/events, tags, layers, and audio assignments for every scene.
- Decide whether collectible and wraith systems should be integrated into levels, then add/configure scene content if desired.
- Review level progression and unlock requirements in scene configuration and code before documenting a required clear condition.
- Define target platforms and a repeatable build/release process.
- Add automated tests or a CI workflow if appropriate for the project's scope.

## Documentation maintenance

Keep this documentation aligned with the current Unity project files. Prefer `ProjectVersion.txt`, `Packages/manifest.json`, Build Settings, scene/prefab contents, and active source code over older planning notes when details conflict. Label roadmap items as planned until their code and scene configuration are present.
