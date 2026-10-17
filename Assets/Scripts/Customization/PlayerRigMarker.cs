using UnityEngine;

namespace MazeRunner.Customization
{
    /// <summary>Records which rig (see <see cref="PlayerRigSettings"/>) a Player's character rig was created from.</summary>
    [DisallowMultipleComponent]
    public class PlayerRigMarker : MonoBehaviour
    {
        public string RigId;
    }
}
