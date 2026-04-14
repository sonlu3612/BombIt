# 🤖 Hệ Thống AI của Game BombIt

## 📋 Mục Lục
1. [Tổng Quan](#tổng-quan)
2. [Kiến Trúc Chính](#kiến-trúc-chính)
3. [Hệ Thống State Machine](#hệ-thống-state-machine)
4. [Các State Chính](#các-state-chính)
5. [Hệ Thống Sensing (Cảm Nhận)](#hệ-thống-sensing--cảm-nhận)
6. [Pathfinding A*](#pathfinding-a)
7. [Decision Making (Ra Quyết Định)](#decision-making--ra-quyết-định)
8. [Difficulty Settings](#difficulty-settings)
9. [Debug & Tracing](#debug--tracing)

---

## 🎮 Tổng Quan

Hệ thống AI trong **BombIt** là một **priority-based state machine** kết hợp with **BFS sensing** để tạo ra những con bot cơ động, thông minh. Bot có thể:
- ✅ Chạy trốn bom nổ
- ✅ Trồng bom chiến lược
- ✅ Tấn công địch thủ
- ✅ Trừ dạng block
- ✅ Lượm items
- ✅ Khám phá map khi chưa có mục tiêu

### 📊 Kiến Trúc Cấp Cao

```
┌─────────────────────────────────────────┐
│         BotBrain (Update Loop)          │
│  - Config, debug settings, state holder |
└─────────┬───────────────────────────────┘
          │
    ┌─────▼──────────────────┐
    │  BotSenseBuilder       │
    │  Builds environment    │
    │  context for bot      │
    └─────┬──────────────────┘
          │ BotSenseContext
    ┌─────▼──────────────────────────┐
    │  BotStateMachine               │
    │  - Priority ordering           │
    │  - State transitions           │
    │  - Update current state        │
    └─────┬──────────────────────────┘
          │
    ┌─────▼──────────────────────────┐
    │  Current IBotState             │
    │  - CanEnter, Enter, Tick, Exit │
    └─────┬──────────────────────────┘
          │
    ┌─────┴───────────┬───────────────┐
    │                 │               │
┌───▼───┐    ┌────────▼────┐   ┌─────▼──────┐
│BotNav.│    │BotBlackboard│   │BotActionEx.│
│Pathfinder  │Path/Memory  │   │Movement/Act│
└────────┘    └─────────────┘   └────────────┘
```

---

## 🏗️ Kiến Trúc Chính

### 1. **BotBrain.cs** - Bộ não chính
```csharp
public class BotBrain : MonoBehaviour
{
    private BotConfig config;              // Cấu hình difficulty
    private BotBlackboard blackboard;      // Bộ nhớ/state
    private BotNavigator navigator;        // Pathfinding
    private BotActionExecutor executor;    // Thực thi action
    private BotStateMachine stateMachine;   // State transitions
    
    // Gọi every thinkInterval (mặc định 0.12s)
    private void Update() {
        BotSenseContext sense = BotSenseBuilder.Build(...);
        stateMachine.Update(sense);
    }
}
```

### 2. **BotConfig.cs** - Cấu hình AI
| Tham số | Giá Trị | Mục Đích |
|---------|--------|---------|
| `thinkInterval` | 0.12s | Tần suất cập nhật AI |
| `repathInterval` | 0.15s | Tần suất tính lại đường |
| `findRange` | 8 ô | Phạm vi tìm kiếm |
| `bombCooldown` | 0.35s | Cooldown giữa các bom |
| `plantBombChance` | 95% | Xác suất trồng bom |
| `itemChance` | 85% | Xác suất lượm item |
| `attackChance` | 80% | Xác suất tấn công |
| `breakBlockChance` | 90% | Xác suất phá block |

### 3. **BotBlackboard.cs** - Bộ nhớ bot
Lưu trữ trạng thái tạm thời:
```csharp
public List<Vector3Int> CurrentPath;      // Đường hiện tại
public int CurrentPathIndex;              // Vị trí trên đường
public Vector3Int? CurrentTargetCell;     // Mục tiêu
public Vector3Int? EscapeCell;            // Điểm trốn
public float LastBombTime;                // Lần bom cuối
public float LastThinkTime;               // Lần suy nghĩ cuối
public string LastStateName;              // State cuối cùng
```

---

## 🔄 Hệ Thống State Machine

### 📊 Thứ Tự Ưu Tiên (Priority Order)

Bot kiểm tra các state theo **thứ tự từ trên xuống**. State đầu tiên có `CanEnter() = true` được chọn:

```
1️⃣  EscapeAfterBombState    (Chạy sau khi trồng bom)
    ├─ Điều kiện: Vừa trồng xong bom, đang có trong escape path
    └─ Mục tiêu: Thoát khỏi vị trí bom

2️⃣  EvadeBombState          (Chạy trốn bom nổ)
    ├─ Điều kiện: Đang trong vùng nguy hiểm (IsInDanger)
    └─ Mục tiêu: Tìm safe zone gần nhất

3️⃣  PlantBombState          (Trồng bom)
    ├─ Điều kiện: Không trong nguy hiểm, có escape plan, có mục tiêu
    └─ Mục tiêu: Trồng bom → chạy trốn

4️⃣  GetItemState            (Lượm item)
    ├─ Điều kiện: Không nguy hiểm, 85% chance, có item gần
    └─ Mục tiêu: Đi lươm item

5️⃣  AttackEnemyState        (Tấn công)
    ├─ Điều kiện: 80% chance, có enemy gần, đánh được
    └─ Mục tiêu: Trồng bom tấn công

6️⃣  BreakBlockState         (Phá block)
    ├─ Điều kiện: 90% chance, có block gần
    └─ Mục tiêu: Trồng bom phá block

7️⃣  WanderState             (Khám phá)
    ├─ Điều kiện: Không có mục tiêu khác (fallback)
    └─ Mục tiêu: Chạy random khám phá map
```

### 🔁 Vòng Đời State

```
┌──────────────────┐
│   Idle (null)    │
└────────┬─────────┘
         │ CanEnter(sense) = true
         ▼
┌──────────────────┐
│   Enter(sense)   │◄─── Chuẩn bị kế hoạch
└────────┬─────────┘
         │
┌────────▼──────────────────────┐
│   Tick Loop                    │
│   - Cập nhật vị trí           │
│   - Kiểm tra IsFinished       │
│   - Thực thi movement         │
└────────┬───────────────────────┘
         │
    IsFinished?
    ├─ YES ─▶ Exit() ─▶ Idle
    └─ NO  ─▶ Tiếp tục Tick

┌──────────────────┐
│    Exit()        │◄─── Cleanup
└──────────────────┘
```

### 🎯 State Promotion (Chuyển State)

Nếu một state **cao hơn trong priority** có `CanEnter() = true`:
- ✅ State hiện tại gọi `Exit()` → Save blackboard snapshot
- ✅ State mới gọi `Enter()`
- ✅ Nếu state mới fail → Restore snapshot, quay lại state cũ

**Ví dụ:** Bot đang `GetItemState` (state #4), nhưng bomb nổ → `EvadeBombState` (state #2) được activate

---

## 🎬 Các State Chính

### 1️⃣ **EscapeAfterBombState**
**Mục tiêu:** Thoát khỏi điểm bom vừa trồng

```csharp
public bool CanEnter(BotSenseContext sense)
{
    // Chỉ vào nếu đang trong escape plan của PlantBomb state
    return blackboard.EscapePath != null && 
           !sense.IsInDanger;
}

public void Tick(BotSenseContext sense)
{
    // Theo path trốn troạn đến escape cell
    executor.MoveAlongPath(blackboard.EscapePath);
    
    // Kết thúc khi đạt được escape cell
    if (sense.CurrentCell == blackboard.EscapeCell)
        finished = true;
}
```

### 2️⃣ **EvadeBombState**
**Mục tiêu:** Chạy trốn bom nổ

```csharp
public bool CanEnter(BotSenseContext sense)
{
    return sense.IsInDanger;  // Đơn giản thôi!
}

public void Enter(BotSenseContext sense)
{
    // Tìm tất cả safe cells không phải vị trí hiện tại
    List<Vector3Int> candidates = sense.SafeCells
        .Where(c => c != sense.CurrentCell)
        .ToList();
    
    // Tìm đường tới one of safe cells (shortest path)
    List<Vector3Int> path = navigator.FindShortestPathToAny(
        sense.CurrentCell,
        candidates,
        sense.DangerCells,
        sense.BlockedCells,
        executor.Player
    );
    
    blackboard.SetPath(path);
}

public void Tick(BotSenseContext sense)
{
    if (!sense.IsInDanger)
        finished = true;  // Kết thúc khi an toàn
    
    executor.MoveAlongPath(blackboard.CurrentPath);
}
```

### 3️⃣ **PlantBombState**
**Mục tiêu:** Trồng bom và thoát

```csharp
public bool CanEnter(BotSenseContext sense)
{
    // Kiểm tra điều kiện
    if (sense.IsInDanger) return false;
    if (!executor.Player.CanPlaceBomb) return false;
    if (Random.value > config.plantBombChance) return false;  // 95% chance
    
    // Tính vùng nổ bom
    HashSet<Vector3Int> blastCells = GetBlastCells(
        sense.CurrentCell, 
        executor.Player.BombRangeStat
    );
    
    // Kiểm tra có thể đánh được enemy hay break block
    bool canHitEnemy = sense.EnemyCells.Any(e => blastCells.Contains(e));
    bool canBreakBlock = sense.BreakableBlocks.Any(b => blastCells.Contains(b));
    
    if (!canHitEnemy && !canBreakBlock) return false;
    
    // Tìm escape plan
    return TryBuildEscapePlan(sense.CurrentCell, sense, 
                               out preparedEscapePath, 
                               out preparedEscapeCell);
}

public void Enter(BotSenseContext sense)
{
    blackboard.PlannedBombCell = sense.CurrentCell;
    blackboard.EscapePath = preparedEscapePath;
    blackboard.EscapeCell = preparedEscapeCell;
    
    executor.PlaceBomb();
    blackboard.LastBombTime = Time.time;
    
    finished = true;  // Kết thúc ngay → chuyển sang EscapeAfterBomb
}
```

### 4️⃣ **WanderState**
**Mục tiêu:** Khám phá map khi không có mục tiêu

```csharp
public void Enter(BotSenseContext sense)
{
    // Chọn random cell từ FreeCells
    Vector3Int target = sense.FreeCells[Random.Range(0, sense.FreeCells.Count)];
    
    List<Vector3Int> path = navigator.FindPath(
        sense.CurrentCell,
        target,
        sense.DangerCells,
        sense.BlockedCells,
        executor.Player
    );
    
    blackboard.SetPath(path);
}

public void Tick(BotSenseContext sense)
{
    // Chạy theo path
    executor.MoveAlongPath(blackboard.CurrentPath);
    
    // Kết thúc khi đạt target hoặc không có path
    if (sense.CurrentCell == blackboard.CurrentTargetCell)
        finished = true;
}
```

---

## 👁️ Hệ Thống Sensing (Cảm Nhận)

### 📍 BotSenseContext
```csharp
public class BotSenseContext
{
    // Vị trí
    public Vector3Int CurrentCell;          // Vị trí hiện tại
    public Vector3Int LogicCell;            // Vị trí logic (grid aligned)
    
    // Nguy hiểm
    public HashSet<Vector3Int> DangerCells;     // Vùng bom nổ
    public Dictionary<Vector3Int, float> DangerTimes;  // Bao lâu còn nguy
    public List<Vector3Int> SafeCells;      // Nơi an toàn
    
    // Obstacles
    public HashSet<Vector3Int> BlockedCells;     // Ô bị block
    public List<Vector3Int> BreakableBlocks;     // Block có thể phá
    
    // Mục tiêu
    public List<Vector3Int> FreeCells;      // Ô có thể đi được (BFS)
    public List<Vector3Int> ItemCells;      // Vị trí items
    public List<Vector3Int> EnemyCells;     // Vị trí enemy
    
    // Refs
    public List<PlayerController> EnemyPlayers;
    public List<BombController> ActiveBombs;
    
    public bool IsInDanger => DangerCells.Contains(CurrentCell);
}
```

### 🔍 BotSenseBuilder - Xây dựng Sense Context

**1. Tìm Enemies**
```csharp
FindObjectsOfType<PlayerController>()
    → Filter ra những ai không phải bot đang tính
    → Lưu vào sense.EnemyPlayers & sense.EnemyCells
```

**2. Tìm Bombs & Danger**
```csharp
FindObjectsOfType<BombController>()
    
    // Nếu chưa nổ
    → Thêm vào ActiveBombs
    → Tính vùng blast (range cells)
    → Thêm vào DangerCells + DangerTimes
    
    // Nếu đã nổ + còn trong hazard window (0.1s)
    → Vẫn coi là nguy hiểm!
```

**3. Build Reachable Cells (BFS)**
```
Từ CurrentCell, BFS ra tối đa findRange ô:
┌─────────────────────────────┐
│  Bot ở đây (start)          │
└───┬─────────────────────┬───┘
    │                     │
┌───▼──┐             ┌───▼──┐
│ Can  │             │Hazard│
│ Walk │             │Zone  │
└──────┘             └──────┘

FreeCells = Tất cả ô có thể đi
BreakableBlocks = Block phía cạnh các ô có thể đi
```

**4. Build Safe Cells**
```csharp
SafeCells = FreeCells mà NOT IN DangerCells
            = Những ô an toàn hiện tại
```

### 🎰 Tính Danger Time

```csharp
// Mỗi bomb có:
// - explodeTime: khi nào nổ
// - postExplosionDamageDelay: 0.1s (vẫn nguy hiểm sau nổ)

float hazardEndTime = bomb.explodeTime + postExplosionDamageDelay;
sense.DangerTimes[cellInBlast] = hazardEndTime - Time.time;

// Kiểm tra nguy hiểm:
bool isDangerous = Time.time < hazardEndTime;
```

---

## 🛣️ Pathfinding A*

### 📐 BotNavigator - A* Algorithm

```csharp
public List<Vector3Int> FindPath(
    Vector3Int start,
    Vector3Int target,
    HashSet<Vector3Int> dangerCells = null,
    HashSet<Vector3Int> blockedCells = null,
    PlayerController ignorePlayer = null
)
```

**Heuristic Function - Manhattan Distance**
```csharp
private int Heuristic(Vector3Int a, Vector3Int b)
{
    return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}
```

**Reject Conditions** (không đi vào ô này):
1. ❌ **Solid Obstacles**: Walls (bức tường)
2. ❌ **Blocked Cells**: Đã có bot khác đứng
3. ❌ **Danger Cells**: Vùng bom nổ (nếu avoidDangerCells=true)

**Path Quality Metrics**
```
visited:          Số ô đã check
rejected-solid:   Walls/obstacles
rejected-blocked: Occupied by other bots
rejected-danger:  Bomb blast zones
path-length:      Tổng số bước
```

### 📊 Multiple Target Pathfinding

```csharp
// Tìm đường tới ONE OF nhiều targets (shortest)
public List<Vector3Int> FindShortestPathToAny(
    Vector3Int start,
    List<Vector3Int> targets,
    HashSet<Vector3Int> dangerCells,
    HashSet<Vector3Int> blockedCells,
    PlayerController ignorePlayer
)

// Ví dụ: EvadeBomb tìm safe cell gần nhất
List<Vector3Int> safeCells = sense.SafeCells;
List<Vector3Int> path = navigator.FindShortestPathToAny(
    sense.CurrentCell,
    safeCells,  // ← Nhiều target
    sense.DangerCells,
    sense.BlockedCells,
    executor.Player
);
```

### ⚠️ Pathfinding Failures

**Khi FindPath trả về null/empty:**
```
→ State kết thúc (IsFinished = true)
→ State machine chọn state khác
→ Thường fall back to WanderState
```

**Nguyên nhân:**
- Không có đường tới target
- Target trong vùng nguy hiểm không thoát được
- Map bị fragment (các ô không kết nối)

---

## 🧠 Decision Making (Ra Quyết Định)

### 📊 Chance-Based Decisions

Khi có nhiều mục tiêu, bot chọn random dựa vào config:

```csharp
// Ví dụ PlantBombState
if (Random.value > config.plantBombChance)  // 95% chance
    return false;  // Không trồng bom

// Chance-BasedDecision = Real-world uncertainty
// vs deterministic AI = quá dễ đoán
```

### 🎯 Target Priority

States được check theo **priority order** (không random):
1. **Highest Priority**: EscapeAfterBomb (safety first)
2. **Lowest Priority**: WanderState (fallback)

### 🔄 Tái Đánh Giá Liên Tục

Mỗi `thinkInterval` (0.12s):
```
1. Rebuild sense context (enemies, bombs, reachable cells)
2. Check ALL states CanEnter()
3. Promote higher priority state nếu có
4. Tính lại path mỗi repathInterval (0.15s)
```

**Ví dụ Timeline:**
```
0.00s - Bot chọn GetItemState → đi lượm item
0.12s - Think lại → still GetItemState
0.24s - Think lại → Bomb nổ gần → Evade (promote)
0.36s - Think lại → Thoát danger → quay lại GetItemState
```

---

## 🎮 Difficulty Settings

### 📊 3 Mức Difficulty

Được setup ở **GameFlowConfig → PlayerSpawner → BotBrain**

```csharp
// Easy: Dễ bị đánh
BotConfig easy = new() {
    thinkInterval = 0.25f,      // Chậm hơn (chỉ think mỗi 250ms)
    plantBombChance = 0.70f,    // Ít trồng bom (70%)
    itemChance = 0.70f,         // Ít lượm item
    attackChance = 0.60f,       // Ít tấn công
};

// Normal: Cân bằng
BotConfig normal = new() {
    thinkInterval = 0.12f,
    plantBombChance = 0.95f,
    itemChance = 0.85f,
    attackChance = 0.80f,
};

// Hard: Khó đánh
BotConfig hard = new() {
    thinkInterval = 0.08f,      // Nhanh hơn (chỉ think mỗi 80ms)
    bombCooldown = 0.25f,       // Cooldown ngắn hơn
    plantBombChance = 0.98f,    // Hay trồng bom
    attackChance = 0.95f,       // Hay tấn công
    breakBlockChance = 0.95f,   // Hay phá block
};
```

### 🔗 Config Flow

```
GameFlowConfig (Prefab with difficulty choice)
    ↓ get BotConfig based on difficulty
    ↓
PlayerSpawner.SpawnBots(BotConfig config)
    ↓
BotBrain.SetConfig(config)
    ↓ Use in state decisions
```

---

## 🐛 Debug & Tracing

### 📊 Debug Draw Settings (BotBrain Inspector)

```csharp
[SerializeField] private bool debugDrawAI = false;         // Paths
[SerializeField] private bool debugDrawSense = false;      // Sense data
[SerializeField] private bool debugDrawPath = false;       // Current path
[SerializeField] private bool debugDrawSearch = false;     // PathFind cells
[SerializeField] private bool debugDrawBlocked = false;    // Blocked map
[SerializeField] private bool debugDrawDanger = false;     // Danger zones
[SerializeField] private bool debugDrawSafeCells = false;  // Safe cells
[SerializeField] private bool debugDrawVisitedCells = false;  // Visited
```

### 📝 Logging Sources

Tất cả comment out để tối ưu performance, nhưng có thể bật:

```csharp
// BotBrain.cs - State changes
BotRuntimeDebugLog.LogBotStateChange(
    playerController,
    previousState,
    nextState,
    currentCell
);

// BotBrain.cs - Thinking process
BotRuntimeDebugLog.LogBotThink(
    playerController,
    sense,
    blackboard,
    currentStateName
);

// BotNavigator.cs - Path finding
BotMovementTraceLog.LogPathSummary(
    "found/failed",
    start,
    target,
    mapContext,
    visitedCount,
    rejectedSolidCount,
    rejectedBlockedCount,
    rejectedDangerCount,
    pathLength
);

// BotBrain.cs - Path details
BotRuntimeDebugLog.LogBotPath(
    playerController,
    stateName,
    path,
    pathIndex,
    targetCell,
    escapeCell
);
```

### 🎯 Gizmos Visualization

**Red Box** = Danger zone  
**Yellow Box** = Safe zone  
**Green Line** = Current path  
**Cyan Box** = Pathfind working area  
**Blue = Visited cells**  
**Gray = Blocked cells**

---

## 🔧 Troubleshooting

### ❌ Bot bị stuck/đứng yên

**Nguyên nhân:**
1. Map bị fragment (có ô bị cách ly)
2. Pathfinding không tìm được đường
3. Sense.FreeCells không cập nhật

**Fix:**
```csharp
// BotSenseBuilder - kiểm tra isTraversable()
// GetItemState - verify target in sense.FreeCells
// WanderState - fallback khi không có mục tiêu
```

### ❌ Bot không trốn bom

**Nguyên nhân:**
1. Không tính SafeCells đúng
2. DangerCells không cập nhật damage delay
3. EvadeBombState không activate

**Fix:**
```csharp
// BombController - postExplosionDamageDelay = 0.1f
// BotSenseBuilder - FilterCondition check IsExplosionHazardActive
// BotSenseBuilder.BuildSafeCells() = FreeCells - DangerCells
```

### ❌ Bot đứng yên khi trồng bom

**Nguyên nhân:**
1. PlantBombState không tìm được escape plan
2. EscapeAfterBombState không activate

**Fix:**
```csharp
// PlantBombState.TryBuildEscapePlan()
// → Tìm escape cell từ safe cells trong tầm detection
// → Build backup plan nếu kế hoạch fail
```

### ⚠️ Performance Issues

**Optimization Tips:**
1. Increase `thinkInterval` (default 0.12s)
2. Decrease `findRange` (default 8)
3. Disable debug drawing in production
4. Comment out logging calls

---

## 📚 Class References

| Class | Mục đích |
|-------|---------|
| **BotBrain** | Entry point, update loop, config holder |
| **BotConfig** | Difficulty settings, decision chances |
| **BotStateMachine** | State transitions, priority ordering |
| **BotBlackboard** | Memory, path storage, state tracking |
| **BotSenseBuilder** | Build sense context từ environment |
| **BotSenseContext** | Sensing data (enemies, bombs, safe cells) |
| **BotNavigator** | A* pathfinding algorithm |
| **BotActionExecutor** | Movement execution, bomb placement |
| **IBotState** | State interface |
| **BotGridUtility** | Grid helpers (blast cells, directions) |

---

## 🎬 Example: Bot Lifecycle

```
1️⃣  Spawn: Bot spawned with BotBrain + config
    └─ Blackboard initialized

2️⃣  Think (0.12s interval):
    └─ Sense: enemies, bombs, reachable cells
    └─ Evaluate: all states CanEnter()
    └─ Update: state machine with sense data

3️⃣  State: WanderState tích hoạt
    └─ Find random target in FreeCells
    └─ BFS pathfinding to target
    └─ Start moving

4️⃣  Bomb placed nearby (0.3s later):
    └─ DangerCells updated
    └─ IsInDanger = true
    └─ State: Evade promoted over Wander

5️⃣  Evade: Follow safe path away from bomb
    └─ IsInDanger = false
    └─ Evade finished → retry state machine

6️⃣  Back to WanderState / or new objective
    └─ Loop continues...
```

---

## 🎓 Conclusion

Hệ thống AI của BombIt uses:
- ✅ **Priority-based State Machine** cho behavior hierarchy
- ✅ **BFS Sensing** cho environmental awareness
- ✅ **A* Pathfinding** cho intelligent movement
- ✅ **Chance-based Decisions** cho AI unpredictability
- ✅ **Configurable Difficulty** cho game balance

**Kết quả:** Bot có vẻ thông minh, đáp ứng nhanh, và khó đoán được kỳ vọng.
