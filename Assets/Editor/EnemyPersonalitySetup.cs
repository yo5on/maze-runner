using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MazeRunner.Enemies;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Gives every enemy prefab exactly one personality (an <see cref="EnemyBehaviour"/>), removing any
    /// other behaviour so an enemy never has two brains. The table below is the whole mapping.
    ///
    /// Menu: Maze Runner > Set Up Enemy Personalities
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.EnemyPersonalitySetup.RunFromCommandLine
    /// </summary>
    public static class EnemyPersonalitySetup
    {
        private static readonly (string prefab, Type behaviour)[] Personalities =
        {
            ("Assets/Enemies/Zombie_Villager/Villager_Zombie_1.prefab", typeof(ZombieShamblerBehaviour)),
            ("Assets/Enemies/Zombie_Villager/Villager_Zombie_3.prefab", typeof(ZombieRusherBehaviour)),
            ("Assets/Enemies/Skeleton_Warrior/Skeleton_Warrior_2.prefab", typeof(ShieldWarriorBehaviour)),
            ("Assets/Enemies/Skeleton_Warrior/Skeleton_Warrior_3.prefab", typeof(DuelistBehaviour)),
            ("Assets/Enemies/Bloody_Alchemist/Bloody_Alchemist_1.prefab", typeof(AlchemistBehaviour)),
            ("Assets/Enemies/Skeleton_Crusader/Skeleton_Crusader_1.prefab", typeof(CrusaderBehaviour)),
            ("Assets/Enemies/Wraith/Wraith_3.prefab", typeof(WraithPhaseBehaviour)),
            ("Assets/Enemies/Ogre/Ogre_Enemy.prefab", typeof(OgreBehaviour)),
            ("Assets/Enemies/Goblin/Goblin_Enemy.prefab", typeof(GoblinSkirmisherBehaviour)),
            ("Assets/Enemies/Goblin/Goblin_Boss.prefab", typeof(BossHunterBehaviour)),
        };

        [MenuItem("Maze Runner/Set Up Enemy Personalities")]
        public static void Run()
        {
            foreach (var (path, type) in Personalities)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.GetComponent<GoblinEnemy>() == null) throw new Exception(path + " has no GoblinEnemy");
                    var removed = new List<string>();
                    foreach (EnemyBehaviour existing in root.GetComponents<EnemyBehaviour>())
                    {
                        if (existing.GetType() == type) continue;
                        removed.Add(existing.GetType().Name);
                        UnityEngine.Object.DestroyImmediate(existing);
                    }
                    bool added = root.GetComponent(type) == null;
                    if (added) root.AddComponent(type);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[EnemyPersonalitySetup] {path}: {type.Name}" + (added ? " added" : " kept") +
                              (removed.Count > 0 ? ", removed " + string.Join(", ", removed) : ""));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
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
    }
}
