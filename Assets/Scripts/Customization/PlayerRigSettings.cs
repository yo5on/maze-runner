using System;
using UnityEngine;

namespace MazeRunner.Customization
{
    /// <summary>
    /// The character rigs the Player can use and how a rig sits on the Player, copied from Level 1's setup.
    /// The default rig is the Fallen Angels one the levels were built with; other playable characters bring
    /// their own rig (generated from the pack's Spriter file) and a copy of the player Animator Controller
    /// that points at that rig's clips. PlayerCharacterSkin puts the right rig on the Player at load time.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerRig", menuName = "Maze Runner/Player Rig Settings")]
    public class PlayerRigSettings : ScriptableObject
    {
        public const string ResourcePath = "PlayerRig";

        /// <summary>Child name PlayerAnimationController looks for.</summary>
        public const string RigName = "Fallen_Angels";

        /// <summary>Id of the rig Level 1's Player is saved with.</summary>
        public const string DefaultRigId = "Fallen_Angels";

        [Serializable]
        public class RigEntry
        {
            public string id;
            public GameObject prefab;
            [Tooltip("Player controller for this rig (same states and parameters as the default one).")]
            public RuntimeAnimatorController controller;
        }

        [SerializeField] private GameObject rigPrefab;
        [SerializeField] private Vector3 localPosition = new Vector3(0f, -0.5f, 0f);
        [SerializeField] private Vector3 localScale = new Vector3(0.35f, 0.35f, 0.35f);
        [SerializeField] private RigEntry[] extraRigs = new RigEntry[0];

        public GameObject RigPrefab => rigPrefab;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalScale => localScale;

        /// <summary>Finds a rig by id. The default rig keeps the controller on its prefab (controller is null).</summary>
        public bool TryGetRig(string id, out GameObject prefab, out RuntimeAnimatorController controller)
        {
            prefab = null;
            controller = null;
            if (string.IsNullOrEmpty(id) || id == DefaultRigId)
            {
                prefab = rigPrefab;
                return prefab != null;
            }

            foreach (RigEntry entry in extraRigs)
            {
                if (entry != null && entry.id == id && entry.prefab != null)
                {
                    prefab = entry.prefab;
                    controller = entry.controller;
                    return true;
                }
            }
            return false;
        }
    }
}
