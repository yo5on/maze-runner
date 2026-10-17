using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MazeRunner.Customization;

namespace MazeRunner.UI
{
    /// <summary>
    /// Drives the character selection screen: the player picks one complete character
    /// (Previous / Next), sees a preview of it, and confirms with Apply. Reset returns the
    /// view to the default character and Back returns to the main menu. The choice is saved
    /// through PlayableCharacters and shown on the Player in every level.
    /// </summary>
    public class CustomizationUIController : MonoBehaviour
    {
        [Header("Category Navigation (hidden: characters are chosen whole)")]
        [SerializeField] private Button categoryPrevButton;
        [SerializeField] private Button categoryNextButton;
        [SerializeField] private TMP_Text categoryLabel;

        [Header("Character Navigation")]
        [SerializeField] private Button optionPrevButton;
        [SerializeField] private Button optionNextButton;
        [SerializeField] private TMP_Text optionLabel;

        [Header("Actions")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;

        [Header("Preview")]
        [SerializeField] private Vector2 previewSize = new Vector2(280f, 280f);
        [SerializeField] private Color selectedColor = new Color(0.55f, 0.9f, 0.55f, 1f);
        [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.75f);

        private int currentIndex;
        private bool isSelectionScreen;
        private Image previewImage;
        private TMP_Text statusLabel;

        private void Awake()
        {
            if (optionPrevButton != null) optionPrevButton.onClick.AddListener(SelectPrevious);
            if (optionNextButton != null) optionNextButton.onClick.AddListener(SelectNext);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefault);
            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (backButton != null) backButton.onClick.AddListener(Back);

            // Level scenes carry a leftover copy of this panel (inactive in Level 1, left active in Levels 2
            // and 3, where it covered the game). Characters are chosen on the Customization screen only.
            bool isCustomizationScene = gameObject.scene.name == "Customization";
            if (!isCustomizationScene && optionLabel != null && optionLabel.transform.parent != null
                && optionLabel.transform.parent.parent != null
                && optionLabel.transform.parent.parent.name == "CustomizationPanel")
            {
                optionLabel.transform.parent.parent.gameObject.SetActive(false);
            }

            isSelectionScreen = isCustomizationScene && optionLabel != null && optionLabel.gameObject.activeInHierarchy;
            if (!isSelectionScreen) return;

            HideCategoryRow();
            BuildPreview();

            // Character names are longer than the old option names: keep them on one line.
            optionLabel.textWrappingMode = TextWrappingModes.NoWrap;
            optionLabel.enableAutoSizing = true;
            optionLabel.fontSizeMax = optionLabel.fontSize;
            optionLabel.fontSizeMin = 18f;
        }

        private void Start()
        {
            if (!isSelectionScreen) return;
            currentIndex = Mathf.Max(0, PlayableCharacters.IndexOf(PlayableCharacters.Selected.Id));
            RefreshUI();
        }

        private PlayableCharacter Current => PlayableCharacters.All[currentIndex];

        private void SelectNext()
        {
            currentIndex = (currentIndex + 1) % PlayableCharacters.All.Count;
            RefreshUI();
        }

        private void SelectPrevious()
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = PlayableCharacters.All.Count - 1;
            RefreshUI();
        }

        private void ResetToDefault()
        {
            currentIndex = PlayableCharacters.IndexOf(PlayableCharacters.Default.Id);
            RefreshUI();
        }

        private void Apply()
        {
            PlayableCharacters.SaveSelection(Current);
            RefreshUI();
        }

        private void Back()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void RefreshUI()
        {
            PlayableCharacter character = Current;

            if (optionLabel != null) optionLabel.text = character.DisplayName;

            if (previewImage != null)
            {
                Sprite preview = PlayableCharacters.LoadPreview(character);
                previewImage.sprite = preview;
                previewImage.enabled = preview != null;
            }

            if (statusLabel != null)
            {
                bool isSelected = PlayableCharacters.Selected.Id == character.Id;
                string position = $"{currentIndex + 1} / {PlayableCharacters.All.Count}";
                statusLabel.text = isSelected ? $"SELECTED  ·  {position}" : $"Press Apply to select  ·  {position}";
                statusLabel.color = isSelected ? selectedColor : unselectedColor;
            }

            // If the scene shows a Player, preview the character on it too.
            var skin = FindAnyObjectByType<PlayerCharacterSkin>();
            if (skin != null) skin.Apply(character);
        }

        private void HideCategoryRow()
        {
            Transform row = categoryLabel != null ? categoryLabel.transform.parent
                : categoryPrevButton != null ? categoryPrevButton.transform.parent : null;
            if (row != null && optionLabel != null && row != optionLabel.transform.parent)
            {
                row.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Adds the preview image and the selected indicator above the character row,
        /// inside the panel's existing vertical layout, using the screen's own font.
        /// </summary>
        private void BuildPreview()
        {
            if (optionLabel == null) return;
            Transform optionRow = optionLabel.transform.parent;
            Transform panel = optionRow != null ? optionRow.parent : null;
            if (panel == null) return;

            int rowIndex = optionRow.GetSiblingIndex();

            var previewGo = new GameObject("CharacterPreview", typeof(RectTransform), typeof(Image));
            var previewRect = (RectTransform)previewGo.transform;
            previewRect.SetParent(panel, false);
            previewRect.sizeDelta = previewSize;
            previewRect.SetSiblingIndex(rowIndex);
            previewImage = previewGo.GetComponent<Image>();
            previewImage.preserveAspect = true;
            previewImage.raycastTarget = false;

            var statusGo = new GameObject("SelectedIndicator", typeof(RectTransform));
            var statusRect = (RectTransform)statusGo.transform;
            statusRect.SetParent(panel, false);
            statusRect.sizeDelta = new Vector2(560f, 40f);
            statusRect.SetSiblingIndex(rowIndex + 1);
            var status = statusGo.AddComponent<TextMeshProUGUI>();
            status.font = optionLabel.font;
            status.fontSize = 24f;
            status.alignment = TextAlignmentOptions.Center;
            status.raycastTarget = false;
            statusLabel = status;
        }
    }
}
