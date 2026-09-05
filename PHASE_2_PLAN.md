# Phase 2: Player Controller - Implementation Plan


## Overview
Create a responsive, physics-based player controller with smooth movement and jumping.

---

## Step 1: Basic Player Setup

### In Unity Editor:
1. Open TestLevel scene (Scenes/Levels/TestLevel.unity)
2. Create Player GameObject:
   - GameObject > 2D Object > Sprites > Square
   - Rename to "Player"
   - Change color in Sprite Renderer (e.g., blue)
   - Position at (0, 1, 0)
3. Add components:
   - Rigidbody2D (Gravity Scale: 3, Constraints: Freeze Rotation Z)
   - Capsule Collider 2D (adjust size to sprite)
4. Save as prefab in Prefabs/Player/Player.prefab

### Script to Create:
**PlayerController.cs**
- Movement speed (configurable)
- Acceleration/deceleration
- Jump force (configurable)
- Max fall speed
- Ground detection
- Input handling via Input System

---

## Step 2: Movement System

### Features:
- Horizontal movement (A/D or Arrow keys)
- Smooth acceleration when starting to move
- Smooth deceleration when stopping
- Rigidbody2D.velocity-based movement
- Configurable max speed

### Inspector Parameters:
- `moveSpeed` (default: 7f)
- `acceleration` (default: 50f)
- `deceleration` (default: 50f)

---

## Step 3: Jump System

### Features:
- Variable jump height (hold Space for higher jump)
- Gravity multiplier when falling
- Jump cut-off when button released early
- Ground check before allowing jump

### Inspector Parameters:
- `jumpForce` (default: 12f)
- `fallGravityMultiplier` (default: 2.5f)
- `jumpCutMultiplier` (default: 0.5f)

### Ground Detection:
- Use Physics2D.OverlapCircle or Raycast
- Ground check position slightly below player
- Layer-based detection (Ground layer)

---

## Step 4: Test Environment

### In Unity Editor:
1. Create ground:
   - GameObject > 2D Object > Sprites > Square
   - Scale to (20, 1, 1)
   - Position at (0, -2, 0)
   - Add Box Collider 2D
   - Layer: Ground (create if needed)

2. Create platforms:
   - Create 2-3 platforms at different heights
   - Different widths to test jumping
   - All on Ground layer

3. Save platforms as prefabs in Prefabs/Environment/

---

## Step 5: Camera Setup

### Using Cinemachine:
1. GameObject > Cinemachine > 2D Camera
2. Set Follow target to Player
3. Adjust Dead Zone and Soft Zone
4. Set camera bounds (optional)

### Parameters:
- Damping for smooth follow
- Look ahead distance (optional)
- Screen boundaries

---

## Step 6: Polish

### Additional Features:
- Coyote time (grace period after leaving platform)
- Jump buffering (pre-input jump)
- Max fall speed limiter
- Debug visualization (ground check, velocity)

---

## Testing Checklist

- [ ] Player moves left/right smoothly
- [ ] Acceleration feels responsive
- [ ] Deceleration stops player naturally
- [ ] Jump height varies with button hold
- [ ] Can't jump in mid-air
- [ ] Camera follows player smoothly
- [ ] Player can land on platforms
- [ ] Movement speed feels appropriate
- [ ] Jump force feels appropriate

---

## Code Structure

```
PlayerController.cs
├── Input System references
├── Movement variables
├── Jump variables
├── Ground check variables
├── Component references (Rigidbody2D)
├── Awake() - Get components
├── OnEnable() - Subscribe to input
├── OnDisable() - Unsubscribe from input
├── Update() - Handle jump input timing
├── FixedUpdate() - Handle physics movement
├── HandleMovement() - Process horizontal movement
├── HandleJump() - Process jump logic
├── IsGrounded() - Check if on ground
└── OnDrawGizmos() - Debug visualization
```

---

## Notes

- Use FixedUpdate() for physics calculations
- Use Update() for input timing (jump buffering)
- Expose important values to Inspector with [SerializeField]
- Add [Header("Movement")] attributes for organization
- Keep movement responsive (avoid sluggish feel)
- Test with different gravity scales if needed
