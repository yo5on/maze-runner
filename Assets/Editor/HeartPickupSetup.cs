using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MazeRunner.Collectibles;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Places the heart pickups (health hearts, the HUD's red heart from Art/UI/Hearts/hearts.png) on chosen
    /// platforms in Levels 1-3, under each level's "Pickups" object. A heart restores 2 health
    /// (the existing Collectible Health rules) and never grants a spare life. Running it again resets the
    /// hearts to the list below: existing HeartPickup_* objects are moved/resized, missing ones created,
    /// extra ones removed.
    ///
    /// Menu: Maze Runner > Place Heart Pickups
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.HeartPickupSetup.RunFromCommandLine
    ///       (-executeMethod MazeRunner.EditorTools.HeartPickupSetup.DumpFromCommandLine lists each level's platforms)
    /// </summary>
    public static class HeartPickupSetup
    {
        private const string HeartSheet = "Assets/Art/UI/Hearts/hearts.png";
        private const string HeartSprite = "hearts_0"; // the red health heart
        private const float HeartHeight = 0.95f;      // world units: about 40 % of the player's height
        private const float PickupRadius = 0.55f;     // world units, a little larger than the heart
        private const float AbovePlatform = 0.95f;    // heart centre above the platform top (clear of it while bobbing)
        private const int HealAmount = 2;             // unchanged from the hearts already in Levels 1-2

        /// <summary>Platform (by name) and where along its top the heart sits (0 = left end, 1 = right end).</summary>
        private static readonly Dictionary<string, (string platform, float along)[]> Hearts = new Dictionary<string, (string, float)[]>
        {
            // Level 1: raised side platforms just off the main path (the two existing hearts stay put).
            { "Level1", new[] { ("Platform_05", 0.5f), ("Platform_08", 0.5f), ("Platform_10", 0.5f) } },
            // Level 2: the high ledges, a proper jump up each time.
            { "Level2", new[] { ("Platform_08", 0.5f), ("Platform_11", 0.5f), ("Platform_13", 0.5f) } },
            // Level 3: small, high platforms between the harder fights.
            { "Level3", new[] { ("Platform_05", 0.5f), ("Platform_08", 0.5f), ("Platform_13", 0.5f) } },
        };

        [MenuItem("Maze Runner/Place Heart Pickups")]
        public static bool Run()
        {
            Sprite heart = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(HeartSheet))
                if (o is Sprite s && s.name == HeartSprite) heart = s;
            if (heart == null) { Debug.LogError("[Hearts] missing " + HeartSheet + ":" + HeartSprite); return false; }

            bool ok = true;
            foreach (var level in Hearts)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/Levels/" + level.Key + ".unity", OpenSceneMode.Single);
                GameObject container = GameObject.Find("Pickups");
                if (container == null) { Debug.LogError("[Hearts] " + level.Key + " has no Pickups object"); ok = false; continue; }

                var existing = new List<Transform>();
                foreach (Transform child in container.transform)
                    if (child.name.StartsWith("HeartPickup_")) existing.Add(child);
                existing.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

                for (int i = 0; i < level.Value.Length; i++)
                {
                    var (platformName, along) = level.Value[i];
                    Collider2D platform = FindPlatform(platformName);
                    if (platform == null) { Debug.LogError($"[Hearts] {level.Key}: no platform {platformName}"); ok = false; continue; }
                    Bounds b = platform.bounds;
                    var position = new Vector3(Mathf.Lerp(b.min.x + 0.4f, b.max.x - 0.4f, along), b.max.y + AbovePlatform, 0f);

                    GameObject go = i < existing.Count ? existing[i].gameObject : new GameObject();
                    go.name = $"HeartPickup_{i + 1:00}";
                    go.transform.SetParent(container.transform, true);
                    go.transform.position = position;
                    go.transform.rotation = Quaternion.identity;
                    float scale = HeartHeight / heart.bounds.size.y;
                    go.transform.localScale = new Vector3(scale, scale, 1f);

                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = go.AddComponent<SpriteRenderer>(); // no ??: Unity objects compare to null by their own rules
                    sr.sprite = heart;
                    sr.color = Color.white;
                    sr.sortingOrder = 2;
                    var col = go.GetComponent<CircleCollider2D>();
                    if (col == null) col = go.AddComponent<CircleCollider2D>(); // no ??: Unity objects compare to null by their own rules
                    col.isTrigger = true;
                    col.radius = PickupRadius / scale;
                    col.offset = Vector2.zero;
                    var pickup = go.GetComponent<Collectible>();
                    if (pickup == null) pickup = go.AddComponent<Collectible>(); // no ??: Unity objects compare to null by their own rules
                    var so = new SerializedObject(pickup);
                    so.FindProperty("type").enumValueIndex = (int)CollectibleType.Health;
                    so.FindProperty("value").intValue = HealAmount;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[Hearts] {level.Key} {go.name} on {platformName} at ({position.x:0.00}, {position.y:0.00})");
                }
                for (int i = level.Value.Length; i < existing.Count; i++)
                {
                    Debug.Log($"[Hearts] {level.Key}: removed {existing[i].name}");
                    Object.DestroyImmediate(existing[i].gameObject);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log(ok ? "[Hearts] PLACE OK" : "[Hearts] PLACE FAILED");
            return ok;
        }

        private static Collider2D FindPlatform(string name)
        {
            foreach (Collider2D c in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (c.name == name && !c.isTrigger) return c;
            return null;
        }

        /// <summary>Lists each level's platforms, hazards, checkpoints, enemies and pickups (to choose heart spots).</summary>
        public static void Dump()
        {
            var sb = new StringBuilder();
            foreach (string level in Hearts.Keys)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Levels/" + level + ".unity", OpenSceneMode.Single);
                sb.Append("== ").Append(level).Append('\n');
                foreach (Collider2D c in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    Bounds b = c.bounds;
                    string kind = c.isTrigger ? "T" : (c.gameObject.layer == 3 ? "G" : "C");
                    var mono = c.GetComponent<MonoBehaviour>();
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} [{2:0.00},{3:0.00}]x[{4:0.00},{5:0.00}] layer={6} {7}\n",
                        kind, c.name, b.min.x, b.max.x, b.min.y, b.max.y, c.gameObject.layer, mono != null ? mono.GetType().Name : "");
                }
            }
            System.IO.File.WriteAllText("Temp/level_dump.txt", sb.ToString());
            Debug.Log("[Hearts] dump written to Temp/level_dump.txt");
        }

        public static void RunFromCommandLine() => EditorApplication.Exit(Run() ? 0 : 1);

        public static void DumpFromCommandLine()
        {
            Dump();
            EditorApplication.Exit(0);
        }
    }
}
