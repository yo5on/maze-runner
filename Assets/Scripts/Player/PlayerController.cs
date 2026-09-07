using UnityEngine;
using UnityEngine.InputSystem;

namespace MazeRunner.Player
{
    /// <summary>
    /// Handles player movement, jumping, and physics-based controls.
    /// Uses the New Input System for input handling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 50f;
        [SerializeField] private float velocityPower = 0.9f;

        [Header("Jumping")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float fallGravityMultiplier = 2.5f;
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.2f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.1f);
        [SerializeField] private LayerMask groundLayer;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        // Components
        private Rigidbody2D rb;
        private PlayerInput playerInput;
        
        // Input
        private InputAction moveAction;
        private InputAction jumpAction;
        private Vector2 moveInput;
        
        // State
        private bool isGrounded;
        private bool wasGrounded;
        private float lastGroundedTime;
        private float lastJumpPressedTime;
        private bool isJumping;
        private bool isFalling;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            
            // Get input actions
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
        }

        private void OnEnable()
        {
            jumpAction.performed += OnJumpPerformed;
            jumpAction.canceled += OnJumpCanceled;
        }

        private void OnDisable()
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;
        }

        private void Update()
        {
            // Read input
            moveInput = moveAction.ReadValue<Vector2>();
            
            // Update timers
            UpdateTimers();
            
            // Check ground state
            CheckGrounded();
            
            // Handle jump buffering
            HandleJumpBuffer();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleGravity();
        }

        private void UpdateTimers()
        {
            lastGroundedTime -= Time.deltaTime;
            lastJumpPressedTime -= Time.deltaTime;
        }

        private void CheckGrounded()
        {
            wasGrounded = isGrounded;
            
            if (groundCheck == null)
            {
                isGrounded = false;
                return;
            }
            
            // Check for ground using OverlapBox
            isGrounded = Physics2D.OverlapBox(
                groundCheck.position,
                groundCheckSize,
                0f,
                groundLayer
            );
            
            // Update coyote time
            if (isGrounded)
            {
                lastGroundedTime = coyoteTime;
                isJumping = false;
                isFalling = false;
            }
            
            // Check if falling
            if (!isGrounded && rb.linearVelocity.y < 0)
            {
                isFalling = true;
            }
        }

        private void HandleMovement()
        {
            float targetSpeed = moveInput.x * moveSpeed;
            float speedDifference = targetSpeed - rb.linearVelocity.x;
            
            // Choose acceleration or deceleration
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            
            // Apply acceleration with power for more responsive feel
            float movement = Mathf.Pow(Mathf.Abs(speedDifference) * accelRate, velocityPower) * Mathf.Sign(speedDifference);
            
            // Apply force to rigidbody
            rb.AddForce(movement * Vector2.right);
        }

        private void HandleGravity()
        {
            // Apply extra gravity when falling for better jump feel
            if (rb.linearVelocity.y < 0)
            {
                rb.gravityScale = fallGravityMultiplier;
            }
            else
            {
                rb.gravityScale = 1f;
            }
        }

        private void HandleJumpBuffer()
        {
            // Jump with coyote time and buffer
            if (lastJumpPressedTime > 0 && lastGroundedTime > 0 && !isJumping)
            {
                Jump();
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            lastJumpPressedTime = jumpBufferTime;
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            // Cut jump short if button released early
            if (isJumping && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        private void Jump()
        {
            // Reset vertical velocity and apply jump force
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            
            isJumping = true;
            lastGroundedTime = 0f;
            lastJumpPressedTime = 0f;
            
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayJump();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || groundCheck == null) return;
            
            // Draw ground check area
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
            
            // Draw velocity indicator
            Gizmos.color = Color.blue;
            if (rb != null)
            {
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb.linearVelocity * 0.1f);
            }
        }

        // Public getters for other systems
        public bool IsGrounded => isGrounded;
        public bool IsJumping => isJumping;
        public bool IsFalling => isFalling;
        public Vector2 Velocity => rb.linearVelocity;
    }
}