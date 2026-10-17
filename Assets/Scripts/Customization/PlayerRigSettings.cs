using UnityEngine;

namespace MazeRunner.Customization
{
    /// <summary>
    /// The character rig the Player uses and how it sits on the Player, copied from Level 1's setup.
    /// Levels without a rig of their own get this one at load time (see PlayerCharacterSkin).
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerRig", menuName = "Maze Runner/Player Rig Settings")]
    public class PlayerRigSettings : ScriptableObject
    {
        public const string ResourcePath = "PlayerRig";

        /// <summary>Child name PlayerAnimationController looks for.</summary>
        public const string RigName = "Fallen_Angels";

        [SerializeField] private GameObject rigPrefab;
        [SerializeField] private Vector3 localPosition = new Vector3(0f, -0.5f, 0f);
        [SerializeField] private Vector3 localScale = new Vector3(0.35f, 0.35f, 0.35f);

        public GameObject RigPrefab => rigPrefab;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalScale => localScale;
    }
}
