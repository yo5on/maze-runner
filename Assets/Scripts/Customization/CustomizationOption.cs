using UnityEngine;

namespace MazeRunner.Customization
{
    /// <summary>
    /// Represents a single customization option (e.g., "Spiky Hair", "Red Shirt").
    /// Each option contains a unique ID, display name, and sprite reference.
    /// </summary>
    [CreateAssetMenu(fileName = "New Customization Option", menuName = "Maze Runner/Customization/Option")]
    public class CustomizationOption : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this option (e.g., 'hair_01', 'shirt_blue')")]
        [SerializeField] private string optionId;
        
        [Tooltip("Display name shown to the player (e.g., 'Spiky Hair', 'Blue Shirt')")]
        [SerializeField] private string displayName;
        
        [Header("Visual")]
        [Tooltip("The sprite to display for this customization option")]
        [SerializeField] private Sprite sprite;
        
        [Header("Optional")]
        [Tooltip("Optional description of this customization option")]
        [TextArea(2, 4)]
        [SerializeField] private string description;
        
        // Public properties for read access
        public string OptionId => optionId;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public string Description => description;
        
        /// <summary>
        /// Validates that the option has required data.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(optionId) && 
                   !string.IsNullOrEmpty(displayName) && 
                   sprite != null;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure option ID doesn't have spaces
            if (!string.IsNullOrEmpty(optionId))
            {
                optionId = optionId.Replace(" ", "_").ToLower();
            }
        }
#endif
    }
}