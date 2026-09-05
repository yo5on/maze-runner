using UnityEngine;

namespace MazeRunner.Customization
{
    /// <summary>
    /// Defines a customization category type (e.g., Hair, Shirt, Pants).
    /// </summary>
    public enum CustomizationCategoryType
    {
        Body,
        Hair,
        Eyes,
        Shirt,
        Pants,
        Shoes,
        Hat,
        Accessory
    }
    
    /// <summary>
    /// Represents a category of customization options (e.g., all available hair styles).
    /// Contains all options available for a specific body part or clothing category.
    /// </summary>
    [CreateAssetMenu(fileName = "New Customization Category", menuName = "Maze Runner/Customization/Category")]
    public class CustomizationCategory : ScriptableObject
    {
        [Header("Category Definition")]
        [Tooltip("The type of customization category (Hair, Shirt, etc.)")]
        [SerializeField] private CustomizationCategoryType categoryType;
        
        [Tooltip("Display name for this category (e.g., 'Hairstyles', 'Shirts')")]
        [SerializeField] private string categoryName;
        
        [Header("Available Options")]
        [Tooltip("All customization options available in this category")]
        [SerializeField] private CustomizationOption[] options;
        
        [Header("Layer Settings")]
        [Tooltip("Sorting order for this layer (higher = rendered on top)")]
        [SerializeField] private int sortingOrder = 0;
        
        [Tooltip("Whether this category is optional (e.g., Hat can be empty)")]
        [SerializeField] private bool isOptional = false;
        
        // Public properties for read access
        public CustomizationCategoryType CategoryType => categoryType;
        public string CategoryName => categoryName;
        public CustomizationOption[] Options => options;
        public int SortingOrder => sortingOrder;
        public bool IsOptional => isOptional;
        
        /// <summary>
        /// Gets the number of available options in this category.
        /// </summary>
        public int OptionCount => options != null ? options.Length : 0;
        
        /// <summary>
        /// Gets a specific option by index.
        /// </summary>
        /// <param name="index">Index of the option</param>
        /// <returns>The customization option, or null if index is invalid</returns>
        public CustomizationOption GetOption(int index)
        {
            if (options == null || index < 0 || index >= options.Length)
            {
                return null;
            }
            
            return options[index];
        }
        
        /// <summary>
        /// Gets an option by its unique ID.
        /// </summary>
        /// <param name="optionId">The option ID to search for</param>
        /// <returns>The customization option, or null if not found</returns>
        public CustomizationOption GetOptionById(string optionId)
        {
            if (options == null || string.IsNullOrEmpty(optionId))
            {
                return null;
            }
            
            foreach (var option in options)
            {
                if (option != null && option.OptionId == optionId)
                {
                    return option;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets the index of an option by its ID.
        /// </summary>
        /// <param name="optionId">The option ID to search for</param>
        /// <returns>The index, or -1 if not found</returns>
        public int GetOptionIndex(string optionId)
        {
            if (options == null || string.IsNullOrEmpty(optionId))
            {
                return -1;
            }
            
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null && options[i].OptionId == optionId)
                {
                    return i;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Validates that the category has valid data.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                return false;
            }
            
            if (options == null || options.Length == 0)
            {
                return isOptional; // Optional categories can have no options
            }
            
            // Check that all options are valid
            foreach (var option in options)
            {
                if (option == null || !option.IsValid())
                {
                    return false;
                }
            }
            
            return true;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-set category name based on type if empty
            if (string.IsNullOrEmpty(categoryName))
            {
                categoryName = categoryType.ToString();
            }
        }
#endif
    }
}