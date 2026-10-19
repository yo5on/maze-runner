using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace MazeRunner.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invincibilityDuration = 1f;
        [SerializeField] private float fallDeathY = -10f;
        [Tooltip("Longest time the death animation may take before the respawn (the clip normally ends sooner).")]
        [SerializeField] private float maxDeathAnimationTime = 1.5f;
        
        private int currentHealth;
        private float invincibilityTimer;
        private Vector3 lastCheckpointPosition;
        private PlayerAnimationController animationController;
        private bool hasUsedRespawn;
        private bool isGameOver;
        private bool isDying;
        private Rigidbody2D body;
        private PlayerController controller;
        private Coroutine deathRoutine;
        
        public event Action OnPlayerDeath;
        public event Action OnGameOver;
        public event Action<int> OnHealthChanged;
        /// <summary>Fires with the new count when the spare life is used up.</summary>
        public event Action<int> OnSpareLivesChanged;
        
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsInvincible => invincibilityTimer > 0;
        /// <summary>True from the moment the player dies until the respawn has finished.</summary>
        public bool IsDying => isDying;
        /// <summary>Spare lives the player starts each level attempt with: one respawn before Game Over.</summary>
        public int StartingSpareLives => 1;
        /// <summary>Spare lives left: the respawn is spent by the first death and comes back only when the level is loaded again.</summary>
        public int SpareLives => hasUsedRespawn ? 0 : StartingSpareLives;

        // Levels 2 and 3 get their PlayerAnimationController (with the character rig) only when the scene has
        // loaded, after this Awake, so it is looked up when needed rather than cached once.
        private PlayerAnimationController AnimationController
        {
            get
            {
                if (animationController == null) animationController = GetComponent<PlayerAnimationController>();
                return animationController;
            }
        }
        
        private void Awake()
        {
            currentHealth = maxHealth;
            lastCheckpointPosition = transform.position;
            animationController = GetComponent<PlayerAnimationController>();
            body = GetComponent<Rigidbody2D>();
            controller = GetComponent<PlayerController>();
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
            if (!isGameOver && !isDying && transform.position.y < fallDeathY)
            {
                Die();
            }
        }
        
        public void TakeDamage(int amount = 1)
        {
            if (IsInvincible || isDying || isGameOver) return;
            
            currentHealth = Mathf.Max(0, currentHealth - amount);
            invincibilityTimer = invincibilityDuration;
            OnHealthChanged?.Invoke(currentHealth);
            
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayDamage();
            }
            
            if (AnimationController != null)
            {
                AnimationController.TriggerHurt();
            }
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        public void Heal(int amount = 1)
        {
            if (isDying || isGameOver) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth);
        }
        
        public void Die()
        {
            // One death at a time: hazards, enemies and the fall check cannot start another one.
            if (isGameOver || isDying) return;

            OnPlayerDeath?.Invoke();
            
            if (!hasUsedRespawn)
            {
                hasUsedRespawn = true;
                OnSpareLivesChanged?.Invoke(SpareLives);
                deathRoutine = StartCoroutine(DieThenRespawn());
            }
            else
            {
                GameOver();
            }
        }

        /// <summary>
        /// The player stops (no input, no physics, no damage) while the death animation plays once,
        /// then the existing respawn runs.
        /// </summary>
        private IEnumerator DieThenRespawn()
        {
            isDying = true;
            SetAlive(false);

            float waited = 0f;
            // Let the Death trigger reach the Dying state, then wait for the clip to finish.
            yield return null;
            while (waited < maxDeathAnimationTime &&
                   !(AnimationController != null && AnimationController.IsDeathAnimationFinished))
            {
                waited += Time.deltaTime;
                yield return null;
            }

            deathRoutine = null;
            Respawn();
        }

        /// <summary>Freezes or restores the player's own movement and collisions.</summary>
        private void SetAlive(bool alive)
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = alive;
            }
            if (controller != null) controller.enabled = alive;
        }
        
        public void Respawn()
        {
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            // Move the physics body too and drop the fall speed: keeping it let the player
            // pass through the checkpoint's platform after respawning and die again at once.
            if (body != null)
            {
                body.position = lastCheckpointPosition;
            }
            transform.position = lastCheckpointPosition;
            SetAlive(true);
            isDying = false;
            currentHealth = maxHealth;
            invincibilityTimer = invincibilityDuration;
            OnHealthChanged?.Invoke(currentHealth);
            
            // Back to the living animation: clears the Death trigger / isDead and plays Idle.
            if (AnimationController != null)
            {
                AnimationController.ResetAnimationState();
            }

            // The camera jumps to the respawn point instead of sweeping across the level.
            var follow = FindAnyObjectByType<MazeRunner.Camera.CameraFollow>();
            if (follow != null) follow.SnapToTarget();
            
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
