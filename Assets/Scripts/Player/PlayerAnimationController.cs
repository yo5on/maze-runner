using UnityEngine;

namespace MazeRunner.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Animator Reference")]
        [SerializeField] private Animator fallenAngelsAnimator;
        
        [Header("Settings")]
        [SerializeField] private bool enableAnimations = true;
        
        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private SpriteRenderer spriteRenderer;
        
        private bool wasGrounded;
        private bool isHurt;
        private bool isDead;
        
        private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
        private static readonly int IsFallingHash = Animator.StringToHash("isFalling");
        private static readonly int IsHurtHash = Animator.StringToHash("isHurt");
        private static readonly int IsDeadHash = Animator.StringToHash("isDead");
        private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
        private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerHealth = GetComponent<PlayerHealth>();
            
            if (fallenAngelsAnimator == null)
            {
                Transform child = transform.Find("Fallen_Angels");
                if (child != null)
                {
                    fallenAngelsAnimator = child.GetComponent<Animator>();
                }
            }
            
            if (fallenAngelsAnimator != null)
            {
                spriteRenderer = fallenAngelsAnimator.GetComponent<SpriteRenderer>();
            }
            
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDeath += OnPlayerDeath;
            }
            
            wasGrounded = true;
        }
        
        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDeath -= OnPlayerDeath;
            }
        }
        
        private void Update()
        {
            if (!enableAnimations || fallenAngelsAnimator == null) return;
            
            UpdateAnimationState();
            UpdateFlip();
        }
        
        private void UpdateAnimationState()
        {
            if (isDead)
            {
                return;
            }
            
            bool isGrounded = playerController.IsGrounded;
            float velocityY = playerController.Velocity.y;
            float horizontalSpeed = Mathf.Abs(playerController.Velocity.x);
            bool isMoving = horizontalSpeed > 0.1f;
            
            // Debug logging
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AnimController] Grounded={isGrounded}, VelX={playerController.Velocity.x:F2}, VelY={velocityY:F2}, Moving={isMoving}, Animator={(fallenAngelsAnimator != null ? "Found" : "NULL")}, Controller={(fallenAngelsAnimator != null && fallenAngelsAnimator.runtimeAnimatorController != null ? "Assigned" : "MISSING")}");
            }
            
            // Always update isRunning based on grounded movement
            bool shouldRun = isGrounded && isMoving;
            SetBool(IsRunningHash, shouldRun);
            
            if (Time.frameCount % 60 == 0 && isMoving)
            {
                Debug.Log($"[AnimController] Setting isRunning={shouldRun}, HasParam={HasParameter(IsRunningHash)}");
            }
            
            if (!wasGrounded && isGrounded)
            {
                SetBool(IsJumpingHash, false);
                SetBool(IsFallingHash, false);
            }
            
            // Trigger Jump animation when leaving ground
            if (wasGrounded && !isGrounded && velocityY > 0.1f)
            {
                SetTrigger(JumpTriggerHash);
                Debug.Log("[AnimController] Jump trigger fired");
            }
            
            wasGrounded = isGrounded;
            
            if (!isGrounded)
            {
                if (velocityY > 0.1f)
                {
                    SetBool(IsJumpingHash, true);
                    SetBool(IsFallingHash, false);
                }
                else if (velocityY < -0.1f)
                {
                    SetBool(IsJumpingHash, false);
                    SetBool(IsFallingHash, true);
                }
            }
            else
            {
                SetBool(IsJumpingHash, false);
                SetBool(IsFallingHash, false);
            }
        }
        
        private void UpdateFlip()
        {
            if (fallenAngelsAnimator == null) return;
            
            float velocityX = playerController.Velocity.x;
            
            if (Mathf.Abs(velocityX) > 0.1f)
            {
                Vector3 scale = fallenAngelsAnimator.transform.localScale;
                fallenAngelsAnimator.transform.localScale = new Vector3(
                    velocityX > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x),
                    scale.y,
                    scale.z
                );
            }
        }
        
        public void TriggerHurt()
        {
            if (fallenAngelsAnimator == null || isDead) return;
            
            isHurt = true;
            SetTrigger(HurtTriggerHash);
            SetBool(IsHurtHash, true);
            Invoke(nameof(EndHurt), 0.4f);
        }
        
        private void EndHurt()
        {
            isHurt = false;
            if (fallenAngelsAnimator != null)
            {
                SetBool(IsHurtHash, false);
            }
        }
        
        private void OnPlayerDeath()
        {
            if (fallenAngelsAnimator == null) return;
            
            isDead = true;
            SetTrigger(DeathTriggerHash);
            SetBool(IsDeadHash, true);
        }
        
        private void SetBool(int hash, bool value)
        {
            if (fallenAngelsAnimator != null && fallenAngelsAnimator.runtimeAnimatorController != null)
            {
                if (HasParameter(hash))
                {
                    fallenAngelsAnimator.SetBool(hash, value);
                }
                else
                {
                    Debug.LogWarning($"[AnimController] Parameter hash {hash} not found in Animator. Check parameter names.");
                }
            }
            else
            {
                Debug.LogWarning($"[AnimController] SetBool failed: Animator={fallenAngelsAnimator != null}, Controller={fallenAngelsAnimator?.runtimeAnimatorController != null}");
            }
        }
        
        private void SetTrigger(int hash)
        {
            if (fallenAngelsAnimator != null && fallenAngelsAnimator.runtimeAnimatorController != null)
            {
                if (HasParameter(hash))
                {
                    fallenAngelsAnimator.SetTrigger(hash);
                }
            }
        }
        
        private bool HasParameter(int hash)
        {
            if (fallenAngelsAnimator == null || fallenAngelsAnimator.runtimeAnimatorController == null)
            {
                return false;
            }
            
            foreach (AnimatorControllerParameter param in fallenAngelsAnimator.parameters)
            {
                if (param.nameHash == hash)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        public void ResetAnimationState()
        {
            isDead = false;
            isHurt = false;
            
            if (fallenAngelsAnimator == null ||
                fallenAngelsAnimator.runtimeAnimatorController == null)
            {
                return;
            }
            
            if (HasParameter(DeathTriggerHash)) fallenAngelsAnimator.ResetTrigger(DeathTriggerHash);
            if (HasParameter(HurtTriggerHash)) fallenAngelsAnimator.ResetTrigger(HurtTriggerHash);
            if (HasParameter(JumpTriggerHash)) fallenAngelsAnimator.ResetTrigger(JumpTriggerHash);
            
            if (HasParameter(IsDeadHash)) fallenAngelsAnimator.SetBool(IsDeadHash, false);
            if (HasParameter(IsHurtHash)) fallenAngelsAnimator.SetBool(IsHurtHash, false);
            if (HasParameter(IsJumpingHash)) fallenAngelsAnimator.SetBool(IsJumpingHash, false);
            if (HasParameter(IsFallingHash)) fallenAngelsAnimator.SetBool(IsFallingHash, false);
            if (HasParameter(IsRunningHash)) fallenAngelsAnimator.SetBool(IsRunningHash, false);
            
            fallenAngelsAnimator.Play("Idle", 0, 0f);
        }
    }
}
