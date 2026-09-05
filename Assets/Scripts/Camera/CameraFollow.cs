using UnityEngine;

namespace MazeRunner.Camera
{
    /// <summary>
    /// Simple smooth camera follow script for 2D games.
    /// Follows a target with damping and optional offset.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        
        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 0.125f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = true;
        
        [Header("Boundaries (Optional)")]
        [SerializeField] private bool useBoundaries = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f);
        [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);
        
        [Header("Look Ahead")]
        [SerializeField] private bool useLookAhead = false;
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSpeed = 2f;
        
        private Vector3 velocity = Vector3.zero;
        private float currentLookAhead = 0f;

        private void LateUpdate()
        {
            if (target == null) return;
            
            // Calculate desired position
            Vector3 desiredPosition = target.position + offset;
            
            // Add look ahead if enabled
            if (useLookAhead)
            {
                float targetLookAhead = target.GetComponent<Rigidbody2D>()?.linearVelocity.x ?? 0f;
                targetLookAhead = Mathf.Clamp(targetLookAhead, -1f, 1f) * lookAheadDistance;
                currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, Time.deltaTime * lookAheadSpeed);
                desiredPosition.x += currentLookAhead;
            }
            
            // Apply follow constraints
            if (!followX)
            {
                desiredPosition.x = transform.position.x;
            }
            if (!followY)
            {
                desiredPosition.y = transform.position.y;
            }
            
            // Smoothly move camera
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            
            // Apply boundaries if enabled
            if (useBoundaries)
            {
                smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
                smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
            }
            
            transform.position = smoothedPosition;
        }
        
        /// <summary>
        /// Set a new target for the camera to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        /// <summary>
        /// Instantly snap camera to target position without smoothing.
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            
            Vector3 targetPosition = target.position + offset;
            
            if (!followX)
            {
                targetPosition.x = transform.position.x;
            }
            if (!followY)
            {
                targetPosition.y = transform.position.y;
            }
            
            transform.position = targetPosition;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!useBoundaries) return;
            
            // Draw camera boundaries
            Gizmos.color = Color.yellow;
            Vector3 bottomLeft = new Vector3(minBounds.x, minBounds.y, 0f);
            Vector3 topRight = new Vector3(maxBounds.x, maxBounds.y, 0f);
            Vector3 topLeft = new Vector3(minBounds.x, maxBounds.y, 0f);
            Vector3 bottomRight = new Vector3(maxBounds.x, minBounds.y, 0f);
            
            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
        }
    }
}