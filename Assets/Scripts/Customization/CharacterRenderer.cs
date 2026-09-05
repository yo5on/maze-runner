using UnityEngine;
using System.Collections.Generic;

namespace MazeRunner.Customization
{
    /// <summary>
    /// Manages the visual rendering of a customizable character.
    /// Controls multiple sprite renderer layers (body, hair, clothes, etc.) and applies
    /// customization options to the appropriate layers.
    /// </summary>
    public class CharacterRenderer : MonoBehaviour
    {
        [Header("Layer Renderers")]
        [Tooltip("Sprite renderer for the body/skin layer")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        
        [Tooltip("Sprite renderer for the hair layer")]
        [SerializeField] private SpriteRenderer hairRenderer;
        
        [Tooltip("Sprite renderer for the eyes layer")]
        [SerializeField] private SpriteRenderer eyesRenderer;
        
        [Tooltip("Sprite renderer for the shirt layer")]
        [SerializeField] private SpriteRenderer shirtRenderer;
        
        [Tooltip("Sprite renderer for the pants layer")]
        [SerializeField] private SpriteRenderer pantsRenderer;
        
        [Tooltip("Sprite renderer for the shoes layer")]
        [SerializeField] private SpriteRenderer shoesRenderer;
        
        [Tooltip("Sprite renderer for the hat layer (optional)")]
        [SerializeField] private SpriteRenderer hatRenderer;
        
        [Tooltip("Sprite renderer for the accessory layer (optional)")]
        [SerializeField] private SpriteRenderer accessoryRenderer;
        
        [Header("Settings")]
        [Tooltip("Base sorting layer name for character sprites")]
        [SerializeField] private string sortingLayerName = "Default";
        
        [Tooltip("Base sorting order offset")]
        [SerializeField] private int baseSortingOrder = 0;
        
        // Dictionary for quick lookup of renderers by category type
        private Dictionary<CustomizationCategoryType, SpriteRenderer> rendererMap;
        
        private void Awake()
        {
            InitializeRendererMap();
        }
        
        /// <summary>
        /// Initializes the dictionary mapping category types to their sprite renderers.
        /// </summary>
        private void InitializeRendererMap()
        {
            rendererMap = new Dictionary<CustomizationCategoryType, SpriteRenderer>
            {
                { CustomizationCategoryType.Body, bodyRenderer },
                { CustomizationCategoryType.Hair, hairRenderer },
                { CustomizationCategoryType.Eyes, eyesRenderer },
                { CustomizationCategoryType.Shirt, shirtRenderer },
                { CustomizationCategoryType.Pants, pantsRenderer },
                { CustomizationCategoryType.Shoes, shoesRenderer },
                { CustomizationCategoryType.Hat, hatRenderer },
                { CustomizationCategoryType.Accessory, accessoryRenderer }
            };
        }
        
        /// <summary>
        /// Applies a customization option to the appropriate layer.
        /// </summary>
        /// <param name="categoryType">The category type to update</param>
        /// <param name="option">The customization option to apply</param>
        public void ApplyCustomization(CustomizationCategoryType categoryType, CustomizationOption option)
        {
            if (rendererMap == null)
            {
                InitializeRendererMap();
            }
            
            if (!rendererMap.TryGetValue(categoryType, out SpriteRenderer renderer))
            {
                Debug.LogWarning($"No renderer found for category type: {categoryType}");
                return;
            }
            
            if (renderer == null)
            {
                Debug.LogWarning($"Renderer for {categoryType} is not assigned in the Inspector");
                return;
            }
            
            // Apply the sprite (null is valid for optional layers)
            renderer.sprite = option != null ? option.Sprite : null;
            
            // Enable/disable renderer based on whether we have a sprite
            renderer.enabled = renderer.sprite != null;
        }
        
        /// <summary>
        /// Clears a specific customization layer.
        /// </summary>
        /// <param name="categoryType">The category type to clear</param>
        public void ClearLayer(CustomizationCategoryType categoryType)
        {
            ApplyCustomization(categoryType, null);
        }
        
        /// <summary>
        /// Sets the sorting order for a specific layer.
        /// </summary>
        /// <param name="categoryType">The category type</param>
        /// <param name="sortingOrder">The sorting order value</param>
        public void SetLayerSortingOrder(CustomizationCategoryType categoryType, int sortingOrder)
        {
            if (rendererMap == null)
            {
                InitializeRendererMap();
            }
            
            if (rendererMap.TryGetValue(categoryType, out SpriteRenderer renderer) && renderer != null)
            {
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = baseSortingOrder + sortingOrder;
            }
        }
        
        /// <summary>
        /// Gets the sprite renderer for a specific category.
        /// </summary>
        /// <param name="categoryType">The category type</param>
        /// <returns>The sprite renderer, or null if not found</returns>
        public SpriteRenderer GetRenderer(CustomizationCategoryType categoryType)
        {
            if (rendererMap == null)
            {
                InitializeRendererMap();
            }
            
            rendererMap.TryGetValue(categoryType, out SpriteRenderer renderer);
            return renderer;
        }
        
        /// <summary>
        /// Hides all customization layers.
        /// </summary>
        public void HideAllLayers()
        {
            foreach (var renderer in rendererMap.Values)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }
        
        /// <summary>
        /// Shows all customization layers that have sprites assigned.
        /// </summary>
        public void ShowAllLayers()
        {
            foreach (var renderer in rendererMap.Values)
            {
                if (renderer != null && renderer.sprite != null)
                {
                    renderer.enabled = true;
                }
            }
        }
        
        /// <summary>
        /// Validates that all required renderers are assigned.
        /// </summary>
        /// <returns>True if all required renderers are assigned</returns>
        public bool ValidateRenderers()
        {
            bool isValid = true;
            
            // Required layers
            if (bodyRenderer == null)
            {
                Debug.LogError("Body renderer is not assigned!", this);
                isValid = false;
            }
            if (hairRenderer == null)
            {
                Debug.LogError("Hair renderer is not assigned!", this);
                isValid = false;
            }
            if (eyesRenderer == null)
            {
                Debug.LogError("Eyes renderer is not assigned!", this);
                isValid = false;
            }
            if (shirtRenderer == null)
            {
                Debug.LogError("Shirt renderer is not assigned!", this);
                isValid = false;
            }
            if (pantsRenderer == null)
            {
                Debug.LogError("Pants renderer is not assigned!", this);
                isValid = false;
            }
            if (shoesRenderer == null)
            {
                Debug.LogError("Shoes renderer is not assigned!", this);
                isValid = false;
            }
            
            // Optional layers (hat and accessory) don't generate errors
            if (hatRenderer == null)
            {
                Debug.LogWarning("Hat renderer is not assigned (optional layer)", this);
            }
            if (accessoryRenderer == null)
            {
                Debug.LogWarning("Accessory renderer is not assigned (optional layer)", this);
            }
            
            return isValid;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Creates child GameObjects with SpriteRenderers for all layers.
        /// </summary>
        [ContextMenu("Setup Layer Renderers")]
        private void SetupLayerRenderers()
        {
            // This can be called from the Inspector context menu to auto-create layers
            CreateLayerRenderer("Body", ref bodyRenderer, 0);
            CreateLayerRenderer("Eyes", ref eyesRenderer, 1);
            CreateLayerRenderer("Hair", ref hairRenderer, 2);
            CreateLayerRenderer("Shirt", ref shirtRenderer, 3);
            CreateLayerRenderer("Pants", ref pantsRenderer, 4);
            CreateLayerRenderer("Shoes", ref shoesRenderer, 5);
            CreateLayerRenderer("Hat", ref hatRenderer, 6);
            CreateLayerRenderer("Accessory", ref accessoryRenderer, 7);
            
            Debug.Log("Character layer renderers created successfully!", this);
        }
        
        private void CreateLayerRenderer(string layerName, ref SpriteRenderer renderer, int sortingOrder)
        {
            // Check if already exists
            Transform existingLayer = transform.Find(layerName);
            if (existingLayer != null)
            {
                renderer = existingLayer.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    return; // Already set up
                }
            }
            
            // Create new layer
            GameObject layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(transform);
            layerObject.transform.localPosition = Vector3.zero;
            layerObject.transform.localRotation = Quaternion.identity;
            layerObject.transform.localScale = Vector3.one;
            
            renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = baseSortingOrder + sortingOrder;
        }
#endif
    }
}