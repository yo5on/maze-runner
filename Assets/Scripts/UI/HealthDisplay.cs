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
        
        private Image[] heartImages;
        
        private void Start()
        {
            if (playerHealth != null)
            {
                InitializeHearts();
                playerHealth.OnHealthChanged += UpdateDisplay;
                UpdateDisplay(playerHealth.CurrentHealth);
            }
        }
        
        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateDisplay;
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
