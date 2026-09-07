using UnityEngine;
using System.Collections.Generic;
using MazeRunner.SaveSystem;

namespace MazeRunner.Customization
{
    /// <summary>
    /// Manages character customization, tracking current selections and applying them
    /// to the CharacterRenderer. Acts as the central hub for all customization operations.
    /// </summary>
    [RequireComponent(typeof(CharacterRenderer))]
    public class CharacterCustomizationManager : MonoBehaviour
    {
        [Header("Customization Categories")]
        [Tooltip("All available customization categories (Hair, Shirts, Pants, etc.)")]
        [SerializeField] private CustomizationCategory[] categories;
        
        [Header("Components")]
        [Tooltip("The character renderer that displays the customization")]
        [SerializeField] private CharacterRenderer characterRenderer;
        
        [Header("Default Selections")]
        [Tooltip("Use default selections on start (first option in each category)")]
        [SerializeField] private bool useDefaultsOnStart = true;
        
        [Header("Save/Load")]
        [Tooltip("Auto-load saved customization on start")]
        [SerializeField] private bool autoLoadOnStart = true;
        
        [Tooltip("PlayerPrefs key for saving customization")]
        [SerializeField] private string saveKey = "CharacterCustomization";
        
        // Current selections: maps category type to selected option index
        private Dictionary<CustomizationCategoryType, int> currentSelections;
        
        // Category lookup: maps category type to its CustomizationCategory asset
        private Dictionary<CustomizationCategoryType, CustomizationCategory> categoryMap;
        
        private void Awake()
        {
            // Get CharacterRenderer component if not assigned
            if (characterRenderer == null)
            {
                characterRenderer = GetComponent<CharacterRenderer>();
            }
            
            InitializeMaps();
        }
        
        private void Start()
        {
            if (autoLoadOnStart && TryLoadCustomization())
            {
                return; // Loaded successfully
            }
            
            if (useDefaultsOnStart)
            {
                ApplyDefaults();
            }
        }
        
        /// <summary>
        /// Initializes internal data structures.
        /// </summary>
        private void InitializeMaps()
        {
            currentSelections = new Dictionary<CustomizationCategoryType, int>();
            categoryMap = new Dictionary<CustomizationCategoryType, CustomizationCategory>();
            
            if (categories == null || categories.Length == 0)
            {
                Debug.LogWarning("No customization categories assigned to CharacterCustomizationManager!", this);
                return;
            }
            
            // Build category lookup map
            foreach (var category in categories)
            {
                if (category != null)
                {
                    categoryMap[category.CategoryType] = category;
                    
                    // Apply sorting order to renderer
                    if (characterRenderer != null)
                    {
                        characterRenderer.SetLayerSortingOrder(category.CategoryType, category.SortingOrder);
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies default customization (first option in each category).
        /// </summary>
        public void ApplyDefaults()
        {
            foreach (var category in categoryMap.Values)
            {
                if (category.OptionCount > 0)
                {
                    SetCustomization(category.CategoryType, 0);
                }
            }
        }
        
        /// <summary>
        /// Sets the customization for a specific category by option index.
        /// </summary>
        /// <param name="categoryType">The category to customize</param>
        /// <param name="optionIndex">Index of the option to apply</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetCustomization(CustomizationCategoryType categoryType, int optionIndex)
        {
            if (!categoryMap.TryGetValue(categoryType, out CustomizationCategory category))
            {
                Debug.LogWarning($"Category {categoryType} not found in customization manager.");
                return false;
            }
            
            CustomizationOption option = category.GetOption(optionIndex);
            if (option == null)
            {
                // For optional categories, null is valid (means "none")
                if (!category.IsOptional)
                {
                    Debug.LogWarning($"Invalid option index {optionIndex} for category {categoryType}.");
                    return false;
                }
            }
            
            // Update selection
            currentSelections[categoryType] = optionIndex;
            
            // Apply to renderer
            if (characterRenderer != null)
            {
                characterRenderer.ApplyCustomization(categoryType, option);
            }
            
            return true;
        }
        
        /// <summary>
        /// Sets the customization for a specific category by option ID.
        /// </summary>
        /// <param name="categoryType">The category to customize</param>
        /// <param name="optionId">ID of the option to apply</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetCustomizationById(CustomizationCategoryType categoryType, string optionId)
        {
            if (!categoryMap.TryGetValue(categoryType, out CustomizationCategory category))
            {
                Debug.LogWarning($"Category {categoryType} not found in customization manager.");
                return false;
            }
            
            int optionIndex = category.GetOptionIndex(optionId);
            if (optionIndex < 0)
            {
                Debug.LogWarning($"Option ID '{optionId}' not found in category {categoryType}.");
                return false;
            }
            
            return SetCustomization(categoryType, optionIndex);
        }
        
        /// <summary>
        /// Gets the currently selected option index for a category.
        /// </summary>
        /// <param name="categoryType">The category to query</param>
        /// <returns>The selected option index, or -1 if not set</returns>
        public int GetCurrentSelection(CustomizationCategoryType categoryType)
        {
            return currentSelections.TryGetValue(categoryType, out int index) ? index : -1;
        }
        
        /// <summary>
        /// Gets the currently selected option for a category.
        /// </summary>
        /// <param name="categoryType">The category to query</param>
        /// <returns>The selected CustomizationOption, or null if not set</returns>
        public CustomizationOption GetCurrentOption(CustomizationCategoryType categoryType)
        {
            if (!categoryMap.TryGetValue(categoryType, out CustomizationCategory category))
            {
                return null;
            }
            
            int index = GetCurrentSelection(categoryType);
            if (index < 0)
            {
                return null;
            }
            
            return category.GetOption(index);
        }
        
        /// <summary>
        /// Cycles to the next option in a category.
        /// </summary>
        /// <param name="categoryType">The category to cycle</param>
        /// <param name="wrap">Whether to wrap around to the first option</param>
        /// <returns>True if successful</returns>
        public bool CycleNext(CustomizationCategoryType categoryType, bool wrap = true)
        {
            if (!categoryMap.TryGetValue(categoryType, out CustomizationCategory category))
            {
                return false;
            }
            
            int currentIndex = GetCurrentSelection(categoryType);
            int nextIndex = currentIndex + 1;
            
            if (nextIndex >= category.OptionCount)
            {
                if (wrap)
                {
                    nextIndex = 0;
                }
                else
                {
                    return false; // Already at last option
                }
            }
            
            return SetCustomization(categoryType, nextIndex);
        }
        
        /// <summary>
        /// Cycles to the previous option in a category.
        /// </summary>
        /// <param name="categoryType">The category to cycle</param>
        /// <param name="wrap">Whether to wrap around to the last option</param>
        /// <returns>True if successful</returns>
        public bool CyclePrevious(CustomizationCategoryType categoryType, bool wrap = true)
        {
            if (!categoryMap.TryGetValue(categoryType, out CustomizationCategory category))
            {
                return false;
            }
            
            int currentIndex = GetCurrentSelection(categoryType);
            int previousIndex = currentIndex - 1;
            
            if (previousIndex < 0)
            {
                if (wrap)
                {
                    previousIndex = category.OptionCount - 1;
                }
                else
                {
                    return false; // Already at first option
                }
            }
            
            return SetCustomization(categoryType, previousIndex);
        }
        
        /// <summary>
        /// Clears customization for a specific category (only for optional categories).
        /// </summary>
        /// <param name="categoryType">The category to clear</param>
        /// <returns>True if successful</returns>
        public bool ClearCustomization(CustomizationCategoryType categoryType)
        {
            if (!categoryMap.TryGetValue(categoryType, out CustomizationCategory category))
            {
                return false;
            }
            
            if (!category.IsOptional)
            {
                Debug.LogWarning($"Cannot clear non-optional category {categoryType}.");
                return false;
            }
            
            currentSelections[categoryType] = -1;
            
            if (characterRenderer != null)
            {
                characterRenderer.ClearLayer(categoryType);
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets the CustomizationCategory for a specific type.
        /// </summary>
        /// <param name="categoryType">The category type</param>
        /// <returns>The CustomizationCategory, or null if not found</returns>
        public CustomizationCategory GetCategory(CustomizationCategoryType categoryType)
        {
            categoryMap.TryGetValue(categoryType, out CustomizationCategory category);
            return category;
        }
        
        /// <summary>
        /// Gets all available categories.
        /// </summary>
        /// <returns>Array of all CustomizationCategory assets</returns>
        public CustomizationCategory[] GetAllCategories()
        {
            return categories;
        }
        
        /// <summary>
        /// Randomizes all customization options.
        /// </summary>
        public void Randomize()
        {
            foreach (var category in categoryMap.Values)
            {
                if (category.OptionCount > 0)
                {
                    int randomIndex = Random.Range(0, category.OptionCount);
                    SetCustomization(category.CategoryType, randomIndex);
                }
            }
        }
        
        /// <summary>
        /// Exports current customization as a dictionary of option IDs.
        /// </summary>
        /// <returns>Dictionary mapping category type to option ID</returns>
        public Dictionary<CustomizationCategoryType, string> ExportCustomization()
        {
            var exported = new Dictionary<CustomizationCategoryType, string>();
            
            foreach (var kvp in currentSelections)
            {
                CustomizationOption option = GetCurrentOption(kvp.Key);
                if (option != null)
                {
                    exported[kvp.Key] = option.OptionId;
                }
            }
            
            return exported;
        }
        
        /// <summary>
        /// Imports customization from a dictionary of option IDs.
        /// </summary>
        /// <param name="customizationData">Dictionary mapping category type to option ID</param>
        public void ImportCustomization(Dictionary<CustomizationCategoryType, string> customizationData)
        {
            if (customizationData == null)
            {
                return;
            }
            
            foreach (var kvp in customizationData)
            {
                SetCustomizationById(kvp.Key, kvp.Value);
            }
        }
        
        /// <summary>
        /// Validates the setup of this customization manager.
        /// </summary>
        /// <returns>True if properly configured</returns>
        public bool ValidateSetup()
        {
            bool isValid = true;
            
            if (characterRenderer == null)
            {
                Debug.LogError("CharacterRenderer is not assigned!", this);
                isValid = false;
            }
            else
            {
                isValid = characterRenderer.ValidateRenderers();
            }
            
            if (categories == null || categories.Length == 0)
            {
                Debug.LogError("No customization categories assigned!", this);
                isValid = false;
            }
            else
            {
                foreach (var category in categories)
                {
                    if (category == null)
                    {
                        Debug.LogError("Null category found in categories array!", this);
                        isValid = false;
                    }
                    else if (!category.IsValid())
                    {
                        Debug.LogError($"Category {category.CategoryName} is not valid!", category);
                        isValid = false;
                    }
                }
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Saves the current customization selections to PlayerPrefs.
        /// </summary>
        public void SaveCustomization()
        {
            var exported = ExportCustomization();
            var data = new CustomizationData(exported);
            data.SaveToPlayerPrefs(saveKey);
        }
        
        /// <summary>
        /// Attempts to load and apply saved customization from PlayerPrefs.
        /// Safely falls back if data is missing, invalid, or corrupted.
        /// </summary>
        /// <returns>True if valid saved data was found and applied</returns>
        public bool TryLoadCustomization()
        {
            CustomizationData data = CustomizationData.LoadFromPlayerPrefs(saveKey);
            
            if (data == null || !data.HasData())
            {
                return false;
            }
            
            Dictionary<CustomizationCategoryType, string> dict = data.ToDictionary();
            bool anyApplied = false;
            
            foreach (var kvp in dict)
            {
                // Skip categories that no longer exist (corrupted/outdated save safety)
                if (!categoryMap.ContainsKey(kvp.Key))
                {
                    continue;
                }
                
                if (SetCustomizationById(kvp.Key, kvp.Value))
                {
                    anyApplied = true;
                }
            }
            
            // Fill any categories missing from save data with defaults
            foreach (var category in categoryMap.Values)
            {
                if (GetCurrentSelection(category.CategoryType) < 0 && category.OptionCount > 0)
                {
                    SetCustomization(category.CategoryType, 0);
                }
            }
            
            return anyApplied;
        }
        
        /// <summary>
        /// Resets customization to defaults and clears saved data.
        /// </summary>
        public void ResetToDefaults()
        {
            ApplyDefaults();
            PlayerPrefs.DeleteKey(saveKey);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Validate Setup")]
        private void ValidateSetupMenu()
        {
            if (ValidateSetup())
            {
                Debug.Log("CharacterCustomizationManager setup is valid!", this);
            }
        }
#endif
    }
}