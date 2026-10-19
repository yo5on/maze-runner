using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MazeRunner.Enemies;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Tools for the Enemy Intel panel (MazeRunner.UI.EnemyIntelPanel):
    /// - Capture Enemy Portraits: renders each enemy prefab (with its variant skin, in its Idle pose)
    ///   to a transparent PNG in Assets/Resources/EnemyPortraits, named by the intel entry's id.
    /// - Verify Enemy Intel: compares every figure in <see cref="EnemyIntel"/> that names a serialized
    ///   field with that field on the prefab, and checks each prefab has the expected behaviour and
    ///   health. Logs one line per entry and fails on any mismatch.
    ///
    /// Menu: Maze Runner > Capture Enemy Portraits / Verify Enemy Intel
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.EnemyIntelTools.RunFromCommandLine
    ///       (verifies, then captures)
    /// </summary>
    public static class EnemyIntelTools
    {
        private const string PortraitFolder = "Assets/Resources/EnemyPortraits";
        private const int PortraitSize = 512;

        // ------------------------------------------------------------------ verify

        [MenuItem("Maze Runner/Verify Enemy Intel")]
        public static bool Verify()
        {
            int problems = 0;
            foreach (EnemyIntelEntry entry in EnemyIntel.All)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.Prefab);
                if (prefab == null) { Problem(ref problems, entry, "prefab not found: " + entry.Prefab); continue; }
                var body = prefab.GetComponent<GoblinEnemy>();
                var behaviour = prefab.GetComponent(entry.Behaviour) as EnemyBehaviour;
                if (body == null) { Problem(ref problems, entry, "no GoblinEnemy"); continue; }
                if (behaviour == null) { Problem(ref problems, entry, "no " + entry.Behaviour.Name); continue; }
                if (prefab.GetComponents<EnemyBehaviour>().Length != 1) Problem(ref problems, entry, "more than one EnemyBehaviour");

                var bodyData = new SerializedObject(body);
                var behaviourData = new SerializedObject(behaviour);
                int health = bodyData.FindProperty("maxHealth").intValue;
                if (health != entry.Health) Problem(ref problems, entry, $"health {entry.Health} shown, prefab has {health}");

                var checkedFields = new List<string>();
                foreach (IntelStat stat in entry.Stats)
                {
                    for (int i = 0; i < stat.Fields.Length; i++)
                    {
                        string field = stat.Fields[i];
                        SerializedObject source = behaviourData;
                        string name = field;
                        if (field.StartsWith("GoblinEnemy.", StringComparison.Ordinal))
                        {
                            source = bodyData;
                            name = field.Substring("GoblinEnemy.".Length);
                        }
                        SerializedProperty property = source.FindProperty(name);
                        if (property == null) { Problem(ref problems, entry, $"{stat.Label}: no field '{field}'"); continue; }
                        float actual = property.propertyType == SerializedPropertyType.Integer ? property.intValue : property.floatValue;
                        float shown = i == 0 ? stat.Min : stat.Max;
                        if (Mathf.Abs(actual - shown) > 0.0001f) Problem(ref problems, entry, $"{stat.Label}: shows {shown}, {field} = {actual}");
                        checkedFields.Add(field + "=" + actual);
                    }
                }
                Debug.Log($"[EnemyIntel] {entry.Name}: {entry.Behaviour.Name} on {Path.GetFileName(entry.Prefab)}, health {health}, " +
                          $"checked {string.Join(", ", checkedFields)}");
            }
            Debug.Log(problems == 0 ? "[EnemyIntel] VERIFY OK: every figure matches its prefab." : $"[EnemyIntel] VERIFY FAILED: {problems} problem(s).");
            return problems == 0;
        }

        private static void Problem(ref int count, EnemyIntelEntry entry, string message)
        {
            count++;
            Debug.LogError($"[EnemyIntel] {entry.Name}: {message}");
        }

        // ------------------------------------------------------------------ capture

        [MenuItem("Maze Runner/Capture Enemy Portraits")]
        public static void CapturePortraits()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(PortraitFolder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            var unlitMaterial = new Material(unlit);

            var cameraObject = new GameObject("PortraitCamera");
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;

            var target = new RenderTexture(PortraitSize, PortraitSize, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = target;

            foreach (EnemyIntelEntry entry in EnemyIntel.All)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.Prefab);
                if (prefab == null) { Debug.LogError("[EnemyIntel] missing " + entry.Prefab); continue; }
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    enemy.transform.position = Vector3.zero;
                    Vector3 s = enemy.transform.localScale;
                    enemy.transform.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 1f); // all face right
                    PoseIdle(enemy);
                    ApplyVariantSkin(enemy);

                    var renderers = new List<SpriteRenderer>();
                    foreach (SpriteRenderer sr in enemy.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (Environment.GetCommandLineArgs().Contains("-intelDebug"))
                            Debug.Log($"[EnemyIntel] {entry.Id} part {sr.name} sprite={(sr.sprite ? sr.sprite.name : "-")} en={sr.enabled} active={sr.gameObject.activeInHierarchy} a={sr.color.a:F2} order={sr.sortingOrder} pos={sr.transform.position}");
                        if (sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy || sr.color.a < 0.05f ||
                            sr.name.IndexOf("FX", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            sr.enabled = false;
                            continue;
                        }
                        sr.sharedMaterial = unlitMaterial;
                        renderers.Add(sr);
                    }
                    if (renderers.Count == 0) { Debug.LogError("[EnemyIntel] nothing to draw for " + entry.Id); continue; }

                    Bounds bounds = renderers[0].bounds;
                    foreach (SpriteRenderer sr in renderers) bounds.Encapsulate(sr.bounds);
                    float half = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.06f;
                    camera.orthographicSize = half;
                    cameraObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);

                    // The first render after loading can miss textures that are still uploading; render twice.
                    camera.Render();
                    camera.Render();
                    SavePng(target, Path.Combine(PortraitFolder, entry.Id + ".png"));
                    Debug.Log($"[EnemyIntel] portrait {entry.Id}: {renderers.Count} parts, {bounds.size.x:F2} x {bounds.size.y:F2} units");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(enemy);
                }
            }

            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            foreach (EnemyIntelEntry entry in EnemyIntel.All) ConfigureImporter(Path.Combine(PortraitFolder, entry.Id + ".png").Replace('\\', '/'));
            AssetDatabase.SaveAssets();
        }

        /// <summary>Samples the first frame of the rig's Idle clip so the portrait shows a standing pose.</summary>
        private static void PoseIdle(GameObject enemy)
        {
            var animator = enemy.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            AnimationClip idle = null;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "Idle") { idle = clip; break; }
                if (idle == null && clip.name.StartsWith("Idle", StringComparison.Ordinal)) idle = clip;
            }
            if (idle != null) idle.SampleAnimation(animator.gameObject, 0f);
        }

        /// <summary>Does what RigSpriteSkin does at runtime: swaps in the variant's body-part sprites.</summary>
        private static void ApplyVariantSkin(GameObject enemy)
        {
            var skin = enemy.GetComponentInChildren<RigSpriteSkin>(true);
            if (skin == null) return;
            var parts = new SerializedObject(skin).FindProperty("parts");
            var byName = new Dictionary<string, Sprite>();
            for (int i = 0; i < parts.arraySize; i++)
            {
                if (parts.GetArrayElementAtIndex(i).objectReferenceValue is Sprite part) byName[part.name] = part;
            }
            foreach (SpriteRenderer sr in enemy.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite != null && byName.TryGetValue(sr.sprite.name, out Sprite replacement)) sr.sprite = replacement;
            }
        }

        private static void SavePng(RenderTexture source, string path)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            var tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = PortraitSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ CLI

        public static void RunFromCommandLine()
        {
            bool ok = true;
            try
            {
                ok = Verify();
                CapturePortraits();
            }
            catch (Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
