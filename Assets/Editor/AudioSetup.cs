using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MazeRunner.Audio;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Makes the game's audio one persistent object: Level 1's AudioManager (the only one with its clips
    /// assigned) becomes Assets/Resources/AudioManager.prefab, which AudioManager spawns before the first
    /// scene, and the per-level copies are removed. Before this, the menus had no AudioManager at all and
    /// Level 2 / Level 3 carried an empty one that, once it became the persistent instance, kept the rest
    /// of the session silent.
    ///
    /// Menu: Maze Runner > Make Persistent Audio Manager
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.AudioSetup.RunFromCommandLine
    /// </summary>
    public static class AudioSetup
    {
        private const string PrefabPath = "Assets/Resources/" + AudioManager.ResourcePath + ".prefab";
        private const string SourceScene = "Assets/Scenes/Levels/Level1.unity";
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Levels/Level1.unity",
            "Assets/Scenes/Levels/Level2.unity",
            "Assets/Scenes/Levels/Level3.unity",
        };

        [MenuItem("Maze Runner/Make Persistent Audio Manager")]
        public static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<AudioManager>(PrefabPath) == null)
            {
                Scene source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
                AudioManager manager = FindRootManager(source);
                if (manager == null) throw new Exception("No AudioManager in " + SourceScene);
                PrefabUtility.SaveAsPrefabAsset(manager.gameObject, PrefabPath);
                Debug.Log("[AudioSetup] Saved " + PrefabPath);
            }

            foreach (string path in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int removed = 0;
                for (AudioManager m = FindRootManager(scene); m != null; m = FindRootManager(scene))
                {
                    UnityEngine.Object.DestroyImmediate(m.gameObject);
                    removed++;
                }
                if (removed > 0) EditorSceneManager.SaveScene(scene);
                Debug.Log("[AudioSetup] " + path + ": removed " + removed + " scene AudioManager(s)");
            }
            AssetDatabase.SaveAssets();
        }

        public static void RunFromCommandLine()
        {
            bool ok = true;
            try { Run(); }
            catch (Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static AudioManager FindRootManager(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var m = root.GetComponent<AudioManager>();
                if (m != null) return m;
            }
            return null;
        }
    }
}
