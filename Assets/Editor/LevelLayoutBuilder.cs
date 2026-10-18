using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Collectibles;
using MazeRunner.Enemies;
using MazeRunner.Levels;
using MazeRunner.Player;
using MazeRunner.UI;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Builds the layouts of Levels 1-3 from the data below: platforms, moving platforms, spike
    /// traps, enemies, checkpoints, health pickups, the start and the goal. Everything it makes
    /// lives under a "Layout" object in each scene, so running it again replaces the layout
    /// without touching the rest of the scene (UI, camera, managers, player).
    ///
    /// Difficulty comes from the layout, not the player. Measured with Level 1's player
    /// (jumpForce 9): a full running jump rises about 4 units and clears about 10 units of
    /// level gap. Level 1 (Difficult) uses gaps of 4-5 and rises up to 2.25; Level 2 (Hard)
    /// 5.5-6 with rises up to 3, timed spikes and moving platforms; Level 3 (Very Hard) 6-8 onto
    /// narrow ledges, chained moving platforms, spike gauntlets and the boss. Recovery gets
    /// sparser: 3 checkpoints + 2 hearts, 2 + 1, then a single checkpoint.
    ///
    /// Enemies (each has its own EnemyBehaviour personality): Level 1 introduces every type on its
    /// own; Level 2 pairs abilities that cover each other (shield guard + alchemist, duelist + ogre,
    /// crusader charge + zombie lunge, wraith + alchemist); Level 3 puts lungers and snipers on narrow
    /// landings and ends with a crusader and a duelist before the Boss Goblin.
    ///
    /// All three levels use Level 1's player settings (7 hearts, jumpForce 9).
    ///
    /// Menu: Maze Runner > Build Level Layouts
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.LevelLayoutBuilder.BuildFromCommandLine
    /// </summary>
    public static class LevelLayoutBuilder
    {
        private const string PlatformPrefabPath = "Assets/Prefabs/Environment/Platform_03.prefab";
        private const string GoblinPrefabPath = "Assets/Enemies/Goblin/Goblin_Enemy.prefab";
        private const string BossPrefabPath = "Assets/Enemies/Goblin/Goblin_Boss.prefab";
        private const string SpikeTexturePath = "Assets/Art/Environment/Spikes.png";
        private const string RouteFile = "Logs/level-routes.json";

        private const int PlayerMaxHealth = 7;
        private const float PlayerJumpForce = 9f;

        private static readonly Color MoverColor = new Color32(0xaf, 0xae, 0xa2, 0xff);   // stone
        private static readonly Color SpikeColor = new Color32(0xd2, 0x13, 0x19, 0xff);   // crimson
        private static readonly Color GoalColor = new Color(1f, 0.85f, 0.35f, 0.9f);

        // ------------------------------------------------------------------ data

        private class Level
        {
            public string Scene;
            public float StartX, StartTop;
            public readonly List<(float l, float r, float top)> Platforms = new List<(float, float, float)>();
            public readonly List<(float cx, float top, float w, float dx, float dy, float period, float phase)> Movers = new List<(float, float, float, float, float, float, float)>();
            public readonly List<(float l, float r, float top)> Spikes = new List<(float, float, float)>();
            public readonly List<(float l, float r, float top, float on, float off, float phase)> Timed = new List<(float, float, float, float, float, float)>();
            public readonly List<(string kind, float x, float top, int dir, float patrol)> Enemies = new List<(string, float, float, int, float)>();
            public readonly List<(float x, float top)> Checkpoints = new List<(float, float)>();
            public readonly List<(float x, float top)> Hearts = new List<(float, float)>();
            public float GoalX, GoalTop;

            public Level P(float l, float r, float top) { Platforms.Add((l, r, top)); return this; }
            public Level Mover(float cx, float top, float w, float dx, float dy, float period, float phase = 0f) { Movers.Add((cx, top, w, dx, dy, period, phase)); return this; }
            public Level Spike(float l, float r, float top) { Spikes.Add((l, r, top)); return this; }
            public Level Trap(float l, float r, float top, float on, float off, float phase) { Timed.Add((l, r, top, on, off, phase)); return this; }
            public Level E(string kind, float x, float top, int dir = 1, float patrol = 1.5f) { Enemies.Add((kind, x, top, dir, patrol)); return this; }
            public Level CP(float x, float top) { Checkpoints.Add((x, top)); return this; }
            public Level Heart(float x, float top) { Hearts.Add((x, top)); return this; }
            public Level Goal(float x, float top) { GoalX = x; GoalTop = top; return this; }
        }

        private static Level[] Levels()
        {
            // Platforms are (left edge, right edge, top surface). Gaps are edge to edge.
            var l1 = new Level { Scene = "Assets/Scenes/Levels/Level1.unity", StartX = -7f, StartTop = -1.25f }
                .P(-10f, 6f, -1.25f)
                .P(10f, 16f, -0.25f).E("Villager_Zombie_1", 13f, -0.25f, 1, 1.5f)
                .P(20f, 24f, 1.25f).E("Goblin", 22f, 1.25f, -1, 1f)
                .P(28.5f, 37f, 0.25f).Spike(31.5f, 33f, 0.25f).E("Goblin", 35.5f, 0.25f, -1, 1f)
                .P(41.5f, 45f, 2.25f)
                .P(49.5f, 60f, 0.75f).CP(51f, 0.75f).E("Villager_Zombie_3", 56f, 0.75f, -1, 1.5f)
                .Mover(65.5f, 0.75f, 3f, 5f, 0f, 4.5f)
                .P(76f, 84f, 0.75f).E("Skeleton_Warrior_2", 80f, 0.75f, 1, 2f)
                .P(88.5f, 92f, 3f).Heart(90.25f, 3f)
                .P(96.5f, 107f, 1.25f).Spike(101f, 102f, 1.25f).E("Goblin", 99.5f, 1.25f, 1, 1f).E("Goblin", 104.5f, 1.25f, -1, 1f)
                .P(111.5f, 115f, 3.25f).Heart(113.25f, 3.25f)
                .P(120f, 132f, 0.75f).CP(121.5f, 0.75f).E("Skeleton_Warrior_3", 127f, 0.75f, -1, 2f)
                .P(137f, 147f, 0.75f).Trap(139.5f, 144.5f, 0.75f, 1.4f, 1.4f, 0f).E("Wraith_3", 145.5f, 0.75f, -1, 1f)
                .P(151.5f, 156f, 2.25f).E("Goblin", 154f, 2.25f, -1, 1f)
                .P(161f, 174f, 0.75f).CP(162.5f, 0.75f).E("Ogre", 168f, 0.75f, -1, 1.2f)
                .P(178.5f, 183f, 2.25f).E("Skeleton_Crusader_1", 181f, 2.25f, -1, 0.6f)
                .P(188f, 202f, 0.75f).E("Bloody_Alchemist_1", 196f, 0.75f, -1, 1f)
                .Goal(200.5f, 0.75f);

            var l2 = new Level { Scene = "Assets/Scenes/Levels/Level2.unity", StartX = -7f, StartTop = -1.25f }
                .P(-10f, 5f, -1.25f)
                .P(10.5f, 14f, 0.75f)
                .P(19.5f, 30f, 0.25f).Spike(22.5f, 24f, 0.25f).Spike(26.5f, 28f, 0.25f).E("Goblin", 25.25f, 0.25f, 1, 0.6f)
                .P(35.5f, 38.5f, 2.75f)
                .P(44.5f, 47.5f, 2.75f)
                .P(53.5f, 66f, 0.75f).CP(55f, 0.75f).E("Skeleton_Warrior_2", 59f, 0.75f, -1, 1.5f).E("Bloody_Alchemist_1", 64.5f, 0.75f, -1, 0.8f)
                .Mover(71.5f, 0.75f, 3f, 0f, 6f, 5f)
                .P(77f, 81f, 7.25f)
                .P(86.5f, 90f, 5.25f).E("Goblin", 88.25f, 5.25f, -1, 0.8f)
                .P(96f, 108f, 2.75f).Trap(98f, 101f, 2.75f, 1.2f, 1.2f, 0f).Trap(102f, 105f, 2.75f, 1.2f, 1.2f, 1.2f).E("Villager_Zombie_3", 106.5f, 2.75f, -1, 0.8f)
                .Mover(114.25f, 2.75f, 2.5f, 7f, 0f, 5f)
                .P(128f, 140f, 2.75f).CP(129.5f, 2.75f).E("Skeleton_Warrior_3", 133f, 2.75f, -1, 1.5f).E("Ogre", 138f, 2.75f, -1, 1f)
                .P(146f, 149f, 5f).Heart(147.5f, 5f)
                .P(155f, 158f, 2f)
                .P(164f, 167f, 4.5f)
                .P(173f, 190f, 1.25f).E("Skeleton_Crusader_1", 177f, 1.25f, -1, 1.2f).Trap(180f, 183f, 1.25f, 1.0f, 1.3f, 0.5f)
                    .E("Villager_Zombie_3", 187f, 1.25f, -1, 0.8f)
                .P(196f, 210f, 1.25f).E("Wraith_3", 200f, 1.25f, -1, 1.2f).E("Bloody_Alchemist_1", 206f, 1.25f, -1, 0.8f)
                .Goal(208.5f, 1.25f);

            var l3 = new Level { Scene = "Assets/Scenes/Levels/Level3.unity", StartX = -7f, StartTop = -1.25f }
                .P(-10f, 4f, -1.25f)
                .P(10.5f, 13f, 1.25f)
                .P(20f, 22.5f, 1.75f)
                .P(29.5f, 40f, 0.25f).Trap(31f, 34f, 0.25f, 1.0f, 1.4f, 0f).Trap(34.5f, 37.5f, 0.25f, 1.0f, 1.4f, 0.8f).E("Goblin", 38.75f, 0.25f, -1, 0.5f)
                .Mover(47.25f, 0.25f, 2.5f, 8f, 0f, 4f)
                .Mover(63.25f, 0.25f, 2.5f, 0f, 5f, 3.5f, 0.5f)
                .P(70f, 72.5f, 5.75f)
                .P(80.5f, 83f, 4.25f).E("Villager_Zombie_3", 81.75f, 4.25f, 1, 0.5f)
                .P(90.5f, 103f, 2.25f).CP(92f, 2.25f).Spike(95f, 96.5f, 2.25f).Spike(99.5f, 101f, 2.25f).E("Skeleton_Warrior_2", 98f, 2.25f, -1, 1f).E("Bloody_Alchemist_1", 102f, 2.25f, -1, 0.5f)
                .P(109f, 111.5f, 4.75f)
                .P(119f, 121.5f, 4.75f).E("Wraith_3", 120.25f, 4.75f, -1, 0.5f)
                .P(129.5f, 132f, 2.75f)
                .Mover(139f, 2.75f, 2f, 10f, 0f, 5f)
                .P(157f, 169f, 2.75f).Trap(158.5f, 161f, 2.75f, 1.0f, 1.1f, 0f).Trap(161.5f, 164f, 2.75f, 1.0f, 1.1f, 0.7f).Trap(164.5f, 167f, 2.75f, 1.0f, 1.1f, 1.4f)
                    .E("Ogre", 168f, 2.75f, -1, 0.5f)
                .P(175f, 177.5f, 5f).E("Skeleton_Crusader_1", 176.25f, 5f, -1, 0.5f)
                .P(185f, 187.5f, 2.5f)
                .P(195f, 197.5f, 4f).E("Bloody_Alchemist_1", 196.25f, 4f, -1, 0.5f)
                // Boss arena: a shield guard and a duelist, then the boss in front of the goal.
                .P(205f, 233f, 1.25f).E("Skeleton_Crusader_1", 210f, 1.25f, -1, 1f).E("Skeleton_Warrior_3", 215f, 1.25f, -1, 1.5f).E("Boss", 224f, 1.25f, -1, 0f)
                .Goal(231.5f, 1.25f);

            return new[] { l1, l2, l3 };
        }

        // ------------------------------------------------------------- entry points

        [MenuItem("Maze Runner/Build Level Layouts")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
        }

        public static void BuildFromCommandLine()
        {
            bool ok = true;
            try { BuildAll(); }
            catch (System.Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static void BuildAll()
        {
            Level[] levels = Levels();
            Sprite spike = EnsureSpikeSprite();
            EnsureGoblinPrefabs(levels[0].Scene);

            var enemyPrefabs = new Dictionary<string, GameObject>
            {
                { "Goblin", AssetDatabase.LoadAssetAtPath<GameObject>(GoblinPrefabPath) },
                { "Boss", AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath) },
                { "Villager_Zombie_1", Load("Assets/Enemies/Zombie_Villager/Villager_Zombie_1.prefab") },
                { "Villager_Zombie_3", Load("Assets/Enemies/Zombie_Villager/Villager_Zombie_3.prefab") },
                { "Skeleton_Warrior_2", Load("Assets/Enemies/Skeleton_Warrior/Skeleton_Warrior_2.prefab") },
                { "Skeleton_Warrior_3", Load("Assets/Enemies/Skeleton_Warrior/Skeleton_Warrior_3.prefab") },
                { "Skeleton_Crusader_1", Load("Assets/Enemies/Skeleton_Crusader/Skeleton_Crusader_1.prefab") },
                { "Bloody_Alchemist_1", Load("Assets/Enemies/Bloody_Alchemist/Bloody_Alchemist_1.prefab") },
                { "Wraith_3", Load("Assets/Enemies/Wraith/Wraith_3.prefab") },
                { "Ogre", Load("Assets/Enemies/Ogre/Ogre_Enemy.prefab") },
            };

            // Hearts HUD and heart sprite come from Level 1, the finished scene.
            Scene level1 = EditorSceneManager.OpenScene(levels[0].Scene, OpenSceneMode.Single);
            HealthDisplay l1Display = Object.FindAnyObjectByType<HealthDisplay>(FindObjectsInactive.Include);
            Sprite heartSprite = (Sprite)new SerializedObject(l1Display).FindProperty("fullHeartSprite").objectReferenceValue;

            var routes = new StringBuilder("{\n");
            for (int i = 0; i < levels.Length; i++)
            {
                BuildLevel(levels[i], spike, heartSprite, enemyPrefabs, levels[0].Scene);
                routes.Append(RouteJson(levels[i])).Append(i < levels.Length - 1 ? ",\n" : "\n");
            }
            routes.Append("}\n");
            Directory.CreateDirectory(Path.GetDirectoryName(RouteFile));
            File.WriteAllText(RouteFile, routes.ToString());

            // Enemies are placed in the scenes now; the old runtime spawn table would add a second set.
            const string table = "Assets/Resources/LevelEnemies/Level1.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(table) != null) AssetDatabase.DeleteAsset(table);

            AssetDatabase.SaveAssets();
            Debug.Log("[LevelLayoutBuilder] Built " + levels.Length + " levels. Routes: " + RouteFile);
        }

        private static GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) throw new System.Exception("Missing prefab " + path);
            return go;
        }

        // ----------------------------------------------------------------- per level

        private static void BuildLevel(Level level, Sprite spike, Sprite heart, Dictionary<string, GameObject> enemies, string level1Path)
        {
            Scene scene = EditorSceneManager.OpenScene(level.Scene, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();

            GameObject checkpointTemplate = roots.FirstOrDefault(r => r.name == "Checkpoint");
            if (checkpointTemplate == null)
            {
                Transform existing = roots.Select(r => r.transform.Find("Checkpoints")).FirstOrDefault(t => t != null && t.childCount > 0);
                checkpointTemplate = existing != null ? existing.GetChild(0).gameObject : null;
            }
            if (checkpointTemplate == null)
            {
                GameObject oldLayout = roots.FirstOrDefault(r => r.name == "Layout");
                CheckpointInLayout(oldLayout, ref checkpointTemplate);
            }
            if (checkpointTemplate == null) throw new System.Exception(level.Scene + ": no Checkpoint to copy");
            GameObject cpCopy = Object.Instantiate(checkpointTemplate);
            cpCopy.name = "CheckpointTemplate";
            cpCopy.SetActive(false);

            // Remove the previous layout (old hand-placed objects or an earlier build).
            foreach (GameObject root in roots)
            {
                string n = root.name;
                bool old = n == "Layout" || n == "Ground" || n == "Checkpoint" || n == "LevelGoal" || n == "Enemy_01" ||
                           n.StartsWith("Platform_") || n.StartsWith("Goblin-_") || n == "BossGoblin";
                if (old) Object.DestroyImmediate(root);
                else if (n == "Square") root.SetActive(false); // white placeholder square
            }

            var layout = new GameObject("Layout").transform;
            Transform platforms = Group(layout, "Platforms");
            Transform movers = Group(layout, "MovingPlatforms");
            Transform hazards = Group(layout, "Hazards");
            Transform foes = Group(layout, "Enemies");
            Transform recovery = Group(layout, "Checkpoints");
            Transform pickups = Group(layout, "Pickups");

            var platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatformPrefabPath);
            Sprite square = platformPrefab.GetComponent<SpriteRenderer>().sprite;

            int index = 1;
            foreach (var p in level.Platforms)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab, scene);
                go.name = $"Platform_{index++:00}";
                go.transform.SetParent(platforms, false);
                go.transform.position = new Vector3((p.l + p.r) / 2f, p.top - 0.25f, 0f);
                go.transform.localScale = new Vector3(p.r - p.l, 0.5f, 1f);
            }

            index = 1;
            foreach (var m in level.Movers)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab, scene);
                go.name = $"MovingPlatform_{index++:00}";
                go.transform.SetParent(movers, false);
                go.transform.position = new Vector3(m.cx, m.top - 0.25f, 0f);
                go.transform.localScale = new Vector3(m.w, 0.5f, 1f);
                go.GetComponent<SpriteRenderer>().color = MoverColor;
                var body = go.GetComponent<Rigidbody2D>();
                if (body == null) body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                var mover = go.AddComponent<MovingPlatform>();
                mover.Configure(new Vector2(m.dx, m.dy), m.period, m.phase);
            }

            index = 1;
            foreach (var s in level.Spikes) MakeSpikes(hazards, spike, $"Spikes_{index++:00}", s.l, s.r, s.top, null);
            foreach (var t in level.Timed) MakeSpikes(hazards, spike, $"SpikeTrap_{index++:00}", t.l, t.r, t.top, (t.on, t.off, t.phase));

            var count = new Dictionary<string, int>();
            foreach (var e in level.Enemies)
            {
                GameObject prefab = enemies[e.kind];
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                count[e.kind] = count.TryGetValue(e.kind, out int c) ? c + 1 : 1;
                go.name = e.kind == "Boss" ? "BossGoblin" : $"{e.kind}_{count[e.kind]:00}";
                go.transform.SetParent(foes, true);
                go.transform.position = new Vector3(e.x, e.top + 0.1f, 0f);
                var goblin = go.GetComponent<GoblinEnemy>();
                if (goblin != null)
                {
                    var so = new SerializedObject(goblin);
                    so.FindProperty("startDirection").intValue = e.dir;
                    so.FindProperty("patrolDistance").floatValue = e.patrol;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            index = 1;
            foreach (var cp in level.Checkpoints)
            {
                GameObject go = Object.Instantiate(cpCopy, recovery);
                go.name = $"Checkpoint_{index++:00}";
                go.SetActive(true);
                go.transform.position = new Vector3(cp.x, cp.top + 0.6f, 0f);
                go.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }
            Object.DestroyImmediate(cpCopy);

            index = 1;
            foreach (var h in level.Hearts)
            {
                var go = new GameObject($"HeartPickup_{index++:00}");
                go.transform.SetParent(pickups, false);
                go.transform.position = new Vector3(h.x, h.top + 0.9f, 0f);
                go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = heart;
                sr.sortingOrder = 2;
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.6f;
                var pickup = go.AddComponent<Collectible>();
                var so = new SerializedObject(pickup);
                so.FindProperty("type").enumValueIndex = (int)CollectibleType.Health;
                so.FindProperty("value").intValue = 2;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            MakeGoal(layout, square, level.GoalX, level.GoalTop);
            SetUpPlayer(level);
            if (level.Scene != level1Path) MatchLevel1Hud(scene, level1Path);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LevelLayoutBuilder] {Path.GetFileNameWithoutExtension(level.Scene)}: {level.Platforms.Count} platforms, " +
                      $"{level.Movers.Count} moving, {level.Spikes.Count + level.Timed.Count} spike strips, {level.Enemies.Count} enemies, " +
                      $"{level.Checkpoints.Count} checkpoints, {level.Hearts.Count} hearts");
        }

        private static void CheckpointInLayout(GameObject oldLayout, ref GameObject template)
        {
            if (oldLayout == null) return;
            Transform group = oldLayout.transform.Find("Checkpoints");
            if (group != null && group.childCount > 0) template = group.GetChild(0).gameObject;
        }

        private static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static void MakeSpikes(Transform parent, Sprite sprite, string name, float l, float r, float top,
            (float on, float off, float phase)? timing)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((l + r) / 2f, top, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(r - l, 0.5f);
            sr.color = SpikeColor;
            sr.sortingOrder = 1;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(r - l - 0.2f, 0.35f);
            col.offset = new Vector2(0f, 0.2f);
            go.AddComponent<Hazard>();
            if (timing.HasValue)
            {
                var trap = go.AddComponent<TimedHazard>();
                trap.Configure(timing.Value.on, timing.Value.off, timing.Value.phase);
            }
        }

        private static void MakeGoal(Transform parent, Sprite square, float x, float top)
        {
            var go = new GameObject("LevelGoal");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, top + 1.25f, 0f);
            go.transform.localScale = new Vector3(1.2f, 2.5f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = GoalColor;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var goal = go.AddComponent<LevelGoal>();
            var so = new SerializedObject(goal);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("inactiveColor").colorValue = GoalColor;
            so.FindProperty("completedColor").colorValue = new Color(0.3f, 0.85f, 0.4f, 0.9f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetUpPlayer(Level level)
        {
            var controller = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (controller == null) throw new System.Exception(level.Scene + ": no player");
            controller.transform.position = new Vector3(level.StartX, level.StartTop + 1f, controller.transform.position.z);

            var so = new SerializedObject(controller);
            so.FindProperty("jumpForce").floatValue = PlayerJumpForce;
            so.ApplyModifiedPropertiesWithoutUndo();

            var health = controller.GetComponent<PlayerHealth>();
            if (health != null)
            {
                var hso = new SerializedObject(health);
                hso.FindProperty("maxHealth").intValue = PlayerMaxHealth;
                hso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Levels 2 and 3 were stubs: their HealthDisplay had no heart container (a "Health: 3 / 3"
        /// text showed instead) and their customization panel was left open. Copy Level 1's hearts.
        /// </summary>
        private static void MatchLevel1Hud(Scene scene, string level1Path)
        {
            HealthDisplay display = Object.FindObjectsByType<HealthDisplay>(FindObjectsInactive.Include)
                .FirstOrDefault(d => d.gameObject.scene == scene);
            if (display == null) return;
            Transform canvas = display.transform.parent;

            Transform existingHearts = canvas.Find("HeartContainer");
            if (existingHearts == null)
            {
                Scene l1 = EditorSceneManager.OpenScene(level1Path, OpenSceneMode.Additive);
                HealthDisplay l1Display = Object.FindObjectsByType<HealthDisplay>(FindObjectsInactive.Include)
                    .First(d => d.gameObject.scene == l1);
                var l1So = new SerializedObject(l1Display);
                var l1Container = (GameObject)l1So.FindProperty("heartContainer").objectReferenceValue;

                GameObject hearts = Object.Instantiate(l1Container);
                hearts.name = "HeartContainer";
                SceneManager.MoveGameObjectToScene(hearts, scene);
                hearts.transform.SetParent(canvas, false);
                hearts.transform.SetSiblingIndex(display.transform.GetSiblingIndex() + 1);
                var src = (RectTransform)l1Container.transform;
                var dst = (RectTransform)hearts.transform;
                dst.anchorMin = src.anchorMin; dst.anchorMax = src.anchorMax; dst.pivot = src.pivot;
                dst.anchoredPosition = src.anchoredPosition; dst.sizeDelta = src.sizeDelta;
                foreach (Transform child in hearts.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);

                var so = new SerializedObject(display);
                so.FindProperty("heartContainer").objectReferenceValue = hearts;
                so.FindProperty("heartPrefab").objectReferenceValue = l1So.FindProperty("heartPrefab").objectReferenceValue;
                so.FindProperty("fullHeartSprite").objectReferenceValue = l1So.FindProperty("fullHeartSprite").objectReferenceValue;
                so.FindProperty("emptyHeartSprite").objectReferenceValue = l1So.FindProperty("emptyHeartSprite").objectReferenceValue;
                so.FindProperty("playerHealth").objectReferenceValue = Object.FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include)
                    .First(h => h.gameObject.scene == scene);
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.CloseScene(l1, true);
            }

            Transform text = canvas.Find("HealthText");
            if (text != null) text.gameObject.SetActive(false);
            Transform customization = canvas.Find("CustomizationPanel");
            if (customization != null) customization.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ assets

        /// <summary>Goblin and boss prefabs made once from Level 1's original scene goblins.</summary>
        private static void EnsureGoblinPrefabs(string level1Path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GoblinPrefabPath) != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath) != null) return;

            Scene scene = EditorSceneManager.OpenScene(level1Path, OpenSceneMode.Single);
            GameObject goblin = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Goblin-_01");
            GameObject boss = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "BossGoblin");
            if (goblin == null || boss == null) throw new System.Exception("Level 1's original goblins are gone; cannot make the goblin prefabs.");
            SaveCopyAsPrefab(goblin, GoblinPrefabPath, "Goblin");
            SaveCopyAsPrefab(boss, BossPrefabPath, "BossGoblin");
        }

        private static void SaveCopyAsPrefab(GameObject source, string path, string name)
        {
            GameObject copy = Object.Instantiate(source);
            copy.name = name;
            copy.transform.position = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(copy, path);
            Object.DestroyImmediate(copy);
        }

        /// <summary>A row of spikes (white, tinted by the renderer), one spike per unit, pivot at the base.</summary>
        private static Sprite EnsureSpikeSprite()
        {
            if (!File.Exists(SpikeTexturePath))
            {
                const int w = 32, h = 16;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var clear = new Color(1f, 1f, 1f, 0f);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // One triangle per tile: full width at the base, a point at the top.
                    float half = (w / 2f) * (1f - (y + 0.5f) / h);
                    bool inside = Mathf.Abs(x + 0.5f - w / 2f) <= half;
                    tex.SetPixel(x, y, inside ? (y < 3 ? new Color(0.8f, 0.8f, 0.8f, 1f) : Color.white) : clear);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(SpikeTexturePath));
                File.WriteAllBytes(SpikeTexturePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(SpikeTexturePath);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(SpikeTexturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpikeTexturePath);
        }

        // ---------------------------------------------------------------- test data

        /// <summary>The route as JSON (for the browser reachability test).</summary>
        private static string RouteJson(Level level)
        {
            string F(float v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.Append($"\"{Path.GetFileNameWithoutExtension(level.Scene)}\": {{\"start\": [{F(level.StartX)}, {F(level.StartTop)}], ");
            sb.Append($"\"goal\": [{F(level.GoalX)}, {F(level.GoalTop)}], \"platforms\": [");
            sb.Append(string.Join(", ", level.Platforms.Select(p => $"[{F(p.l)}, {F(p.r)}, {F(p.top)}]")));
            sb.Append("], \"movers\": [");
            sb.Append(string.Join(", ", level.Movers.Select(m => $"[{F(m.cx)}, {F(m.top)}, {F(m.w)}, {F(m.dx)}, {F(m.dy)}, {F(m.period)}, {F(m.phase)}]")));
            sb.Append("], \"enemies\": ").Append(level.Enemies.Count).Append("}");
            return sb.ToString();
        }
    }
}
