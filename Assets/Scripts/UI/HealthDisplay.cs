using UnityEngine;
using UnityEngine.UI;
using MazeRunner.Player;

namespace MazeRunner.UI
{
    public class HealthDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private GameObject heartContainer;
        [SerializeField] private GameObject heartPrefab;
        [SerializeField] private Sprite fullHeartSprite;
        [SerializeField] private Sprite emptyHeartSprite;
        
        [Header("Spare lives")]
        [Tooltip("Heart from the same sheet (the blue one), drawn small in a row under the health hearts, one per spare life. A spent life shows the empty heart.")]
        [SerializeField] private Sprite spareLifeSprite;
        [SerializeField] private Vector2 spareLifeSize = new Vector2(26, 24);
        [SerializeField] private float spareLifeGap = 6f;
        
        private Image[] heartImages;
        private Image[] spareLifeImages;
        
        private void Start()
        {
            if (playerHealth != null)
            {
                InitializeHearts();
                InitializeSpareLives();
                playerHealth.OnHealthChanged += UpdateDisplay;
                playerHealth.OnSpareLivesChanged += UpdateSpareLives;
                UpdateDisplay(playerHealth.CurrentHealth);
                UpdateSpareLives(playerHealth.SpareLives);
            }
        }
        
        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateDisplay;
                playerHealth.OnSpareLivesChanged -= UpdateSpareLives;
            }
        }
        
        private void InitializeHearts()
        {
            if (heartContainer == null)
            {
                Debug.LogError("HealthDisplay: heartContainer not assigned!", this);
                return;
            }
            
            // Clear existing hearts
            foreach (Transform child in heartContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            heartImages = new Image[playerHealth.MaxHealth];
            
            for (int i = 0; i < playerHealth.MaxHealth; i++)
            {
                GameObject heart;
                if (heartPrefab != null)
                {
                    heart = Instantiate(heartPrefab, heartContainer.transform);
                }
                else
                {
                    heart = new GameObject($"Heart_{i}");
                    heart.transform.SetParent(heartContainer.transform, false);
                    Image img = heart.AddComponent<Image>();
                    img.sprite = fullHeartSprite;
                    img.preserveAspect = true;
                    
                    RectTransform rt = heart.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(40, 40);
                }
                
                heartImages[i] = heart.GetComponent<Image>();
                if (heartImages[i] == null)
                {
                    heartImages[i] = heart.AddComponent<Image>();
                }
            }
        }
        
        /// <summary>
        /// A small row under the health hearts, inside the heart container (so it moves with it) but outside
        /// its layout, with one slot for each spare life the player starts the level with.
        /// </summary>
        private void InitializeSpareLives()
        {
            if (heartContainer == null || spareLifeSprite == null) return;
            
            float heartHeight = 40f;
            if (heartImages != null && heartImages.Length > 0 && heartImages[0] != null)
            {
                heartHeight = heartImages[0].rectTransform.sizeDelta.y;
            }
            
            var row = new GameObject("SpareLives", typeof(RectTransform), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().ignoreLayout = true;
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(heartContainer.transform, false);
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -(heartHeight + spareLifeGap));
            rowRect.sizeDelta = new Vector2(0f, spareLifeSize.y);
            
            spareLifeImages = new Image[playerHealth.StartingSpareLives];
            for (int i = 0; i < spareLifeImages.Length; i++)
            {
                var life = new GameObject($"SpareLife_{i}", typeof(RectTransform), typeof(Image));
                var lifeRect = (RectTransform)life.transform;
                lifeRect.SetParent(rowRect, false);
                lifeRect.anchorMin = lifeRect.anchorMax = lifeRect.pivot = new Vector2(0f, 1f);
                lifeRect.anchoredPosition = new Vector2(i * (spareLifeSize.x + spareLifeGap), 0f);
                lifeRect.sizeDelta = spareLifeSize;
                
                spareLifeImages[i] = life.GetComponent<Image>();
                spareLifeImages[i].preserveAspect = true;
                spareLifeImages[i].raycastTarget = false;
            }
        }
        
        private void UpdateSpareLives(int spareLives)
        {
            if (spareLifeImages == null) return;
            
            for (int i = 0; i < spareLifeImages.Length; i++)
            {
                if (spareLifeImages[i] != null)
                {
                    spareLifeImages[i].sprite = i < spareLives ? spareLifeSprite : emptyHeartSprite;
                }
            }
        }
        
        private void UpdateDisplay(int currentHealth)
        {
            if (heartImages == null || heartImages.Length == 0) return;
            
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                {
                    heartImages[i].sprite = i < currentHealth ? fullHeartSprite : emptyHeartSprite;
                }
            }
        }
    }
}
