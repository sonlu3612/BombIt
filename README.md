# BombIt - Bomberman Game

A modern Bomberman game built with **Unity** featuring an intelligent AI system, balanced gameplay, and impressive graphics.

## Main Features

- **Advanced AI System**: Intelligent bots with priority-based state machine
- **Classic Gameplay**: Plant bombs, break blocks, attack other players
- **Multi-Difficulty AI**: Easy, Normal, Hard - each difficulty with different gameplay
- **Smart Pathfinding**: Uses A* algorithm for optimal pathfinding
- **Modern Interface**: UI Toolkit with sophisticated game state management
- **Audio & Effects**: SFX and music to enhance gameplay experience

## Requirements

### Software Requirements
- **Unity Engine**: Version **2022.3 LTS** or higher
- **.NET Framework**: .NET 6.0 or higher (bundled with Unity)
- **Git**: For cloning the repository

### Minimum Hardware
- **CPU**: Intel i5 / AMD Ryzen 5 or equivalent
- **RAM**: 8GB
- **Storage**: 5GB for project (varies with packages)
- **GPU**: Integrated graphics or dedicated GPU

## Installation Guide

### 1. Clone Repository

```bash
git clone https://github.com/sonlu3612/BombIt.git
cd BombIt
```

### 2. Open Project in Unity

**Method 1: Using Unity Hub**
1. Open Unity Hub
2. Select **"Open"** → choose `BombIt` folder
3. Select Unity version **2022.3 LTS** (or higher)
4. Wait for Unity to import the project (5-10 minutes on first import)

**Method 2: Direct Open**
1. Open Unity Hub or Unity Editor
2. Select **"Open Project"**
3. Navigate to the `BombIt` folder
4. Select the root folder and click **"Select Folder"**

### 3. Wait for Asset Import

- Unity will automatically import all assets (2D sprites, scripts, materials)
- Check **Console** (Ctrl+Shift+C) for any errors
- All errors will be highlighted in red

### 4. Check Scene

1. Open the **Assets/Scenes** folder
2. Double-click on **"MainScene"** or **"GameScene"** to load
3. Check if GameObjects appear in Hierarchy

## Running the Game

### Play in Unity Editor
1. Open a scene from **Assets/Scenes**
2. Click the **Play** button at the top center of the editor
3. Use keyboard to control:
   - **Arrow Keys** or **WASD**: Move
   - **Space**: Plant bomb
   - **Esc**: Pause/Quit

### Build & Play Standalone
1. **File** → **Build Settings**
2. Select **PC, Mac & Linux Standalone** (or other platform)
3. Click **"Build and Run"**
4. Choose a folder to save the build
5. Game will run directly

## Project Structure

```
BombIt/
├── Assets/
│   ├── Scripts/              # Main C# code
│   │   ├── AI/              # Bot AI system
│   │   ├── Gameplay/        # Game logic, bomb, player
│   │   ├── UI/              # UI system
│   │   └── Utils/           # Helper classes
│   ├── Scenes/              # Game scenes
│   ├── Prefabs/             # Reusable GameObjects
│   ├── Art/                 # Sprites, textures
│   │   ├── Sprites/         # 2D sprites
│   │   └── Audio/           # Sound effects, music
│   └── Settings/            # Project settings
├── Libraries/               # External packages
├── ProjectSettings/         # Unity project settings
└── README.md               # This guide file
```

## AI System

The project uses an advanced AI system based on **Priority-based State Machine**:

### Main States
1. **EscapeAfterBombState** - Escape after planting bomb
2. **EvadeBombState** - Evade bomb explosions
3. **PlantBombState** - Plant bomb strategically
4. **GetItemState** - Collect items
5. **AttackEnemyState** - Attack other players
6. **BreakBlockState** - Break blocks
7. **WanderState** - Explore map

### Difficulty Levels
- **Easy**: Slow thinking (0.25s), low bomb placement (70%)
- **Normal**: Balanced (0.12s), bomb placement 95%
- **Hard**: Fast thinking (0.08s), high bomb placement (98%)

For more details: see [**AI_SYSTEM_DOCUMENTATION.md**](AI_SYSTEM_DOCUMENTATION.md)

## Development

### Scripts Folder Structure
```
Scripts/
├── Core/
│   ├── GameFlowConfig.cs     # Game configuration
│   └── GameManager.cs        # Game state management
├── Gameplay/
│   ├── PlayerController.cs   # Player control
│   ├── BombController.cs     # Bomb logic
│   └── ExplosionController.cs # Explosion handling
├── AI/
│   ├── BotBrain.cs          # Main bot brain
│   ├── BotStateMachine.cs   # State machine
│   ├── BotNavigator.cs      # A* pathfinding
│   ├── States/              # All AI states
│   └── BotConfig.cs         # AI configuration
├── UI/
│   ├── GameUI.cs            # In-game UI
│   └── MenuUI.cs            # Main menu
└── Utils/
    ├── GridUtility.cs       # Grid helpers
    └── DebugVisualizer.cs   # Debug tools
```

### Build Process
```bash
# Clean build
File → New Window → Open Project
# or
rm -r Library
rm -r obj
# Then reopen the project
```

### Debug Mode
Enable debug mode to visualize AI:
1. Select Bot GameObject in Hierarchy
2. Find **BotBrain** component in Inspector
3. Enable checkboxes:
   - `debugDrawPath` - Show pathfinding
   - `debugDrawSense` - Show sensing
   - `debugDrawDanger` - Show danger zone

## Troubleshooting

### "Assembly-CSharp dll not found"
**Solution:**
- Close Unity → Delete `Library` folder → Reopen project

### Scripts don't compile
**Solution:**
- Check Console (Ctrl+Shift+C)
- Read error messages
- Ensure Unity version >= 2022.3

### Scene won't load
**Solution:**
- Open **Assets/Scenes/MainScene** or **GameScene**
- If still fails → delete Scene → recreate from prefabs

### Bot runs too fast/slow
**Solution:**
- Go to **Assets/Settings/BotConfig**
- Adjust `thinkInterval` (default 0.12s)
- Or change difficulty level

## Future Features

- [ ] Online multiplayer mode
- [ ] Special items (shield, speed up)
- [ ] Level editor
- [ ] Leaderboard
- [ ] Mobile support (Android/iOS)

## License

This project is released under the **MIT** license. See [LICENSE](LICENSE) file for details.

## Development Team

- **Author**: sonlu3612
- **AI System**: Priority-based State Machine + A* Pathfinding
- **Engine**: Unity 2022.3 LTS+

## Support & Feedback

- Issues: [GitHub Issues](https://github.com/sonlu3612/BombIt/issues)
- Discussions: [GitHub Discussions](https://github.com/sonlu3612/BombIt/discussions)

---

**Enjoy the game! Happy Bombing!**

