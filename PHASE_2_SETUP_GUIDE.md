# Phase 2 Setup Guide - Player Controller

## Scripts Created

✅ **PlayerController.cs** - `Assets/Scripts/Player/PlayerController.cs`
✅ **CameraFollow.cs** - `Assets/Scripts/Camera/CameraFollow.cs`

---

## Unity Setup Instructions

### Step 1: Create Ground Layer

1. **Edit > Project Settings > Tags and Layers**
2. Find first empty **Layer** slot (e.g., Layer 6)
3. Name it: **"Ground"**
4. Click outside to save

---

### Step 2: Create Ground Platform

1. **GameObject > 2D Object > Sprites > Square**
2. Rename to **"Ground"**
3. **Transform:**
   - Position: `(0, -2, 0)`
   - Scale: `(20, 1, 1)`
4. **Change Color (optional):**
   - Select Ground
   - In Sprite Renderer, click Color
   - Choose brown/gray color
5. **Add Component: Box Collider 2D**
6. **Set Layer:**
   - Top of Inspector, change Layer dropdown to **"Ground"**

---

### Step 3: Create Test Platforms

**Platform 1 (Low):**
1. GameObject > 2D Object > Sprites > Square
2. Rename to **"Platform_01"**
3. Transform:
   - Position: `(5, 0, 0)`
   - Scale: `(3, 0.5, 1)`
4. Add Component: **Box Collider 2D**
5. Set Layer: **"Ground"**

**Platform 2 (Medium):**
1. Duplicate Platform_01 (Ctrl+D)
2. Rename to **"Platform_02"**
3. Transform:
   - Position: `(10, 2, 0)`
   - Scale: `(2, 0.5, 1)`

**Platform 3 (High):**
1. Duplicate Platform_02
2. Rename to **"Platform_03"**
3. Transform:
   - Position: `(15, 4, 0)`
   - Scale: `(2.5, 0.5, 1)`

---

### Step 4: Create Player

1. **GameObject > 2D Object > Sprites > Square**
2. Rename to **"Player"**
3. **Transform:**
   - Position: `(0, 1, 0)`
   - Scale: `(1, 1, 1)` (default)
4. **Change Color:**
   - Sprite Renderer > Color: Choose blue or green
5. **Add Component: Rigidbody2D**
   - Body Type: **Dynamic**
   - Gravity Scale: **3**
   - Constraints: Check **Freeze Rotation Z**
6. **Add Component: Capsule Collider 2D**
   - Adjust size if needed (should fit the sprite)
7. **Add Component: Player Input**
   - Actions: Drag **InputSystem_Actions** from Project window
   - Default Map: **Player**
   - Behavior: **Send Messages** or **Invoke Unity Events**
8. **Add Component: Player Controller (Script)**
   - Should auto-attach from Assets/Scripts/Player/

---

### Step 5: Create Ground Check (for Player)

1. **Right-click Player in Hierarchy > Create Empty**
2. Rename to **"GroundCheck"**
3. **Transform:**
   - Position: `(0, -0.5, 0)` (relative to player - bottom of player)
   - Reset Rotation and Scale
4. **Important:** GroundCheck should be a **child** of Player

---

### Step 6: Configure Player Controller

**Select Player in Hierarchy, look at Inspector:**

**Movement Section:**
- Move Speed: `7`
- Acceleration: `50`
- Deceleration: `50`
- Velocity Power: `0.9`

**Jumping Section:**
- Jump Force: `12`
- Fall Gravity Multiplier: `2.5`
- Jump Cut Multiplier: `0.5`
- Coyote Time: `0.15`
- Jump Buffer Time: `0.2`

**Ground Detection Section:**
- Ground Check: **Drag GroundCheck object here**
- Ground Check Size: `X: 0.4, Y: 0.1`
- Ground Layer: **Select "Ground" layer**

**Debug Section:**
- Show Debug Gizmos: **Checked** (to see ground detection)

---

### Step 7: Setup Camera

1. **Select Main Camera** in Hierarchy
2. **Add Component: Camera Follow (Script)**
3. **Configure Camera Follow:**

**Target:**
- Target: **Drag Player object here**

**Follow Settings:**
- Smooth Speed: `0.125`
- Offset: `X: 0, Y: 2, Z: -10`
- Follow X: **Checked**
- Follow Y: **Checked**

**Boundaries:**
- Use Boundaries: **Unchecked** (for now)

**Look Ahead:**
- Use Look Ahead: **Unchecked** (for now - enable later for polish)

---

### Step 8: Save Everything

1. **File > Save** (Ctrl+S)
2. **Create Prefab:**
   - Drag **Player** from Hierarchy to `Assets/Prefabs/Player/`
   - Name it **"Player.prefab"**
3. **Save Platforms (optional):**
   - Drag each platform to `Assets/Prefabs/Environment/`

---

## Testing Checklist

### Basic Movement
- [ ] Press **Play** button
- [ ] Press **A** or **Left Arrow** - Player moves left
- [ ] Press **D** or **Right Arrow** - Player moves right
- [ ] Release keys - Player decelerates smoothly
- [ ] Movement feels responsive

### Jumping
- [ ] Press **Space** - Player jumps
- [ ] Hold **Space** - Player jumps higher
- [ ] Tap **Space** quickly - Player does short jump
- [ ] Can't jump in mid-air
- [ ] Can jump again after landing

### Ground Detection
- [ ] In Scene view, look at Player's Ground Check
- [ ] When on ground: **Green box** appears
- [ ] When in air: **Red box** appears
- [ ] Player lands on platforms correctly

### Camera
- [ ] Camera follows player smoothly
- [ ] Camera stays at appropriate distance
- [ ] Camera doesn't jitter or shake
- [ ] Can see player and platforms clearly

### Physics
- [ ] Player falls naturally
- [ ] Player doesn't rotate when moving
- [ ] Player collides with platforms correctly
- [ ] Player doesn't fall through platforms

---

## Troubleshooting

**Problem: Player doesn't move**
- Check Input Actions asset is assigned to Player Input component
- Check Default Map is set to "Player"
- Verify Move Speed is not 0

**Problem: Player won't jump**
- Check Ground Layer is set correctly in Player Controller
- Verify GroundCheck is assigned and positioned correctly
- Check Jump Force is not 0
- Make sure platforms have Layer set to "Ground"

**Problem: Player falls through platforms**
- Verify platforms have Box Collider 2D
- Check platforms Layer is set to "Ground"
- Verify Rigidbody2D is on Player (not platforms)

**Problem: Camera doesn't follow**
- Check Target is assigned to Player in Camera Follow component
- Verify Camera Follow script is on Main Camera
- Check Smooth Speed is not 0

**Problem: Ground detection not working**
- GroundCheck position should be at bottom of player
- Ground Layer must be selected in Player Controller
- Ground Check Size should match player width
- Scene view should show green/red gizmo when selected

**Problem: Compilation errors**
- Check Console (Window > Console) for specific errors
- Verify both scripts are in correct folders
- Ensure Input System package is installed

---

## Expected Behavior

✅ Player moves left/right with smooth acceleration  
✅ Player jumps with variable height based on button hold  
✅ Player has coyote time (can jump shortly after leaving platform)  
✅ Player has jump buffering (can press jump slightly before landing)  
✅ Camera smoothly follows player  
✅ Debug gizmos show ground detection (green = grounded, red = in air)  
✅ Player can navigate between platforms  

---

## Next Steps (After Testing)

Once basic movement is working:
1. Test and adjust movement speed values
2. Test and adjust jump force
3. Fine-tune acceleration/deceleration
4. Experiment with gravity multiplier
5. Test coyote time and jump buffer

**Do not proceed to Phase 3 until movement feels good!**

---

## Notes

- Ground Check gizmo only visible when Player is selected
- Blue line shows velocity (for debugging)
- All values are configurable in Inspector
- Can adjust values while game is running (changes won't save)
- To save adjusted values: Copy component, stop game, paste values