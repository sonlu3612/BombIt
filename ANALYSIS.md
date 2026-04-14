# Bot Stuck Analysis

## Problem Summary
Bot bị kẹt không di chuyển khi map bị chia cô lập sau khi bom phá hủy các khối.

## Root Cause

### 1. Map Destruction
- Lúc 23:50:02: Bom tại (-4,-5,0) phá hủy 2 khối destructible
- 2 khối này là **junction points** kết nối khu vực (2,3,0)-(2,1,0) với phần còn lại của map
- Khu vực (2,3,0)-(2,1,0) trở nên **cô lập hoàn toàn**

### 2. Pathfinding Failure
```
Before destruction (23:50:01):
[PATH] phase=found start=(2,3,0) target=(2,3,0) ... visited=8 solid=5 danger=0 pathCount=5

After destruction (23:50:02):
[PATH] phase=failed start=(2,3,0) target=(2,3,0) ... visited=17 solid=28 blocked=0 danger=0 pathCount=0
```

- BFS correctly identifies: **Không có đường đi** (visited=17 cells before hitting walls)
- solid increased from 26 → 28 (2 destroyed blocks now block current path)

### 3. State Machine Failure

**GetItemState.cs** (Line 46-54):
```csharp
public void Enter(BotSenseContext sense)
{
    // ...
    List<Vector3Int> path = navigator.FindShortestPathToAny(
        sense.CurrentCell,
        candidates,
        // ...
    );

    if (path == null || path.Count == 0)  // ← If no path found
    {
        executor.Stop();
        finished = true;
        return;  // ← State exits immediately, no fallback!
    }
}
```

**BreakBlockState.cs** (Line 90-98):
```csharp
else if (!TryBuildApproachPlan(
    sense,
    out targetBlockCell,
    out bombCell,
    out approachPath,
    out plannedEscapePath))
{
    executor.Stop();
    finished = true;  // ← Same issue!
    return;
}
```

Both states have **IDENTICAL BUG**: When pathfinding fails, they immediately exit without fallback strategy.

## Evidence from Log

**23:50:03.665-23:50:03.813** (Multiple pathfinding failures):
```
[PATH] phase=failed start=(2,3,0) target=(3,-5,0) visited=17 solid=24 blocked=0 danger=0 pathCount=0
[PATH] phase=failed start=(2,3,0) target=(3,-7,0) visited=17 solid=24 blocked=0 danger=0 pathCount=0
[PATH] phase=failed start=(2,3,0) target=(4,-6,0) visited=17 solid=24 blocked=0 danger=0 pathCount=0
... (many more failed attempts)
```

But:
```
[PATH] phase=found start=(2,3,0) target=(2,1,0) pathCount=3  ← CAN reach (2,1,0)!
```

This proves: Bot CAN move within isolated area but CANNOT reach items/blocks outside.

## Why Bot Stands Still

1. Bot enters area (2,3,0)-(2,1,0) before destruction
2. Bom destroys 2 junction blocks (23:50:02)
3. Bot tries GetItemState → pathfinding fails → exits
4. Bot tries BreakBlockState → pathfinding fails → exits
5. Bot falls back to WanderState but can only move within 17 reachable cells
6. All items/blocks are now unreachable → nothing to do
7. **Bot loops idle** with no meaningful state transitions

## Solution Options

1. **Reachability Check**: Only target items/blocks that are actually reachable
2. **Fallback Strategy**: When stuck, attempt to:
   - Find ANY reachable objective (not just items/blocks in view)
   - Break nearby destructible blocks to create new paths
   - Move to edge of known area for exploration
3. **Periodic Replanning**: Every N frames, rebuild world model and update reachability
4. **Bidirectional Search**: Check if items can reach bot's area (symmetry check)

## Files Involved
- `GetItemState.cs` - Line 46-54: Bad exit logic
- `BreakBlockState.cs` - Line 90-98: Bad exit logic
- `BotNavigator.cs` - Correct: Returns null when no path found, this is expected
- `GridOccupancyService.cs` - Correctly tracks block status changes

## Confirmed: Bot DOES try to move to blocks/items
- BreakBlockState builds approach path via `navigator.FindPath()`
- GetItemState finds path to items via `navigator.FindShortestPathToAny()`
- Both use `executor.FollowPath()` to execute movement
- **Problem: No path = immediate state exit = no fallback**
