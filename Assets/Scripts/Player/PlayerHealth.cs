using UnityEngine;
using UnityEngine.SceneManagement;
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
        private PlayerAnimationController animationController;
        private bool hasUsedRespawn;
        private bool isGameOver;
        private Rigidbody2D body;
        
        public event Action OnPlayerDeath;
        public event Action OnGameOver;
        public event Action<int> OnHealthChanged;
        
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsInvincible => invincibilityTimer > 0;
        
        private void Awake()
        {
            currentHealth = maxHealth;
            lastCheckpointPosition = transform.position;
            animationController = GetComponent<PlayerAnimationController>();
            body = GetComponent<Rigidbody2D>();
            hasUsedRespawn = false;
        }
        
        private void Update()
        {
            if (invincibilityTimer > 0)
            {
                invincibilityTimer -= Time.deltaTime;
            }
            
            // After game over the player stays below the kill height (time is stopped),
            // so stop checking; otherwise Die/OnGameOver would fire every frame.
            if (!isGameOver && transform.position.y < fallDeathY)
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
            
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayDamage();
            }
            
            if (animationController != null)
            {
                animationController.TriggerHurt();
            }
            
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
            if (isGameOver) return;

            OnPlayerDeath?.Invoke();
            
            if (!hasUsedRespawn)
            {
                hasUsedRespawn = true;
                Respawn();
            }
            else
            {
                GameOver();
            }
        }
        
        public void Respawn()
        {
            // Move the physics body too and drop the fall speed: keeping it let the player
            // pass through the checkpoint's platform after respawning and die again at once.
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = lastCheckpointPosition;
            }
            transform.position = lastCheckpointPosition;
            currentHealth = maxHealth;
            invincibilityTimer = invincibilityDuration;
            OnHealthChanged?.Invoke(currentHealth);
            
            if (animationController != null)
            {
                animationController.ResetAnimationState();
            }
            
            // Play respawn sound
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayRespawn();
            }
        }
        
        private void GameOver()
        {
            isGameOver = true;
            OnGameOver?.Invoke();
            Time.timeScale = 0f;
            
            if (GetComponent<PlayerController>() != null)
            {
                GetComponent<PlayerController>().enabled = false;
            }
        }
        
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        public void SetCheckpoint(Vector3 position)
        {
            lastCheckpointPosition = position;
        }
    }
}
