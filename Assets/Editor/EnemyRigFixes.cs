using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Corrections for the imported enemy rigs (RosterImport runs this after rebuilding them).
    ///
    /// 1. Death plays once: the Spriter importer marks every clip as looping, including Dying.
    ///    Only the clips used by the "Dying" states of the enemy Animator Controllers are switched
    ///    to non-looping; every other clip keeps its settings.
    /// 2. Wraith size: the Wraith rig is drawn at about half the height of the other enemies. Its enemy
    ///    prefab gets a uniform root scale that brings it in line with the Villager Zombie and Skeleton
    ///    Warrior (proportions, sprites and import settings are untouched; collider and attack point keep
    ///    their world size).
    ///
    /// Menu: Maze Runner > Fix Enemy Rigs
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.EnemyRigFixes.RunFromCommandLine
    /// </summary>
    public static class EnemyRigFixes
    {
        private const string EnemyFolder = "Assets/Enemies";
        private const string WraithPrefab = "Assets/Enemies/Wraith/Wraith_3.prefab";
        // Measured in play (AITestProbe.EnemySizes) at the shared 0.4 enemy scale: the Wraith's body is
        // 1.24-1.36 units tall in Idle, Walking and Slashing, the Villager Zombies and Skeleton Warriors
        // 2.57-2.65. Its rig is drawn at half their size, so it gets twice their scale.
        private const float WraithScale = 0.8f;
        [MenuItem("Maze Runner/Fix Enemy Rigs")]
        public static void Run()
        {
            MakeDeathClipsPlayOnce();
            MatchWraithSize();
            AssetDatabase.SaveAssets();
        }

        public static void RunFromCommandLine()
        {
            bool ok = true;
            try { Run(); }
            catch (System.Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static void MakeDeathClipsPlayOnce()
        {
            var clips = new HashSet<AnimationClip>();
            foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController", new[] { EnemyFolder }))
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (AnimatorControllerLayer layer in controller.layers)
                {
                    foreach (ChildAnimatorState child in layer.stateMachine.states)
                    {
                        if (child.state.name == "Dying" && child.state.motion is AnimationClip clip) clips.Add(clip);
                    }
                }
            }

            foreach (AnimationClip clip in clips)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                if (!settings.loopTime) continue;
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
                Debug.Log($"[EnemyRigFixes] {AssetDatabase.GetAssetPath(clip)}: '{clip.name}' ({clip.length:F2}s) no longer loops");
            }
        }

        /// <summary>
        /// Scales the Wraith to <see cref="WraithScale"/> while its collider and attack point keep their
        /// world size (the same 1 x 2 body every other enemy has), so only the drawing changes.
        /// Running it again does nothing.
        /// </summary>
        public static void MatchWraithSize()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(WraithPrefab);
            try
            {
                float current = root.transform.localScale.y;
                float factor = WraithScale / current;
                if (Mathf.Abs(factor - 1f) < 0.001f) return;

                root.transform.localScale = new Vector3(WraithScale, WraithScale, WraithScale);
                var capsule = root.GetComponent<CapsuleCollider2D>();
                if (capsule != null)
                {
                    capsule.size /= factor;
                    capsule.offset /= factor;
                }
                var goblin = root.GetComponent<MazeRunner.Enemies.GoblinEnemy>();
                var attackPoint = goblin != null ? (Transform)new SerializedObject(goblin).FindProperty("attackPoint").objectReferenceValue : null;
                if (attackPoint != null) attackPoint.localPosition /= factor;

                PrefabUtility.SaveAsPrefabAsset(root, WraithPrefab);
                Debug.Log($"[EnemyRigFixes] Wraith_3 scale {current:F2} -> {WraithScale:F2}; collider {(capsule != null ? capsule.size.ToString() : "-")}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
