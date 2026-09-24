# Blender Asset Workflow

## Asset and tool version

- **Blender:** 4.2.1 LTS (the version of the Blender executable used to create and export the asset)
- **Asset:** a low-poly, weathered stone ruin pillar with a small turquoise faceted rune and scattered base stones
- **Role in Maze Runner:** decorative background scenery for the existing 2D platformer

Blender was used here to create the asset during this integration. This documentation does not imply Blender had been used for earlier project assets.

## Why Blender

The pillar benefits from block-based 3D modeling, beveled edges, low-poly facets, a controlled three-quarter camera, and a transparent render. The FBX preserves the editable 3D model for reuse, while the rendered sprite fits the project's URP 2D renderer without adding 3D geometry or physics to platforming scenes.

## Modeling and materials

1. Build the broad foot, stacked plinth, shaft, neck band, and capital from cube meshes.
2. Apply one-segment bevels to soften the stone edges while keeping the flat, faceted low-poly appearance.
3. Offset the upper courses to form a broken crown and add a few small rubble blocks around the base.
4. Add a small low-subdivision icosphere as the faceted rune on the visible side of the shaft.
5. Set up an orthographic three-quarter camera and warm key, cool fill, and rim lights; use Eevee and render with a transparent background.

The Blender scene uses four named materials: weathered warm grey stone, chipped stone highlights, dark stone seams, and a muted turquoise emissive rune. The surface colors and lighting are baked into the PNG render; the FBX carries the mesh and material assignments for later 3D use.

## Export and Unity import

- **Blender source:** `Assets/Blender/MazeRunner_Environment.blend`
- **3D export:** `Assets/Models/Blender/MazeRunner_RuinPillar.fbx` (selected mesh objects, FBX, Blender forward `-Z` and up `Y`, with scale applied)
- **2D render:** `Assets/Art/Environment/Blender/MazeRunner_RuinPillar.png` (768 × 1024 RGBA PNG)
- **Unity prefab:** `Assets/Prefabs/Environment/MazeRunner_RuinPillar_Background.prefab`

Unity imports the FBX through its FBX model importer and the transparent PNG as a single Sprite with 128 pixels per unit, bilinear filtering, alpha transparency, and uncompressed texture data. The prefab contains only a `SpriteRenderer`, uses the URP 2D unlit material, is scaled to 0.72, and has sorting order −10 so it draws behind the ordinary order-0 gameplay sprites. It has no collider, Rigidbody, or gameplay script. The `.blend` file remains in `Assets/Blender` as the editable source, with a `DefaultImporter` meta file so this Unity installation does not attempt a native Blender conversion.

One prefab instance is saved in `Assets/Scenes/Levels/Level1.unity`. It sits on the world ground near the opening camera view at position `(−0.50, 0.58, 0)` in the current scene, with sorting layer `Default` and order `−10`. The Unity Editor placement check projected the SpriteRenderer bounds into the starting camera frame at viewport X `0.63–0.75` and Y `0.26–0.68`, inside the visible `0–1` range. The renderer is behind every existing sprite on the Default layer. The instance has zero `Collider2D` components, so it does not participate in collision or gameplay. This is the only Level1 scene addition; player movement, enemies, combat, audio, checkpoints, goals, and progression were left unchanged.

## Limitations

- The PNG is a static, baked-lighting image from one camera angle; it has no animation or parallax behavior.
- The FBX is available for future 3D use, but the gameplay integration uses the PNG sprite so the game remains 2D.
- The model has no collision geometry or gameplay behavior.
- This Unity editor reported that Blender could not be found through its file association when it attempted native `.blend` import. The Blender 4.2.1 executable was available for creating the source, FBX, and render, but native `.blend` conversion is not relied on here. The exported FBX and rendered PNG are imported as standalone Unity assets.
- The prefab instance is currently in Level1 only; other levels have not been changed.
