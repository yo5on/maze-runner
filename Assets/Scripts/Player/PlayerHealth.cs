using UnityEngine;
using System;

namespace MazeRunner.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invincibilityDuration = 1f;
        [SerializeField] private float fallDeathY = -10f;
        
        private int currentHealth;
        private float invincibilityTimer;
        private Vector3 lastCheckpointPosition;
        
        public event Action OnPlayerDeath;
        public event Action<int> OnHealthChanged;
        
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsInvincible => invincibilityTimer > 0;
        
        private void Awake()
        {
            currentHealth = maxHealth;
            lastCheckpointPosition = transform.position;
        }
        
        private void Update()
        {
            if (invincibilityTimer > 0)
            {
                invincibilityTimer -= Time.deltaTime;
            }
            
            if (transform.position.y < fallDeathY)
            {
                Die();
            }
        }
        
        public void TakeDamage(int amount = 1)
        {
            if (IsInvincible) return;
            
            currentHealth = Mathf.Max(0, currentHealth - amount);
            invincibilityTimer = invincibilityDuration;
            OnHealthChanged?.Invoke(currentHealth);
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        public void Heal(int amount = 1)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth);
        }
        
        public void Die()
        {
            OnPlayerDeath?.Invoke();
            Respawn();
        }
        
        public void Respawn()
        {
            transform.position = lastCheckpointPosition;
            currentHealth = maxHealth;
            invincibilityTimer = invincibilityDuration;
            OnHealthChanged?.Invoke(currentHealth);
        }
        
        public void SetCheckpoint(Vector3 position)
        {
            lastCheckpointPosition = position;
        }
    }
}
