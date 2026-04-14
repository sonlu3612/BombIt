# 🎮 BombIt - Bomberman Game

Một trò chơi Bomberman hiện đại được xây dựng bằng **Unity** với hệ thống AI thông minh, gameplay cân bằng và đồ họa ấn tượng.

## ✨ Tính Năng Chính

- 🤖 **Hệ thống AI tiên tiến**: Bot thông minh với priority-based state machine
- 💣 **Gameplay cổ điển**: Trồng bom, phá block, tấn công người chơi khác
- 🎯 **AI đa cấp độ**: Easy, Normal, Hard - mỗi cấp độ có cách chơi khác nhau
- 🗺️ **Pathfinding thông minh**: Sử dụng A* algorithm để tìm đường tối ưu
- 🎨 **Giao diện hiện đại**: UI Toolkit, quản lý trạng thái game tinh tế
- 🔊 **Âm thanh và hiệu ứng**: SFX và music để tăng trải nghiệm chơi

## 📋 Requirements

### Phần Mềm Cần Thiết
- **Unity Engine**: Phiên bản **2022.3 LTS** hoặc cao hơn
- **.NET Framework**: .NET 6.0 hoặc cao hơn (đi kèm với Unity)
- **Git**: Để clone repository

### Phần Cứng Tối Thiểu
- **CPU**: Intel i5 / AMD Ryzen 5 hoặc tương đương
- **RAM**: 8GB
- **Storage**: 5GB cho project (tùy packages)
- **GPU**: Integrated graphics hoặc dedicated GPU

## 🚀 Hướng Dẫn Cài Đặt

### 1. Clone Repository

```bash
git clone https://github.com/sonlu3612/BombIt.git
cd BombIt
```

### 2. Mở Project trong Unity

**Cách 1: Sử dụng Unity Hub**
1. Mở Unity Hub
2. Chọn **"Open"** → chọn thư mục `BombIt`
3. Chọn Unity version **2022.3 LTS** (hoặc cao hơn)
4. Đợi Unity import project (5-10 phút lần đầu)

**Cách 2: Mở trực tiếp**
1. Mở Unity Hub hoặc Unity Editor
2. Chọn **"Open Project"**
3. Điều hướng đến thư mục `BombIt`
4. Chọn thư mục gốc và click **"Select Folder"**

### 3. Chờ Import Assets

- Unity sẽ tự động import tất cả assets (2D sprites, scripts, materials)
- Kiểm tra **Console** (Ctrl+Shift+C) để xem có lỗi gì không
- Tất cả lỗi sẽ được highlight màu đỏ

### 4. Kiểm Tra Scene

1. Mở thư mục **Assets/Scenes**
2. Double-click vào **"MainScene"** hoặc **"GameScene"** để load
3. Kiểm tra Hierarchy có GameObject không

## 🎮 Chạy Game

### Play trong Unity Editor
1. Mở scene muốn chơi từ **Assets/Scenes**
2. Click nút **Play** (▶️) ở giữa trên cùng của editor
3. Sử dụng keyboard để điều khiển:
   - **Arrow Keys** hoặc **WASD**: Di chuyển
   - **Space**: Đặt bom
   - **Esc**: Pause/Quit

### Build & Play Standalone
1. **File** → **Build Settings**
2. Chọn **PC, Mac & Linux Standalone** (hoặc platform khác)
3. Click **"Build and Run"**
4. Chọn thư mục để lưu build
5. Game sẽ chạy trực tiếp

## 📁 Cấu Trúc Project

```
BombIt/
├── Assets/
│   ├── Scripts/              # Code C# chính
│   │   ├── AI/              # Hệ thống AI Bot
│   │   ├── Gameplay/        # Game logic, bomb, player
│   │   ├── UI/              # UI system
│   │   └── Utils/           # Helper classes
│   ├── Scenes/              # Các scene game
│   ├── Prefabs/             # Reusable GameObjects
│   ├── Art/                 # Sprites, textures
│   │   ├── Sprites/         # 2D sprites
│   │   └── Audio/           # Sound effects, music
│   └── Settings/            # Project settings
├── Libraries/               # External packages
├── ProjectSettings/         # Unity project settings
└── README.md               # File hướng dẫn này
```

## 🤖 Hệ Thống AI

Dự án sử dụng một hệ thống AI tiên tiến dựa trên **Priority-based State Machine**:

### Các State Chính
1. **EscapeAfterBombState** - Thoát sau khi trồng bom
2. **EvadeBombState** - Trốn tránh bom nổ
3. **PlantBombState** - Trồng bom chiến lược
4. **GetItemState** - Lượm item
5. **AttackEnemyState** - Tấn công người chơi
6. **BreakBlockState** - Phá block
7. **WanderState** - Khám phá map

### Cấp Độ Khó
- **Easy**: Suy nghĩ chậm (0.25s), trồng bom ít (70%)
- **Normal**: Cân bằng (0.12s), trồng bom 95%
- **Hard**: Suy nghĩ nhanh (0.08s), trồng bom hay (98%)

Chi tiết hơn: xem [**AI_SYSTEM_DOCUMENTATION.md**](AI_SYSTEM_DOCUMENTATION.md)

## 📚 Phát Triển

### Cấu Trúc Thư Mục Scripts
```
Scripts/
├── Core/
│   ├── GameFlowConfig.cs     # Cấu hình game
│   └── GameManager.cs        # Quản lý game state
├── Gameplay/
│   ├── PlayerController.cs   # Điều khiển player
│   ├── BombController.cs     # Logic bom
│   └── ExplosionController.cs # Xử lý nổ bom
├── AI/
│   ├── BotBrain.cs          # Bộ não bot chính
│   ├── BotStateMachine.cs   # State machine
│   ├── BotNavigator.cs      # Pathfinding A*
│   ├── States/              # Tất cả AI states
│   └── BotConfig.cs         # Cấu hình AI
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
# hoặc
rm -r Library
rm -r obj
# Rồi mở lại project
```

### Debug Mode
Bật debug mode để visualize AI:
1. Chọn Bot GameObject trong Hierarchy
2. Tìm component **BotBrain** trong Inspector
3. Enable các checkbox:
   - `debugDrawPath` - Hiễn thị đường đi
   - `debugDrawSense` - Hiển thị cảm nhận
   - `debugDrawDanger` - Hiển thị vùng nguy hiểm

## 🐛 Troubleshooting

### ❌ "Assembly-CSharp dll not found"
**Giải pháp:**
- Close Unity → Delete `Library` folder → Mở lại project

### ❌ Scripts không compile
**Giải pháp:**
- Kiểm tra Console (Ctrl+Shift+C)
- Xem error messages
- Đảm bảo Unity version >= 2022.3

### ❌ Scene không load
**Giải pháp:**
- Mở **Assets/Scenes/MainScene** hoặc **GameScene**
- Nếu vẫn lỗi → delete Scene → tạo mới từ prefabs

### ❌ Bot chạy quá nhanh/chậm
**Giải pháp:**
- Vào **Assets/Settings/BotConfig**
- Adjust `thinkInterval` (mặc định 0.12s)
- Hoặc thay đổi cấp độ khó

## 🎯 Tính Năng Sắp Tới

- [ ] Chế độ multiplayer online
- [ ] Thêm items đặc biệt (shield, speed up)
- [ ] Level editor
- [ ] Leaderboard
- [ ] Mobile support (Android/iOS)

## 📄 License

Project này được phát hành dưới license **MIT**. Xem file [LICENSE](LICENSE) để chi tiết.

## 👥 Đội Phát Triển

- **Tác giả**: sonlu3612
- **AI System**: Priority-based State Machine + A* Pathfinding
- **Engine**: Unity 2022.3 LTS+

## 📞 Support & Feedback

- 📧 Issues: [GitHub Issues](https://github.com/sonlu3612/BombIt/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/sonlu3612/BombIt/discussions)

---

**Chúc bạn chơi game vui vẻ! Happy Bombing! 💣**

