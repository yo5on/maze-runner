# Scripts Directory

All C# scripts organized by system.

## Subdirectories

- **Player/** - Player controller, movement, physics, death/respawn
- **Customization/** - Character customization manager and layer system
- **Enemies/** - Enemy base classes, AI behaviors, collision
- **Collectibles/** - Collectible items (coins, gems, power-ups)
- **Levels/** - Level management, transitions, checkpoints
- **UI/** - UI controllers for menus, HUD, pause screen
- **Managers/** - Global managers (GameManager, AudioManager, SaveManager)
- **Camera/** - Camera controller and follow scripts
- **SaveSystem/** - Save/load functionality

## Naming Conventions

- Use PascalCase for class names: `PlayerController.cs`
- One class per file
- Match filename to class name
- Use meaningful, descriptive names

## Code Style

- Follow Unity C# conventions
- Use `[SerializeField]` for Inspector-exposed private fields
- Add XML documentation comments for public APIs
- Keep classes focused (Single Responsibility Principle)
- Use namespaces where appropriate