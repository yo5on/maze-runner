using System;
using UnityEditor;
using UnityEngine;
using MazeRunner.Enemies;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Makes the three level bosses from the existing enemy art, so no new characters are needed:
    /// - Level 1, Ogre Warlord: the Ogre, 1.35x, ember-tinted, <see cref="OgreWarlordBehaviour"/>.
    /// - Level 2, Plague Alchemist: the Bloody Alchemist, 1.3x, plague-green, <see cref="AlchemistBossBehaviour"/>.
    /// - Level 3, Goblin Warchief: the existing Boss Goblin prefab (<see cref="BossHunterBehaviour"/>), retuned.
    /// The first two are rebuilt from their source prefabs each run (same paths and GUIDs, so scenes keep
    /// their references). <see cref="LevelLayoutBuilder"/> runs this before placing the bosses.
    ///
    /// Menu: Maze Runner > Set Up Level Bosses
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.BossEncounterSetup.RunFromCommandLine
    /// </summary>
    public static class BossEncounterSetup
    {
        public const string OgreWarlordPath = "Assets/Enemies/Ogre/Ogre_Warlord.prefab";
        public const string PlagueAlchemistPath = "Assets/Enemies/Bloody_Alchemist/Plague_Alchemist.prefab";
        public const string GoblinBossPath = "Assets/Enemies/Goblin/Goblin_Boss.prefab";

        [MenuItem("Maze Runner/Set Up Level Bosses")]
        public static void Run()
        {
            Make("Assets/Enemies/Ogre/Ogre_Enemy.prefab", OgreWarlordPath, typeof(OgreWarlordBehaviour),
                1.35f, new Color(1f, 0.8f, 0.72f),
                body =>
                {
                    Boss(body, "Ogre Warlord", 6);
                    body.FindProperty("attackRange").floatValue = 2.4f;
                },
                brain =>
                {
                    brain.FindProperty("walkSpeed").floatValue = 1.3f;
                    brain.FindProperty("noticeRange").floatValue = 12f;
                    brain.FindProperty("slamRange").floatValue = 3.2f;
                    brain.FindProperty("slamReach").floatValue = 3.4f;
                    brain.FindProperty("cooldown").floatValue = 3.2f;
                });

            Make("Assets/Enemies/Bloody_Alchemist/Bloody_Alchemist_1.prefab", PlagueAlchemistPath, typeof(AlchemistBossBehaviour),
                1.3f, new Color(0.8f, 0.93f, 0.7f),
                body => Boss(body, "Plague Alchemist", 7),
                brain =>
                {
                    brain.FindProperty("noticeRange").floatValue = 12f;
                    brain.FindProperty("tooClose").floatValue = 4f;
                    brain.FindProperty("tooFar").floatValue = 8f;
                    brain.FindProperty("retreatSpeed").floatValue = 2.6f;
                    brain.FindProperty("throwCooldown").floatValue = 3f;
                    brain.FindProperty("volley").intValue = 3;
                    brain.FindProperty("maxFlasksInAir").intValue = 8;
                    brain.FindProperty("maxPools").intValue = 4;
                });

            // The Level 3 boss keeps its prefab (art, rig, weapon); only its numbers change.
            GameObject root = PrefabUtility.LoadPrefabContents(GoblinBossPath);
            try
            {
                var body = new SerializedObject(root.GetComponent<GoblinEnemy>());
                Boss(body, "Goblin Warchief", 9);
                body.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GoblinBossPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[BossEncounterSetup] Ogre Warlord, Plague Alchemist and Goblin Warchief ready.");
        }

        public static void RunFromCommandLine()
        {
            bool ok = true;
            try { Run(); }
            catch (Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static void Boss(SerializedObject body, string title, int health)
        {
            body.FindProperty("isBoss").boolValue = true;
            body.FindProperty("bossTitle").stringValue = title;
            body.FindProperty("maxHealth").intValue = health;
            body.FindProperty("patrolDistance").floatValue = 0f;
        }

        private static void Make(string source, string path, Type behaviour, float scale, Color tint,
            Action<SerializedObject> tuneBody, Action<SerializedObject> tuneBrain)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
            if (prefab == null) throw new Exception("Missing prefab " + source);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                // Its own prefab (the rig inside stays a nested prefab of the original art).
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                go.name = System.IO.Path.GetFileNameWithoutExtension(path);
                go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(scale, scale, 1f));

                // Animations only key the alpha, so the tint (RGB) stays.
                foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.color = new Color(sr.color.r * tint.r, sr.color.g * tint.g, sr.color.b * tint.b, sr.color.a);
                }

                // The boss brain subclasses the source's brain: carry its tuned values (and potion art) over.
                string carried = null;
                foreach (EnemyBehaviour existing in go.GetComponents<EnemyBehaviour>())
                {
                    if (existing.GetType().IsAssignableFrom(behaviour)) carried = EditorJsonUtility.ToJson(existing);
                    UnityEngine.Object.DestroyImmediate(existing);
                }
                Component added = go.AddComponent(behaviour);
                if (carried != null) EditorJsonUtility.FromJsonOverwrite(carried, added);
                var brain = new SerializedObject(added);
                var body = new SerializedObject(go.GetComponent<GoblinEnemy>());
                tuneBody(body);
                tuneBrain(brain);
                body.ApplyModifiedPropertiesWithoutUndo();
                brain.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
