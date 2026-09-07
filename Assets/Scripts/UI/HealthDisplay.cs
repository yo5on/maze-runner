using UnityEngine;
using TMPro;
using MazeRunner.Player;

namespace MazeRunner.UI
{
    public class HealthDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private TMP_Text healthText;
        
        private void Start()
        {
            if (playerHealth != null)
            {
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
        
        private void UpdateDisplay(int currentHealth)
        {
            if (healthText != null && playerHealth != null)
            {
                healthText.text = $"Health: {currentHealth} / {playerHealth.MaxHealth}";
            }
        }
    }
}