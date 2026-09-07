using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MazeRunner.Customization;

namespace MazeRunner.UI
{
    /// <summary>
    /// Drives the character customization screen: category selection,
    /// previous/next option cycling, reset, apply, and back navigation.
    /// Reuses CharacterCustomizationManager for all logic.
    /// </summary>
    public class CustomizationUIController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private CharacterCustomizationManager customizationManager;

        [Header("Category Navigation")]
        [SerializeField] private Button categoryPrevButton;
        [SerializeField] private Button categoryNextButton;
        [SerializeField] private TMP_Text categoryLabel;

        [Header("Option Navigation")]
        [SerializeField] private Button optionPrevButton;
        [SerializeField] private Button optionNextButton;
        [SerializeField] private TMP_Text optionLabel;

        [Header("Actions")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;

        private CustomizationCategoryType[] categoryOrder;
        private int currentCategoryIndex;

        private void Awake()
        {
            categoryOrder = new[]
            {
                CustomizationCategoryType.Body,
                CustomizationCategoryType.Eyes,
                CustomizationCategoryType.Hair,
                CustomizationCategoryType.Shirt,
                CustomizationCategoryType.Pants,
                CustomizationCategoryType.Shoes,
                CustomizationCategoryType.Hat,
                CustomizationCategoryType.Accessory
            };

            if (categoryPrevButton != null) categoryPrevButton.onClick.AddListener(SelectPreviousCategory);
            if (categoryNextButton != null) categoryNextButton.onClick.AddListener(SelectNextCategory);
            if (optionPrevButton != null) optionPrevButton.onClick.AddListener(SelectPreviousOption);
            if (optionNextButton != null) optionNextButton.onClick.AddListener(SelectNextOption);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (backButton != null) backButton.onClick.AddListener(Back);
        }

        private void Start()
        {
            RefreshUI();
        }

        private CustomizationCategoryType CurrentCategoryType => categoryOrder[currentCategoryIndex];

        private void SelectNextCategory()
        {
            currentCategoryIndex = (currentCategoryIndex + 1) % categoryOrder.Length;
            RefreshUI();
        }

        private void SelectPreviousCategory()
        {
            currentCategoryIndex--;
            if (currentCategoryIndex < 0) currentCategoryIndex = categoryOrder.Length - 1;
            RefreshUI();
        }

        private void SelectNextOption()
        {
            if (customizationManager == null) return;
            customizationManager.CycleNext(CurrentCategoryType);
            RefreshOptionLabel();
        }

        private void SelectPreviousOption()
        {
            if (customizationManager == null) return;
            customizationManager.CyclePrevious(CurrentCategoryType);
            RefreshOptionLabel();
        }

        private void ResetToDefaults()
        {
            if (customizationManager == null) return;
            customizationManager.ResetToDefaults();
            RefreshOptionLabel();
        }

        private void Apply()
        {
            if (customizationManager == null) return;
            customizationManager.SaveCustomization();
        }

        private void Back()
        {
            gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            RefreshCategoryLabel();
            RefreshOptionLabel();
        }

        private void RefreshCategoryLabel()
        {
            if (categoryLabel != null)
            {
                categoryLabel.text = CurrentCategoryType.ToString();
            }
        }

        private void RefreshOptionLabel()
        {
            if (optionLabel == null || customizationManager == null) return;

            CustomizationOption option = customizationManager.GetCurrentOption(CurrentCategoryType);
            optionLabel.text = option != null ? option.DisplayName : "None";
        }
    }
}
