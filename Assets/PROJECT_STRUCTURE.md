# Maze Runner - Project Structure

## Overview
Original 2D side-scrolling platformer built with Unity 6 and Universal Render Pipeline (URP).

**Project Name:** maze-runner  
**Unity Version:** 6000.5.1f1  
**Template:** Universal 2D (URP)  
**Input System:** New Input System (v1.19.0)

---

## Folder Structure

```
Assets/
├── Art/                          # All visual assets
│   ├── Characters/              # Player and NPC sprites
│   ├── Enemies/                 # Enemy sprites and animations
│   ├── Environment/             # Platforms, backgrounds, tiles
│   └── UI/                      # UI sprites and icons
│
├── Audio/                        # All audio assets
│   ├── Music/                   # Background music tracks
│   └── SFX/                     # Sound effects
│
├── Animations/                   # Animation controllers and clips
│
├── Materials/                    # 2D materials and physics materials
│
├── Prefabs/                      # Reusable game objects
│   ├── Player/                  # Player prefab and variants
│   ├── Enemies/                 # Enemy prefabs
│   ├── Collectibles/            # Coins, gems, pickups
│   ├── Environment/             # Platforms, hazards, checkpoints
│   └── UI/                      # UI prefabs
│
├── Scenes/                       # Unity scenes
│   ├── MainMenu/                # Main menu scene
│   ├── Customization/           # Character customization scene
│   ├── Levels/                  # Level scenes (Level01, Level02, etc.)
│   └── SampleScene.unity        # Default test scene
│
├── Scripts/                      # All C# scripts
│   ├── Player/                  # Player controller and movement
│   ├── Customization/           # Character customization system
│   ├── Enemies/                 # Enemy AI and behaviors
│   ├── Collectibles/            # Collectible logic
│   ├── Levels/                  # Level management
│   ├── UI/                      # UI controllers
│   ├── Managers/                # Game managers (GameManager, AudioManager)
│   ├── Camera/                  # Camera controller scripts
│   └── SaveSystem/              # Save/load functionality
│
├── ScriptableObjects/            # Data-driven configurations
│
├── Settings/                     # URP and render settings
│   ├── Renderer2D.asset
│   ├── UniversalRP.asset
│   └── URP2DSceneTemplate.unity
│
└── InputSystem_Actions.inputactions  # Input action mappings
```

---

## Key Systems (To Be Implemented)

### Phase 1: Foundation ✓
- [x] Project structure setup
- [x] Cinemachine package installed
- [ ] Basic player GameObject
- [ ] Test environment scene

### Phase 2: Player System
- Movement (left/right with acceleration)
- Jumping (variable height)
- Ground detection
- Coyote time & jump buffering
- Death & respawn

### Phase 3: Character Customization
- Modular layer system (body, hair, clothes, etc.)
- CharacterCustomizationManager
- Layer switching and rendering

### Phase 4: Customization UI
- Selection interface
- Live preview
- Save/load system

### Phase 5: Animation
- Player animation states
- Layer synchronization

### Phase 6: Enemies & Collectibles
- Enemy base class
- AI behaviors (patrol, detection)
- Collectible system

### Phase 7: Level System
- Multiple level scenes
- Checkpoints
- Level transitions

### Phase 8: UI & Menus
- Main menu
- HUD
- Pause menu
- Level complete screen

### Phase 9: Audio & Polish
- AudioManager
- Sound effects
- Music system
- Particle effects

---

## Installed Packages

- **Cinemachine** 3.1.2 - Camera system
- **Input System** 1.19.0 - Player input
- **2D Animation** 15.1.0 - Sprite animation
- **2D Sprite** 1.0.0 - Sprite rendering
- **2D Tilemap** 1.0.0 + Extras 8.0.3 - Level design
- **Universal RP** 17.5.0 - Render pipeline
- **Unity UI** 2.5.0 - User interface
- **Timeline** 1.8.12 - Cutscenes

---

## Input Bindings

### Keyboard
- **A / Left Arrow** - Move left
- **D / Right Arrow** - Move right
- **W / Up Arrow** - Move up (UI navigation)
- **S / Down Arrow** - Move down (UI navigation)
- **Space** - Jump
- **Esc** - Pause menu

### Gamepad
- **Left Stick** - Movement
- **Button South (A/X)** - Jump
- **Start** - Pause menu

---

## Notes

- All art assets must be original (no copyrighted Mario/Nintendo assets)
- Use placeholder sprites during development
- Keep scripts focused and modular
- Follow Unity C# conventions
- Use SerializeField for Inspector-exposed private fields
- Document complex logic with comments