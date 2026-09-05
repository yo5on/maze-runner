using System;
using System.Collections.Generic;
using UnityEngine;

namespace MazeRunner.SaveSystem
{
    /// <summary>
    /// Serializable data structure for saving and loading character customization.
    /// Stores the selected option ID for each customization category.
    /// Can be serialized to JSON or saved to PlayerPrefs.
    /// </summary>
    [Serializable]
    public class CustomizationData
    {
        [SerializeField] private string bodyOptionId;
        [SerializeField] private string hairOptionId;
        [SerializeField] private string eyesOptionId;
        [SerializeField] private string shirtOptionId;
        [SerializeField] private string pantsOptionId;
        [SerializeField] private string shoesOptionId;
        [SerializeField] private string hatOptionId;
        [SerializeField] private string accessoryOptionId;
        
        // Public properties for access
        public string BodyOptionId { get => bodyOptionId; set => bodyOptionId = value; }
        public string HairOptionId { get => hairOptionId; set => hairOptionId = value; }
        public string EyesOptionId { get => eyesOptionId; set => eyesOptionId = value; }
        public string ShirtOptionId { get => shirtOptionId; set => shirtOptionId = value; }
        public string PantsOptionId { get => pantsOptionId; set => pantsOptionId = value; }
        public string ShoesOptionId { get => shoesOptionId; set => shoesOptionId = value; }
        public string HatOptionId { get => hatOptionId; set => hatOptionId = value; }
        public string AccessoryOptionId { get => accessoryOptionId; set => accessoryOptionId = value; }
        
        /// <summary>
        /// Creates an empty CustomizationData instance.
        /// </summary>
        public CustomizationData()
        {
            // Initialize with empty strings
            bodyOptionId = string.Empty;
            hairOptionId = string.Empty;
            eyesOptionId = string.Empty;
            shirtOptionId = string.Empty;
            pantsOptionId = string.Empty;
            shoesOptionId = string.Empty;
            hatOptionId = string.Empty;
            accessoryOptionId = string.Empty;
        }
        
        /// <summary>
        /// Creates a CustomizationData instance from a dictionary.
        /// </summary>
        /// <param name="customizationDict">Dictionary from CharacterCustomizationManager.ExportCustomization()</param>
        public CustomizationData(Dictionary<Customization.CustomizationCategoryType, string> customizationDict)
        {
            if (customizationDict == null)
            {
                return;
            }
            
            // Import each category from the dictionary
            customizationDict.TryGetValue(Customization.CustomizationCategoryType.Body, out bodyOptionId);
            customizationDict.TryGetValue(Customization.CustomizationCategoryType.Hair, out hairOptionId);
            customizationDict.TryGetValue(Customization.CustomizationCategoryType.Eyes, out eyesOptionId);
            customizationDict.TryGetValue(Customization.CustomizationCategoryType.Shirt, out shirtOptionId);
            customizationDict.TryGetValue(Customization.CustomizationCategoryType.Pants, out pantsOptionId);
            customizationDict.TryGetValue(Customization.CustomizationCategoryType.Shoes, out shoesOptionId);
            customizationDict.TryGetValue(Customization.CustomizationCategoryType.Hat, out hatOptionId);
            customizationDict.TryGetValue(Customization.CustomizationCategoryType.Accessory, out accessoryOptionId);
        }
        
        /// <summary>
        /// Converts this CustomizationData to a dictionary for use with CharacterCustomizationManager.
        /// </summary>
        /// <returns>Dictionary mapping category type to option ID</returns>
        public Dictionary<Customization.CustomizationCategoryType, string> ToDictionary()
        {
            var dict = new Dictionary<Customization.CustomizationCategoryType, string>();
            
            if (!string.IsNullOrEmpty(bodyOptionId))
                dict[Customization.CustomizationCategoryType.Body] = bodyOptionId;
            
            if (!string.IsNullOrEmpty(hairOptionId))
                dict[Customization.CustomizationCategoryType.Hair] = hairOptionId;
            
            if (!string.IsNullOrEmpty(eyesOptionId))
                dict[Customization.CustomizationCategoryType.Eyes] = eyesOptionId;
            
            if (!string.IsNullOrEmpty(shirtOptionId))
                dict[Customization.CustomizationCategoryType.Shirt] = shirtOptionId;
            
            if (!string.IsNullOrEmpty(pantsOptionId))
                dict[Customization.CustomizationCategoryType.Pants] = pantsOptionId;
            
            if (!string.IsNullOrEmpty(shoesOptionId))
                dict[Customization.CustomizationCategoryType.Shoes] = shoesOptionId;
            
            if (!string.IsNullOrEmpty(hatOptionId))
                dict[Customization.CustomizationCategoryType.Hat] = hatOptionId;
            
            if (!string.IsNullOrEmpty(accessoryOptionId))
                dict[Customization.CustomizationCategoryType.Accessory] = accessoryOptionId;
            
            return dict;
        }
        
        /// <summary>
        /// Gets the option ID for a specific category.
        /// </summary>
        /// <param name="categoryType">The category to query</param>
        /// <returns>The option ID, or empty string if not set</returns>
        public string GetOptionId(Customization.CustomizationCategoryType categoryType)
        {
            return categoryType switch
            {
                Customization.CustomizationCategoryType.Body => bodyOptionId,
                Customization.CustomizationCategoryType.Hair => hairOptionId,
                Customization.CustomizationCategoryType.Eyes => eyesOptionId,
                Customization.CustomizationCategoryType.Shirt => shirtOptionId,
                Customization.CustomizationCategoryType.Pants => pantsOptionId,
                Customization.CustomizationCategoryType.Shoes => shoesOptionId,
                Customization.CustomizationCategoryType.Hat => hatOptionId,
                Customization.CustomizationCategoryType.Accessory => accessoryOptionId,
                _ => string.Empty
            };
        }
        
        /// <summary>
        /// Sets the option ID for a specific category.
        /// </summary>
        /// <param name="categoryType">The category to set</param>
        /// <param name="optionId">The option ID to set</param>
        public void SetOptionId(Customization.CustomizationCategoryType categoryType, string optionId)
        {
            switch (categoryType)
            {
                case Customization.CustomizationCategoryType.Body:
                    bodyOptionId = optionId;
                    break;
                case Customization.CustomizationCategoryType.Hair:
                    hairOptionId = optionId;
                    break;
                case Customization.CustomizationCategoryType.Eyes:
                    eyesOptionId = optionId;
                    break;
                case Customization.CustomizationCategoryType.Shirt:
                    shirtOptionId = optionId;
                    break;
                case Customization.CustomizationCategoryType.Pants:
                    pantsOptionId = optionId;
                    break;
                case Customization.CustomizationCategoryType.Shoes:
                    shoesOptionId = optionId;
                    break;
                case Customization.CustomizationCategoryType.Hat:
                    hatOptionId = optionId;
                    break;
                case Customization.CustomizationCategoryType.Accessory:
                    accessoryOptionId = optionId;
                    break;
            }
        }
        
        /// <summary>
        /// Serializes this data to JSON.
        /// </summary>
        /// <returns>JSON string representation</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
        
        /// <summary>
        /// Deserializes CustomizationData from JSON.
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>CustomizationData instance, or null if deserialization fails</returns>
        public static CustomizationData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            
            try
            {
                return JsonUtility.FromJson<CustomizationData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize CustomizationData: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Saves this customization data to PlayerPrefs.
        /// </summary>
        /// <param name="key">The PlayerPrefs key to use</param>
        public void SaveToPlayerPrefs(string key = "CharacterCustomization")
        {
            string json = ToJson();
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Loads customization data from PlayerPrefs.
        /// </summary>
        /// <param name="key">The PlayerPrefs key to use</param>
        /// <returns>CustomizationData instance, or null if not found</returns>
        public static CustomizationData LoadFromPlayerPrefs(string key = "CharacterCustomization")
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return null;
            }
            
            string json = PlayerPrefs.GetString(key);
            return FromJson(json);
        }
        
        /// <summary>
        /// Checks if any customization data is set.
        /// </summary>
        /// <returns>True if at least one option is set</returns>
        public bool HasData()
        {
            return !string.IsNullOrEmpty(bodyOptionId) ||
                   !string.IsNullOrEmpty(hairOptionId) ||
                   !string.IsNullOrEmpty(eyesOptionId) ||
                   !string.IsNullOrEmpty(shirtOptionId) ||
                   !string.IsNullOrEmpty(pantsOptionId) ||
                   !string.IsNullOrEmpty(shoesOptionId) ||
                   !string.IsNullOrEmpty(hatOptionId) ||
                   !string.IsNullOrEmpty(accessoryOptionId);
        }
        
        /// <summary>
        /// Clears all customization data.
        /// </summary>
        public void Clear()
        {
            bodyOptionId = string.Empty;
            hairOptionId = string.Empty;
            eyesOptionId = string.Empty;
            shirtOptionId = string.Empty;
            pantsOptionId = string.Empty;
            shoesOptionId = string.Empty;
            hatOptionId = string.Empty;
            accessoryOptionId = string.Empty;
        }
    }
}