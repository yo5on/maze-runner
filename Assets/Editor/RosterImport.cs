using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Spriter2UnityDX;
using Spriter2UnityDX.Importing;
using Spriter2UnityDX.Prefabs;
using MazeRunner.Customization;
using MazeRunner.Enemies;
using MazeRunner.Levels;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Builds the extra playable characters and enemies from the Craftpix packs (see docs).
    ///
    /// For each rig, the pack's body-part PNGs sit in the rig's Assets folder and its Spriter file in
    /// ArtSource/Spriter/&lt;Rig&gt;/Animations.scml (outside Assets, so the Spriter2UnityDX postprocessor and
    /// its options window never run). This tool feeds the Spriter file to the same PrefabBuilder the
    /// existing Goblin and Fallen Angels rigs were made with, then:
    /// - copies the existing player / goblin Animator Controllers for each rig and points every state at the
    ///   rig's clip of the same name (with a fallback where a pack has no such clip);
    /// - makes enemy prefab variants with the existing GoblinEnemy AI, and skin variants for enemies that
    ///   share a skeleton (RigSpriteSkin);
    /// - registers the player rigs in Resources/PlayerRig and writes Level 1's spawn table.
    /// It can be run again: existing assets are updated in place, keeping their GUIDs.
    ///
    /// Menu: Maze Runner > Import Character Roster
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.RosterImport.RunFromCommandLine
    /// </summary>
    public static class RosterImport
    {
        private const string ArtSource = "ArtSource/Spriter";
        private const string PlayerTemplateController = "Assets/Vector Parts/Fallen_Angels.controller";
        private const string EnemyTemplateController = "Assets/Enemies/Goblin-backup/Goblin.controller";
        private const string PreviewTemplate = "Assets/Resources/Characters/Fallen_Angels_1/Preview.png";
        private const int PlayerLayer = 6;
        private const int EnemyLayer = 7;

        private class Rig
        {
            public string Id;          // entity / prefab name
            public string Folder;      // Assets folder with the parts
            public string SourceEntity; // entity name in the .scml when it differs from Id
            public Rig(string id, string folder, string sourceEntity = null)
            {
                Id = id; Folder = folder; SourceEntity = sourceEntity;
            }
        }

        private static readonly Rig[] PlayerRigs =
        {
            new Rig("Dark_Oracle", "Assets/Characters/Dark_Oracle"),
            new Rig("Valkyrie", "Assets/Characters/Valkyrie"),
        };

        private static readonly Rig[] EnemyRigs =
        {
            new Rig("Zombie_Villager", "Assets/Enemies/Zombie_Villager"),
            new Rig("Skeleton_Warrior", "Assets/Enemies/Skeleton_Warrior"),
            new Rig("Bloody_Alchemist", "Assets/Enemies/Bloody_Alchemist"),
            new Rig("Skeleton_Crusader", "Assets/Enemies/Skeleton_Crusader"),
            new Rig("Wraith", "Assets/Enemies/Wraith", "03"),
            new Rig("Ogre", "Assets/Enemies/Ogre"),
        };

        private class EnemySpec
        {
            public string Name;        // prefab and display name
            public string Rig;
            public string SkinFolder;  // null = the rig's own look
            public float Scale = 0.4f;
            public float MoveSpeed, AttackRange, AttackCooldown;
            public int Health;
        }

        // Goblins (the existing convention): speed 2, health 2, attack range 1.5, cooldown 1.5.
        private static readonly EnemySpec[] EnemySpecs =
        {
            new EnemySpec { Name = "Villager_Zombie_1", Rig = "Zombie_Villager", MoveSpeed = 1.4f, Health = 2, AttackRange = 1.4f, AttackCooldown = 1.8f },
            new EnemySpec { Name = "Villager_Zombie_3", Rig = "Zombie_Villager", SkinFolder = "Assets/Enemies/Zombie_Villager/Skins/Zombie_Villager_3", MoveSpeed = 1.4f, Health = 2, AttackRange = 1.4f, AttackCooldown = 1.8f },
            new EnemySpec { Name = "Skeleton_Warrior_2", Rig = "Skeleton_Warrior", MoveSpeed = 2.0f, Health = 3, AttackRange = 1.6f, AttackCooldown = 1.4f },
            new EnemySpec { Name = "Skeleton_Warrior_3", Rig = "Skeleton_Warrior", SkinFolder = "Assets/Enemies/Skeleton_Warrior/Skins/Skeleton_Warrior_3", MoveSpeed = 2.0f, Health = 3, AttackRange = 1.6f, AttackCooldown = 1.4f },
            new EnemySpec { Name = "Bloody_Alchemist_1", Rig = "Bloody_Alchemist", MoveSpeed = 1.8f, Health = 2, AttackRange = 1.6f, AttackCooldown = 1.6f },
            new EnemySpec { Name = "Skeleton_Crusader_1", Rig = "Skeleton_Crusader", MoveSpeed = 1.6f, Health = 3, AttackRange = 1.6f, AttackCooldown = 1.6f },
            new EnemySpec { Name = "Wraith_3", Rig = "Wraith", MoveSpeed = 1.8f, Health = 3, AttackRange = 1.8f, AttackCooldown = 1.8f },
            new EnemySpec { Name = "Ogre", Rig = "Ogre", Scale = 0.5f, MoveSpeed = 1.4f, Health = 4, AttackRange = 1.9f, AttackCooldown = 2.0f },
        };

        // Level 1 placements on platforms without goblins, the checkpoint (x 64) or the goal (x 168).
        // y = platform top (centre + 0.25) + 0.05.
        private static readonly (string enemy, float x, float y, int dir, float patrol)[] Level1Spawns =
        {
            ("Villager_Zombie_1", 16f, -0.2f, 1, 1.5f),
            ("Villager_Zombie_3", 30f, 0.1f, -1, 1.2f),
            ("Skeleton_Warrior_2", 47f, 0.3f, 1, 2f),
            ("Skeleton_Warrior_3", 85f, 0.3f, -1, 2f),
            ("Wraith_3", 103f, 0.3f, 1, 2f),
            ("Ogre", 127.5f, 0.8f, -1, 1.2f),
            ("Skeleton_Crusader_1", 130.5f, 0.8f, 1, 1.2f),
            ("Bloody_Alchemist_1", 148f, 0.8f, -1, 0.6f),
        };

        // Clip used when a pack has no clip named like the template state.
        private static readonly Dictionary<string, string[]> ClipFallbacks = new Dictionary<string, string[]>
        {
            { "Walking", new[] { "Walking", "Moving Forward" } },
            { "Running", new[] { "Running", "Moving Forward", "Walking" } },
            { "Slashing", new[] { "Slashing", "Attack" } },
            { "Run Slashing", new[] { "Run Slashing", "Attack", "Slashing" } },
            { "Idle Blinking", new[] { "Idle Blinking", "Idle" } },
            { "Jump Start", new[] { "Jump Start", "Idle" } },
            { "Jump Loop", new[] { "Jump Loop", "Idle" } },
            { "Falling Down", new[] { "Falling Down", "Idle" } },
        };

        [MenuItem("Maze Runner/Import Character Roster")]
        public static void Run()
        {
            AssetDatabase.Refresh();
            ScmlImportOptions.options = new ScmlImportOptions(); // 100 pixels per unit, as the existing rigs

            // Body parts are single sprites named like their files (Body, Head, ...), as in the existing rigs;
            // part sets are matched to a rig by these names.
            foreach (Rig rig in PlayerRigs.Concat(EnemyRigs)) UseSingleSprites(rig.Folder);

            var rigPrefabs = new Dictionary<string, GameObject>();
            foreach (Rig rig in PlayerRigs.Concat(EnemyRigs))
            {
                rigPrefabs[rig.Id] = BuildRig(rig);
            }

            ImportPartSet("Assets/Resources/Characters/Dark_Oracle_3", "Assets/Characters/Dark_Oracle");
            ImportPartSet("Assets/Enemies/Zombie_Villager/Skins/Zombie_Villager_3", "Assets/Enemies/Zombie_Villager");
            ImportPartSet("Assets/Enemies/Skeleton_Warrior/Skins/Skeleton_Warrior_3", "Assets/Enemies/Skeleton_Warrior");
            foreach (string id in new[] { "Dark_Oracle_2", "Dark_Oracle_3", "Valkyrie_1" })
            {
                ImportPreview($"Assets/Resources/Characters/{id}/Preview.png");
            }

            // Player rigs.
            var rigEntries = new List<(string id, GameObject prefab, AnimatorController controller)>();
            foreach (Rig rig in PlayerRigs)
            {
                var controller = CloneController(PlayerTemplateController, $"{rig.Folder}/{rig.Id}_Player.controller", rigPrefabs[rig.Id]);
                rigEntries.Add((rig.Id, rigPrefabs[rig.Id], controller));
            }
            RegisterPlayerRigs(rigEntries);

            // Enemies.
            var enemyControllers = new Dictionary<string, AnimatorController>();
            foreach (Rig rig in EnemyRigs)
            {
                enemyControllers[rig.Id] = CloneController(EnemyTemplateController, $"{rig.Folder}/{rig.Id}_Enemy.controller", rigPrefabs[rig.Id]);
            }

            var enemyPrefabs = new Dictionary<string, GameObject>();
            foreach (EnemySpec spec in EnemySpecs)
            {
                Rig rig = EnemyRigs.First(r => r.Id == spec.Rig);
                enemyPrefabs[spec.Name] = BuildEnemy(spec, rig, rigPrefabs[rig.Id], enemyControllers[rig.Id]);
            }

            WriteSpawnTable(enemyPrefabs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RosterImport] Done.");
        }

        public static void RunFromCommandLine()
        {
            bool ok = true;
            try
            {
                Run();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[RosterImport] Failed: " + e);
                ok = false;
            }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        // ------------------------------------------------------------------ rigs

        private static GameObject BuildRig(Rig rig)
        {
            string scmlSource = $"{ArtSource}/{rig.Id}/Animations.scml";
            ScmlObject scml;
            using (var reader = new StreamReader(scmlSource))
            {
                scml = (ScmlObject)new XmlSerializer(typeof(ScmlObject)).Deserialize(reader);
            }

            foreach (var entity in scml.entities)
            {
                if (rig.SourceEntity != null && entity.name == rig.SourceEntity) entity.name = rig.Id;
            }

            // The builder only uses this path's folder (for the part PNGs and the outputs).
            string virtualScmlPath = $"{rig.Folder}/Animations.scml";
            var builder = new PrefabBuilder(new ScmlProcessingInfo());
            // The first pass fixes the parts' texture import settings (pivots from the .scml) and stops.
            if (!builder.Build(scml, virtualScmlPath))
            {
                AssetDatabase.Refresh();
                builder = new PrefabBuilder(new ScmlProcessingInfo());
                if (!builder.Build(scml, virtualScmlPath))
                {
                    throw new System.Exception($"Spriter build failed for {rig.Id}");
                }
            }

            AssetDatabase.SaveAssets();
            string prefabPath = $"{rig.Folder}/{rig.Id}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new System.Exception($"No rig prefab at {prefabPath}");

            int clips = AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<AnimationClip>().Count();
            Debug.Log($"[RosterImport] Rig {rig.Id}: {prefabPath} with {clips} clips");
            return prefab;
        }

        private static void UseSingleSprites(string folder)
        {
            foreach (string path in Directory.GetFiles(folder, "*.png"))
            {
                var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
                if (importer == null || importer.spriteImportMode == SpriteImportMode.Single) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Imports a variant's part PNGs exactly like the same-named parts of the rig they belong to.</summary>
        private static void ImportPartSet(string folder, string rigFolder)
        {
            foreach (string path in Directory.GetFiles(folder, "*.png"))
            {
                string assetPath = path.Replace('\\', '/');
                if (Path.GetFileName(assetPath) == "Preview.png") continue;

                var source = AssetImporter.GetAtPath($"{rigFolder}/{Path.GetFileName(assetPath)}") as TextureImporter;
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (source == null || importer == null)
                {
                    Debug.LogWarning($"[RosterImport] No rig part to copy settings from for {assetPath}");
                    continue;
                }

                var settings = new TextureImporterSettings();
                source.ReadTextureSettings(settings);
                importer.SetTextureSettings(settings);
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = source.maxTextureSize;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Imports a character preview exactly like the existing ones (sprite, centre pivot, full size).</summary>
        private static void ImportPreview(string path)
        {
            var source = AssetImporter.GetAtPath(PreviewTemplate) as TextureImporter;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (source == null || importer == null) return;

            var settings = new TextureImporterSettings();
            source.ReadTextureSettings(settings);
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = source.maxTextureSize;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ controllers

        /// <summary>
        /// Copies an existing controller (same states, parameters and transitions) and points each state at
        /// the rig's clip with the same name, or the closest fallback the pack provides.
        /// </summary>
        private static AnimatorController CloneController(string templatePath, string path, GameObject rigPrefab)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) == null)
            {
                if (!AssetDatabase.CopyAsset(templatePath, path)) throw new System.Exception($"Could not copy {templatePath}");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            string prefabPath = AssetDatabase.GetAssetPath(rigPrefab);
            var clips = AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<AnimationClip>()
                .GroupBy(c => c.name).ToDictionary(g => g.Key, g => g.First());

            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                foreach (ChildAnimatorState child in layer.stateMachine.states)
                {
                    AnimatorState state = child.state;
                    AnimationClip clip = FindClip(clips, state.name);
                    if (clip == null)
                    {
                        Debug.LogWarning($"[RosterImport] {Path.GetFileName(path)}: no clip for state '{state.name}', kept empty");
                        state.motion = null;
                        continue;
                    }
                    if (clip.name != state.name)
                    {
                        Debug.Log($"[RosterImport] {Path.GetFileName(path)}: state '{state.name}' uses clip '{clip.name}'");
                    }
                    state.motion = clip;
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip FindClip(Dictionary<string, AnimationClip> clips, string stateName)
        {
            if (clips.TryGetValue(stateName, out AnimationClip exact)) return exact;
            if (ClipFallbacks.TryGetValue(stateName, out string[] options))
            {
                foreach (string option in options)
                {
                    if (clips.TryGetValue(option, out AnimationClip clip)) return clip;
                }
            }
            return clips.TryGetValue("Idle", out AnimationClip idle) ? idle : null;
        }

        private static void RegisterPlayerRigs(List<(string id, GameObject prefab, AnimatorController controller)> rigs)
        {
            var settings = Resources.Load<PlayerRigSettings>(PlayerRigSettings.ResourcePath);
            if (settings == null) throw new System.Exception("Resources/PlayerRig.asset is missing");

            var so = new SerializedObject(settings);
            SerializedProperty list = so.FindProperty("extraRigs");
            list.arraySize = rigs.Count;
            for (int i = 0; i < rigs.Count; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = rigs[i].id;
                entry.FindPropertyRelative("prefab").objectReferenceValue = rigs[i].prefab;
                entry.FindPropertyRelative("controller").objectReferenceValue = rigs[i].controller;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        // ------------------------------------------------------------------ enemies

        private static GameObject BuildEnemy(EnemySpec spec, Rig rig, GameObject rigPrefab, AnimatorController controller)
        {
            // Never overwrite the rig prefab itself (the Ogre enemy shares its rig's name).
            string path = spec.Name == rig.Id ? $"{rig.Folder}/{spec.Name}_Enemy.prefab" : $"{rig.Folder}/{spec.Name}.prefab";
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            try
            {
                instance.name = spec.Name;
                instance.transform.localScale = new Vector3(spec.Scale, spec.Scale, spec.Scale);

                instance.GetComponent<Animator>().runtimeAnimatorController = controller;

                // Same physics body as the Level 1 goblins (values in rig units, before scaling).
                var body = instance.GetComponent<Rigidbody2D>();
                if (body == null) body = instance.AddComponent<Rigidbody2D>();
                body.gravityScale = 1f;
                body.mass = 1f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;

                var capsule = instance.GetComponent<CapsuleCollider2D>();
                if (capsule == null) capsule = instance.AddComponent<CapsuleCollider2D>();
                capsule.offset = new Vector2(0f, 2.57f);
                capsule.size = new Vector2(2.5f, 5f);
                capsule.direction = CapsuleDirection2D.Vertical;

                Transform attackPoint = instance.transform.Find("AttackPoint");
                if (attackPoint == null)
                {
                    attackPoint = new GameObject("AttackPoint").transform;
                    attackPoint.SetParent(instance.transform, false);
                }
                attackPoint.localPosition = new Vector3(2.2f, 2.6f, 0f);

                var ai = instance.GetComponent<GoblinEnemy>();
                if (ai == null) ai = instance.AddComponent<GoblinEnemy>();
                var so = new SerializedObject(ai);
                so.FindProperty("moveSpeed").floatValue = spec.MoveSpeed;
                so.FindProperty("patrolDistance").floatValue = 1f;
                so.FindProperty("attackRange").floatValue = spec.AttackRange;
                so.FindProperty("attackCooldown").floatValue = spec.AttackCooldown;
                so.FindProperty("attackDamage").intValue = 1;
                so.FindProperty("attackPoint").objectReferenceValue = attackPoint;
                so.FindProperty("attackRadius").floatValue = 0.5f;
                so.FindProperty("playerLayer").intValue = 1 << PlayerLayer;
                so.FindProperty("visionRange").floatValue = 3f;
                so.FindProperty("visionWidth").floatValue = 2f;
                so.FindProperty("noticeSpeedMultiplier").floatValue = 1.6f;
                so.FindProperty("noticeSpeedDuration").floatValue = 2f;
                so.FindProperty("rearDetectionRange").floatValue = 2f;
                so.FindProperty("maxHealth").intValue = spec.Health;
                so.FindProperty("fallDeathY").floatValue = -10f;
                so.ApplyModifiedPropertiesWithoutUndo();

                if (spec.SkinFolder != null)
                {
                    var skin = instance.GetComponent<RigSpriteSkin>();
                if (skin == null) skin = instance.AddComponent<RigSpriteSkin>();
                    var skinSo = new SerializedObject(skin);
                    SerializedProperty parts = skinSo.FindProperty("parts");
                    var sprites = Directory.GetFiles(spec.SkinFolder, "*.png")
                        .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\', '/')))
                        .Where(s => s != null).ToArray();
                    parts.arraySize = sprites.Length;
                    for (int i = 0; i < sprites.Length; i++) parts.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                    skinSo.ApplyModifiedPropertiesWithoutUndo();
                }

                // Like the goblins, enemies carry their weapon visibly while patrolling (the rigs start it
                // transparent and only the attack clips make it opaque).
                foreach (SpriteRenderer part in instance.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (part.name != "Sword" && part.name != "Shield") continue;
                    Color color = part.color;
                    color.a = 1f;
                    part.color = color;
                }

                SetLayerRecursively(instance.transform, EnemyLayer);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
                Debug.Log($"[RosterImport] Enemy {spec.Name}: {path}");
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }

        private static void WriteSpawnTable(Dictionary<string, GameObject> enemyPrefabs)
        {
            const string folder = "Assets/Resources/" + LevelEnemySpawnTable.ResourceFolder;
            const string path = folder + "/Level1.asset";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", LevelEnemySpawnTable.ResourceFolder);

            var table = AssetDatabase.LoadAssetAtPath<LevelEnemySpawnTable>(path);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<LevelEnemySpawnTable>();
                AssetDatabase.CreateAsset(table, path);
            }

            var so = new SerializedObject(table);
            SerializedProperty list = so.FindProperty("spawns");
            list.arraySize = Level1Spawns.Length;
            for (int i = 0; i < Level1Spawns.Length; i++)
            {
                var s = Level1Spawns[i];
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("label").stringValue = s.enemy;
                entry.FindPropertyRelative("prefab").objectReferenceValue = enemyPrefabs[s.enemy];
                entry.FindPropertyRelative("position").vector2Value = new Vector2(s.x, s.y);
                entry.FindPropertyRelative("startDirection").intValue = s.dir;
                entry.FindPropertyRelative("patrolDistance").floatValue = s.patrol;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
        }
    }
}
