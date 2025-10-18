using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GhostState
{
    Normal,
    Scared,
    Recovering,
    Dead
}

public class GhostController : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float normalSpeed = 2.7f;
    public float scaredSpeed = 1.35f;
    public float recoveringSpeed = 1.35f;
    public float deadSpeed = 4.0f;
    
    [Header("Ghost Identity")]
    public int ghostID = 1;
    
    [Header("Animation")]
    public Animator animator;
    
    private GhostState currentState = GhostState.Normal;
    private Vector3 initialPosition;
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 currentDirection;
    private Vector3 lastDirection;
    private float currentSpeed;
    private float lerpProgress = 0f;
    private bool isMoving = false;
    private bool hasExitedSpawn = false;
    
    // 移动历史记录
    private Queue<Vector2Int> positionHistory = new Queue<Vector2Int>();
    private const int HISTORY_SIZE = 2;
    
    // 幽灵4的移动状态
    private Vector3[] clockwiseDirections = { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
    private int currentClockwiseIndex = 0;
    
    // 幽灵4的四个角落目标
    private Vector3[] cornerTargets = {
        new Vector3(-19f, -17f, 0f),  // 左下角
        new Vector3(-19f, 9f, 0f),    // 左上角
        new Vector3(6f, 9f, 0f),      // 右上角
        new Vector3(6f, -17f, 0f)     // 右下角
    };
    private int currentCornerIndex = 0;
    private bool hasReachedFirstCorner = false;
    
    private PacStudentController pacStudent;
    private LevelGenerator levelGenerator;
    private GameManager gameManager;
    private int originalMapWidth;
    private int originalMapHeight;
    
    // 产卵区边界
    private readonly float SPAWN_MIN_X = -10f;
    private readonly float SPAWN_MAX_X = -3f;
    private readonly float SPAWN_MIN_Y = -6f;
    private readonly float SPAWN_MAX_Y = -2f;
    
    public GhostState CurrentState { get { return currentState; } }
    
    void Start()
    {
        initialPosition = transform.position;
        currentSpeed = normalSpeed;
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        levelGenerator = FindObjectOfType<LevelGenerator>();
        pacStudent = FindObjectOfType<PacStudentController>();
        gameManager = FindObjectOfType<GameManager>();
        
        if (levelGenerator != null)
        {
            originalMapWidth = levelGenerator.levelMap.GetLength(1);
            originalMapHeight = levelGenerator.levelMap.GetLength(0);
        }
        
        currentGridPos = WorldToGridPosition(transform.position);
        targetPosition = transform.position;
        
        RecordPosition(currentGridPos);
        
        // 设置初始方向
        if (ghostID == 1 || ghostID == 3)
        {
            currentDirection = Vector3.up;
        }
        else
        {
            currentDirection = Vector3.down;
        }
        lastDirection = -currentDirection;
        
        // 初始化幽灵4
        if (ghostID == 4)
        {
            currentClockwiseIndex = 0;
            currentDirection = clockwiseDirections[currentClockwiseIndex];
            currentCornerIndex = 0;
            hasReachedFirstCorner = false;
        }
        
        SetNormal();
        
        Debug.Log($"Ghost {ghostID} 初始化在位置: {transform.position}, 网格位置: {currentGridPos}");
    }
    
    void Update()
    {
        if (gameManager != null && !gameManager.IsGameRunning()) return;
        
        if (currentState == GhostState.Dead)
        {
            HandleDeadState();
            return;
        }
        
        if (!isMoving)
        {
            MakeMovementDecision();
        }
        else
        {
            ContinueLerping();
        }
        
        UpdateAnimation();
    }
    
    private void HandleDeadState()
    {
        Debug.Log($"Ghost {ghostID} 死亡状态处理中... 当前位置: {transform.position}, 在产卵区: {IsInSpawnArea(transform.position)}");
        
        Vector3 spawnCenter = new Vector3((SPAWN_MIN_X + SPAWN_MAX_X) / 2, (SPAWN_MIN_Y + SPAWN_MAX_Y) / 2, 0);
        float distanceToSpawn = Vector3.Distance(transform.position, spawnCenter);
        
        Debug.Log($"Ghost {ghostID} 距离出生点: {distanceToSpawn}");
        
        if (distanceToSpawn < 0.5f) // 到达出生区
        {
            Debug.Log($"Ghost {ghostID} 到达出生区，准备复活");
            OnReachedSpawnArea();
        }
        else
        {
            // 直接移动回出生区
            Vector3 directionToSpawn = (spawnCenter - transform.position).normalized;
            transform.position += directionToSpawn * deadSpeed * Time.deltaTime;
            Debug.Log($"Ghost {ghostID} 正在返回出生区，方向: {directionToSpawn}");
        }
    }
    
    private void MakeMovementDecision()
    {
        if (isMoving) return;
        
        Debug.Log($"Ghost {ghostID} 做出移动决策，状态: {currentState}, 位置: {transform.position}");
        
        // 如果在产卵区内，强制前往出口
        if (IsInSpawnArea(transform.position) && !hasExitedSpawn)
        {
            Debug.Log($"Ghost {ghostID} 在产卵区内，强制离开");
            ForceExitSpawn();
            return;
        }
        
        // 正常移动逻辑
        Vector3[] possibleDirections = GetPossibleDirections();
        
        if (possibleDirections.Length == 0) 
        {
            Debug.Log($"Ghost {ghostID} 没有可用方向，处理无方向情况");
            HandleNoPossibleDirections();
            return;
        }
        
        Vector3 selectedDirection = ChooseDirection(possibleDirections);
        
        lastDirection = currentDirection;
        currentDirection = selectedDirection;
        StartMovement(selectedDirection);
    }

    private void ForceExitSpawn()
    {
        Debug.Log($"Ghost {ghostID} 强制离开产卵区");

        Vector3 targetPosition;
        if (ghostID == 1 || ghostID == 3)
        {
            targetPosition = new Vector3(-6f, -1f, 0f); // 上方出口
        }
        else
        {
            targetPosition = new Vector3(-6f, -7f, 0f); // 下方出口
        }

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 gridDirection = RoundToGridDirection(directionToTarget);

        Vector2Int targetGridPos = currentGridPos + WorldToGridDirection(gridDirection);

        // 检查目标位置是否可行且不是幽灵出口（除非死亡状态）
        bool isWalkable = currentState == GhostState.Dead ?
            IsPositionWalkableForDeadGhost(targetGridPos) :
            IsPositionWalkable(targetGridPos);

        if (isWalkable && !IsPreviousPosition(targetGridPos))
        {
            lastDirection = currentDirection;
            currentDirection = gridDirection;
            StartMovement(gridDirection);
        }
        else
        {
            Vector3[] possibleDirections = GetPossibleDirections();
            Vector3 bestDirection = FindBestDirectionToTarget(possibleDirections, targetPosition);

            if (bestDirection != Vector3.zero)
            {
                lastDirection = currentDirection;
                currentDirection = bestDirection;
                StartMovement(bestDirection);
            }
            else if (possibleDirections.Length > 0)
            {
                lastDirection = currentDirection;
                currentDirection = possibleDirections[0];
                StartMovement(possibleDirections[0]);
            }
        }

        // 检查是否已经离开产卵区
        Vector2Int testPos = currentGridPos + WorldToGridDirection(currentDirection);
        Vector3 testWorldPos = GridToWorldPosition(testPos);
        if (!IsInSpawnArea(testWorldPos))
        {
            hasExitedSpawn = true;
            Debug.Log($"Ghost {ghostID} 已离开产卵区");
        }
    }

    private bool IsPositionWalkableForDeadGhost(Vector2Int gridPosition)
    {
        Vector2Int coords = MapToOriginalQuadrant(gridPosition);
        
        if (coords.x < 0 || coords.x >= originalMapWidth || 
            coords.y < 0 || coords.y >= originalMapHeight)
        {
            return false;
        }

        int tile = levelGenerator.levelMap[coords.y, coords.x];
        // 死亡状态可以穿过所有路径，包括幽灵出口
        return tile == 0 || tile == 5 || tile == 6 || tile == 8;
    }

    // 死亡状态的专用行走检测（可以穿过所有路径包括幽灵出口）
    private bool IsPositionWalkable(Vector2Int gridPosition)
    {
        if (currentState == GhostState.Dead) return true;
        
        Vector2Int coords = MapToOriginalQuadrant(gridPosition);
        
        if (coords.x < 0 || coords.x >= originalMapWidth || 
            coords.y < 0 || coords.y >= originalMapHeight)
        {
            return false;
        }

        int tile = levelGenerator.levelMap[coords.y, coords.x];
        
        // 非死亡状态下禁止进入幽灵出口 (tile 8)
        if (tile == 8 && currentState != GhostState.Dead)
        {
            // 允许在产卵区内且尚未离开的幽灵通过出口
            if (IsInSpawnArea(GridToWorldPosition(currentGridPos)) && !hasExitedSpawn)
            {
                Debug.Log($"Ghost {ghostID} 允许通过出口离开产卵区 (IsPositionWalkable)");
                return true;
            }
            else
            {
                return false;
            }
        }
        
        return tile == 0 || tile == 5 || tile == 6 || tile == 8;
    }
    
    private void StartMovement(Vector3 direction)
    {
        Vector2Int directionGrid = WorldToGridDirection(direction);
        targetGridPos = currentGridPos + directionGrid;
        
        Debug.Log($"Ghost {ghostID} 开始移动，方向: {direction}, 目标网格: {targetGridPos}");
        
        // 根据状态使用不同的行走检测
        bool isWalkable = currentState == GhostState.Dead ? 
            IsPositionWalkableForDeadGhost(targetGridPos) : 
            IsPositionWalkable(targetGridPos);
        
        if (!isWalkable)
        {
            Debug.Log($"Ghost {ghostID} 目标位置不可行走，重新决策");
            MakeMovementDecision();
            return;
        }
        
        startPosition = GridToWorldPosition(currentGridPos);
        targetPosition = GridToWorldPosition(targetGridPos);
        
        lerpProgress = 0f;
        isMoving = true;
    }

    private Vector3 FindBestDirectionToTarget(Vector3[] possibleDirections, Vector3 target)
    {
        Vector3 bestDirection = Vector3.zero;
        float bestDistance = float.MaxValue;
        
        foreach (Vector3 dir in possibleDirections)
        {
            Vector3 newPos = GridToWorldPosition(currentGridPos + WorldToGridDirection(dir));
            float distance = Vector3.Distance(newPos, target);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestDirection = dir;
            }
        }
        
        return bestDirection;
    }
    
    private void ContinueLerping()
    {
        lerpProgress += Time.deltaTime * currentSpeed;
        if (lerpProgress > 1f) lerpProgress = 1f;

        transform.position = Vector3.Lerp(startPosition, targetPosition, lerpProgress);

        if (lerpProgress >= 1f)
        {
            transform.position = targetPosition;
            currentGridPos = targetGridPos;
            isMoving = false;
            
            RecordPosition(currentGridPos);
            
            // 检查是否在产卵区内
            if (IsInSpawnArea(transform.position))
            {
                hasExitedSpawn = false;
            }
            else
            {
                hasExitedSpawn = true;
            }
            
            // 幽灵4检查是否到达目标角落
            if (ghostID == 4)
            {
                CheckCornerReached();
            }
            
            Debug.Log($"Ghost {ghostID} 移动完成，新位置: {transform.position}, 网格: {currentGridPos}");
        }
    }

    private void CheckCornerReached()
    {
        Vector3 currentPos = transform.position;
        Vector3 currentTarget = cornerTargets[currentCornerIndex];
        
        float distance = Vector3.Distance(currentPos, currentTarget);
        if (distance < 0.5f)
        {
            if (!hasReachedFirstCorner)
            {
                hasReachedFirstCorner = true;
                Debug.Log($"Ghost4 到达第一个角落: {currentTarget}");
            }
            else
            {
                Debug.Log($"Ghost4 到达角落 {currentCornerIndex}: {currentTarget}");
            }
            
            // 移动到下一个角落
            currentCornerIndex = (currentCornerIndex + 1) % cornerTargets.Length;
            Debug.Log($"Ghost4 下一个目标角落: {currentCornerIndex} - {cornerTargets[currentCornerIndex]}");
        }
    }
    
    private void OnReachedSpawnArea()
    {
        if (currentState == GhostState.Dead)
        {
            Debug.Log($"=== Ghost {ghostID} 到达出生区，开始复活过程 ===");
            
            // 首先重置到初始位置
            ResetToInitialPosition();
            
            // 根据游戏状态设置新的状态
            if (gameManager != null && gameManager.IsPowerPillActive)
            {
                if (gameManager.PowerPillTimeRemaining <= 3f)
                {
                    SetRecovering();
                }
                else
                {
                    SetScared();
                }
            }
            else
            {
                SetNormal();
            }
            
            hasExitedSpawn = false;
            
            // 确保动画状态正确更新
            UpdateAnimation();
            
            Debug.Log($"=== Ghost {ghostID} 复活完成，新状态: {currentState} ===");
        }
    }
    
    private Vector3[] GetPossibleDirections()
    {
        List<Vector3> directions = new List<Vector3>();
        Vector3[] checkDirections = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        
        foreach (Vector3 dir in checkDirections)
        {
            Vector2Int testGridPos = currentGridPos + WorldToGridDirection(dir);
            
            if (IsDirectionValid(dir) && !IsPreviousPosition(testGridPos))
            {
                directions.Add(dir);
            }
        }
        
        Debug.Log($"Ghost {ghostID} 可用方向: {directions.Count}");
        return directions.ToArray();
    }
    
    private bool IsPreviousPosition(Vector2Int gridPos)
    {
        if (positionHistory.Count < 1) return false;
        
        Vector2Int[] historyArray = positionHistory.ToArray();
        Vector2Int previousPosition = historyArray[0];
        
        return gridPos == previousPosition;
    }
    
    private bool IsDirectionValid(Vector3 direction)
    {
        Vector2Int directionGrid = WorldToGridDirection(direction);
        Vector2Int newGridPos = currentGridPos + directionGrid;
        
        // 如果是非死亡状态，检查目标位置是否是幽灵出口
        if (currentState != GhostState.Dead)
        {
            Vector2Int coords = MapToOriginalQuadrant(newGridPos);
            if (coords.x >= 0 && coords.x < originalMapWidth && 
                coords.y >= 0 && coords.y < originalMapHeight)
            {
                int tile = levelGenerator.levelMap[coords.y, coords.x];
                if (tile == 8)
                {
                    // 允许在产卵区内且尚未离开的幽灵通过出口
                    if (IsInSpawnArea(transform.position) && !hasExitedSpawn)
                    {
                        Debug.Log($"Ghost {ghostID} 允许通过出口离开产卵区");
                        // 允许通过
                    }
                    else
                    {
                        Debug.Log($"Ghost {ghostID} 禁止进入幽灵出口，状态: {currentState}, 已离开产卵区: {hasExitedSpawn}");
                        return false;
                    }
                }
            }
        }
        
        return IsPositionWalkable(newGridPos);
    }
    
    private bool IsInSpawnArea(Vector3 position)
    {
        return position.x >= SPAWN_MIN_X && position.x <= SPAWN_MAX_X && 
               position.y >= SPAWN_MIN_Y && position.y <= SPAWN_MAX_Y;
    }
    
    private void HandleNoPossibleDirections()
    {
        Vector3[] allDirections = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        List<Vector3> validDirections = new List<Vector3>();
        
        foreach (Vector3 dir in allDirections)
        {
            Vector2Int testGridPos = currentGridPos + WorldToGridDirection(dir);
            if (IsPositionWalkable(testGridPos) && !IsPreviousPosition(testGridPos))
            {
                validDirections.Add(dir);
            }
        }
        
        if (validDirections.Count == 0)
        {
            foreach (Vector3 dir in allDirections)
            {
                Vector2Int testGridPos = currentGridPos + WorldToGridDirection(dir);
                if (IsPositionWalkable(testGridPos))
                {
                    validDirections.Add(dir);
                }
            }
        }
        
        if (validDirections.Count > 0)
        {
            lastDirection = currentDirection;
            currentDirection = validDirections[0];
            StartMovement(validDirections[0]);
        }
    }
    
    private Vector3 ChooseDirection(Vector3[] possibleDirections)
    {
        if (pacStudent == null) return GetRandomDirection(possibleDirections);
        
        switch (currentState)
        {
            case GhostState.Scared:
            case GhostState.Recovering:
                return GetGhost1Direction(possibleDirections);
            case GhostState.Normal:
                switch (ghostID)
                {
                    case 1: return GetGhost1Direction(possibleDirections);
                    case 2: return GetGhost2Direction(possibleDirections);
                    case 3: return GetRandomDirection(possibleDirections);
                    case 4: return GetGhost4ClockwiseDirection(possibleDirections);
                    default: return GetRandomDirection(possibleDirections);
                }
            default:
                return GetRandomDirection(possibleDirections);
        }
    }
    
    private Vector3 GetGhost1Direction(Vector3[] possibleDirections)
    {
        List<Vector3> safeDirections = new List<Vector3>();
        List<Vector3> riskyDirections = new List<Vector3>();
        
        foreach (Vector3 direction in possibleDirections)
        {
            Vector2Int nextGridPos = currentGridPos + WorldToGridDirection(direction);
            if (HasMultipleExits(nextGridPos, direction))
            {
                safeDirections.Add(direction);
            }
            else
            {
                riskyDirections.Add(direction);
            }
        }
        
        Vector3[] candidates = safeDirections.Count > 0 ? safeDirections.ToArray() : riskyDirections.ToArray();
        
        if (candidates.Length == 0) return possibleDirections.Length > 0 ? possibleDirections[0] : Vector3.up;
        
        Vector3 playerPos = pacStudent.transform.position;
        
        Vector3 bestDirection = candidates[0];
        float maxDistance = -1f;
        
        foreach (Vector3 direction in candidates)
        {
            Vector3 newPos = GridToWorldPosition(currentGridPos + WorldToGridDirection(direction));
            float newDistance = Vector3.Distance(newPos, playerPos);
            
            if (newDistance > maxDistance)
            {
                maxDistance = newDistance;
                bestDirection = direction;
            }
        }
        
        return bestDirection;
    }
    
    private Vector3 GetGhost2Direction(Vector3[] possibleDirections)
    {
        List<Vector3> safeDirections = new List<Vector3>();
        List<Vector3> riskyDirections = new List<Vector3>();
        
        foreach (Vector3 direction in possibleDirections)
        {
            Vector2Int nextGridPos = currentGridPos + WorldToGridDirection(direction);
            if (HasMultipleExits(nextGridPos, direction))
            {
                safeDirections.Add(direction);
            }
            else
            {
                riskyDirections.Add(direction);
            }
        }
        
        Vector3[] candidates = safeDirections.Count > 0 ? safeDirections.ToArray() : riskyDirections.ToArray();
        
        if (candidates.Length == 0) return possibleDirections.Length > 0 ? possibleDirections[0] : Vector3.up;
        
        Vector3 playerPos = pacStudent.transform.position;
        
        Vector3 bestDirection = candidates[0];
        float minDistance = float.MaxValue;
        
        foreach (Vector3 direction in candidates)
        {
            Vector3 newPos = GridToWorldPosition(currentGridPos + WorldToGridDirection(direction));
            float newDistance = Vector3.Distance(newPos, playerPos);
            
            if (newDistance < minDistance)
            {
                minDistance = newDistance;
                bestDirection = direction;
            }
        }
        
        return bestDirection;
    }
    
    private Vector3 GetGhost4ClockwiseDirection(Vector3[] possibleDirections)
{
    Vector3 currentPos = transform.position;
    
    // 如果还没有到达第一个角落，强制前往左下角
    if (!hasReachedFirstCorner)
    {
        Debug.Log("Ghost4 前往第一个角落（左下角）");
        Vector3 bottomLeftTarget = new Vector3(-19f, -17f, 0f);
        float distance = Vector3.Distance(currentPos, bottomLeftTarget);
        if (distance > 0.5f)
        {
            return FindDirectionToTarget(possibleDirections, bottomLeftTarget);
        }
        else
        {
            hasReachedFirstCorner = true;
            currentCornerIndex = 1; // 切换到第一阶段
            Debug.Log("Ghost4 到达左下角，开始第一阶段移动");
        }
    }
    
    // 根据当前阶段执行不同的移动逻辑
    return GetExactWallPathDirection(possibleDirections, currentPos);
}

private Vector3 GetExactWallPathDirection(Vector3[] possibleDirections, Vector3 currentPos)
{
    // 精确的外墙路径逻辑
    switch (currentCornerIndex)
    {
        case 1: // 第一阶段：左下角 → 往上走到头 → 往右走到头 → 往上走到(-14,2) → 往左走到头 → 往上走到头到达左上角
            return GetPhase1Direction(possibleDirections, currentPos);
            
        case 2: // 第二阶段：左上角 → 往右走到(-5,5) → 往上走到头 → 往右走到头到达右上角
            return GetPhase2Direction(possibleDirections, currentPos);
            
        case 3: // 第三阶段：右上角 → 往下走到头 → 往左走到头 → 往下走到(1,-10) → 向右走到头 → 向下走到头到达右下角
            return GetPhase3Direction(possibleDirections, currentPos);
            
        case 0: // 第四阶段：右下角 → 往左走到(-8,-13) → 向下走到头 → 向左走到头到达左下角
            return GetPhase4Direction(possibleDirections, currentPos);
            
        default:
            return GetRandomDirection(possibleDirections);
    }
}

private Vector3 GetPhase1Direction(Vector3[] possibleDirections, Vector3 currentPos)
{
    // 第一阶段：左下角 → 往上走到头 → 往右走到头 → 往上走到(-14,2) → 往左走到头 → 往上走到头到达左上角
    
    // 1. 从左下角往上走到头 (y = 9)
    if (currentPos.y < 9f && Mathf.Abs(currentPos.x - (-19f)) < 0.5f)
    {
        Debug.Log("阶段1: 从左下角往上走到头");
        if (ArrayContains(possibleDirections, Vector3.up)) return Vector3.up;
    }
    
    // 2. 往上走到头后，往右走到头 (x = 6)
    if (Mathf.Abs(currentPos.y - 9f) < 0.5f && currentPos.x < 6f)
    {
        Debug.Log("阶段1: 往上走到头后，往右走到头");
        if (ArrayContains(possibleDirections, Vector3.right)) return Vector3.right;
    }
    
    // 3. 往右走到头后，往上走到(-14,2)
    if (Mathf.Abs(currentPos.x - 6f) < 0.5f && currentPos.y > 2f)
    {
        Debug.Log("阶段1: 往右走到头后，往上走到(-14,2)");
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    // 4. 到达(-14,2)后，往左走到头 (x = -19)
    Vector3 targetPos1 = new Vector3(-14f, 2f, 0f);
    if (Vector3.Distance(currentPos, targetPos1) < 0.5f || 
        (Mathf.Abs(currentPos.y - 2f) < 0.5f && currentPos.x > -19f))
    {
        Debug.Log("阶段1: 到达(-14,2)后，往左走到头");
        if (ArrayContains(possibleDirections, Vector3.left)) return Vector3.left;
    }
    
    // 5. 往左走到头后，往上走到头到达左上角 (y = 9)
    if (Mathf.Abs(currentPos.x - (-19f)) < 0.5f && currentPos.y < 9f)
    {
        Debug.Log("阶段1: 往左走到头后，往上走到头到达左上角");
        if (ArrayContains(possibleDirections, Vector3.up)) return Vector3.up;
    }
    
    // 检查是否到达左上角
    Vector3 topLeftTarget = new Vector3(-19f, 9f, 0f);
    if (Vector3.Distance(currentPos, topLeftTarget) < 0.5f)
    {
        currentCornerIndex = 2;
        Debug.Log("Ghost4 到达左上角，开始第二阶段移动");
    }
    
    return GetDirectionToNextPoint(possibleDirections, currentPos, GetPhase1Target(currentPos));
}

private Vector3 GetPhase2Direction(Vector3[] possibleDirections, Vector3 currentPos)
{
    // 第二阶段：左上角 → 往右走到(-5,5) → 往上走到头 → 往右走到头到达右上角
    
    // 1. 从左上角往右走到(-5,5)
    Vector3 targetPos2 = new Vector3(-5f, 5f, 0f);
    if (Vector3.Distance(currentPos, targetPos2) > 0.5f && Mathf.Abs(currentPos.y - 9f) < 0.5f)
    {
        Debug.Log("阶段2: 从左上角往右走到(-5,5)");
        if (ArrayContains(possibleDirections, Vector3.right)) return Vector3.right;
    }
    
    // 2. 到达(-5,5)后，往上走到头 (y = 9)
    if (Vector3.Distance(currentPos, targetPos2) < 0.5f || 
        (Mathf.Abs(currentPos.x - (-5f)) < 0.5f && currentPos.y < 9f))
    {
        Debug.Log("阶段2: 到达(-5,5)后，往上走到头");
        if (ArrayContains(possibleDirections, Vector3.up)) return Vector3.up;
    }
    
    // 3. 往上走到头后，往右走到头到达右上角 (x = 6)
    if (Mathf.Abs(currentPos.y - 9f) < 0.5f && currentPos.x < 6f)
    {
        Debug.Log("阶段2: 往上走到头后，往右走到头到达右上角");
        if (ArrayContains(possibleDirections, Vector3.right)) return Vector3.right;
    }
    
    // 检查是否到达右上角
    Vector3 topRightTarget = new Vector3(6f, 9f, 0f);
    if (Vector3.Distance(currentPos, topRightTarget) < 0.5f)
    {
        currentCornerIndex = 3;
        Debug.Log("Ghost4 到达右上角，开始第三阶段移动");
    }
    
    return GetDirectionToNextPoint(possibleDirections, currentPos, GetPhase2Target(currentPos));
}

private Vector3 GetPhase3Direction(Vector3[] possibleDirections, Vector3 currentPos)
{
    // 第三阶段：右上角 → 往下走到头 → 往左走到头 → 往下走到(1,-10) → 向右走到头 → 向下走到头到达右下角
    
    // 1. 从右上角往下走到头 (y = -17)
    if (currentPos.y > -17f && Mathf.Abs(currentPos.x - 6f) < 0.5f)
    {
        Debug.Log("阶段3: 从右上角往下走到头");
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    // 2. 往下走到头后，往左走到头 (x = -19)
    if (Mathf.Abs(currentPos.y - (-17f)) < 0.5f && currentPos.x > -19f)
    {
        Debug.Log("阶段3: 往下走到头后，往左走到头");
        if (ArrayContains(possibleDirections, Vector3.left)) return Vector3.left;
    }
    
    // 3. 往左走到头后，往下走到(1,-10)
    Vector3 targetPos3 = new Vector3(1f, -10f, 0f);
    if (Mathf.Abs(currentPos.x - (-19f)) < 0.5f && currentPos.y > -10f)
    {
        Debug.Log("阶段3: 往左走到头后，往下走到(1,-10)");
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    // 4. 到达(1,-10)后，向右走到头 (x = 6)
    if (Vector3.Distance(currentPos, targetPos3) < 0.5f || 
        (Mathf.Abs(currentPos.y - (-10f)) < 0.5f && currentPos.x < 6f))
    {
        Debug.Log("阶段3: 到达(1,-10)后，向右走到头");
        if (ArrayContains(possibleDirections, Vector3.right)) return Vector3.right;
    }
    
    // 5. 向右走到头后，向下走到头到达右下角 (y = -17)
    if (Mathf.Abs(currentPos.x - 6f) < 0.5f && currentPos.y > -17f)
    {
        Debug.Log("阶段3: 向右走到头后，向下走到头到达右下角");
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    // 检查是否到达右下角
    Vector3 bottomRightTarget = new Vector3(6f, -17f, 0f);
    if (Vector3.Distance(currentPos, bottomRightTarget) < 0.5f)
    {
        currentCornerIndex = 0;
        Debug.Log("Ghost4 到达右下角，开始第四阶段移动");
    }
    
    return GetDirectionToNextPoint(possibleDirections, currentPos, GetPhase3Target(currentPos));
}

private Vector3 GetPhase4Direction(Vector3[] possibleDirections, Vector3 currentPos)
{
    // 第四阶段：右下角 → 往左走到(-8,-13) → 向下走到头 → 向左走到头到达左下角
    
    // 1. 从右下角往左走到(-8,-13)
    Vector3 targetPos4 = new Vector3(-8f, -13f, 0f);
    if (Vector3.Distance(currentPos, targetPos4) > 0.5f && Mathf.Abs(currentPos.y - (-17f)) < 0.5f)
    {
        Debug.Log("阶段4: 从右下角往左走到(-8,-13)");
        if (ArrayContains(possibleDirections, Vector3.left)) return Vector3.left;
    }
    
    // 2. 到达(-8,-13)后，向下走到头 (y = -17)
    if (Vector3.Distance(currentPos, targetPos4) < 0.5f || 
        (Mathf.Abs(currentPos.x - (-8f)) < 0.5f && currentPos.y > -17f))
    {
        Debug.Log("阶段4: 到达(-8,-13)后，向下走到头");
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    // 3. 向下走到头后，向左走到头到达左下角 (x = -19)
    if (Mathf.Abs(currentPos.y - (-17f)) < 0.5f && currentPos.x > -19f)
    {
        Debug.Log("阶段4: 向下走到头后，向左走到头到达左下角");
        if (ArrayContains(possibleDirections, Vector3.left)) return Vector3.left;
    }
    
    // 检查是否到达左下角
    Vector3 bottomLeftTarget = new Vector3(-19f, -17f, 0f);
    if (Vector3.Distance(currentPos, bottomLeftTarget) < 0.5f)
    {
        currentCornerIndex = 1;
        Debug.Log("Ghost4 到达左下角，开始新的循环");
    }
    
    return GetDirectionToNextPoint(possibleDirections, currentPos, GetPhase4Target(currentPos));
}

// 辅助方法：获取当前阶段的下一个目标点
private Vector3 GetPhase1Target(Vector3 currentPos)
{
    if (currentPos.y < 9f && Mathf.Abs(currentPos.x - (-19f)) < 0.5f) return new Vector3(-19f, 9f, 0f);
    if (Mathf.Abs(currentPos.y - 9f) < 0.5f && currentPos.x < 6f) return new Vector3(6f, 9f, 0f);
    if (Mathf.Abs(currentPos.x - 6f) < 0.5f && currentPos.y > 2f) return new Vector3(6f, 2f, 0f);
    if (Mathf.Abs(currentPos.y - 2f) < 0.5f && currentPos.x > -19f) return new Vector3(-19f, 2f, 0f);
    if (Mathf.Abs(currentPos.x - (-19f)) < 0.5f && currentPos.y < 9f) return new Vector3(-19f, 9f, 0f);
    return new Vector3(-19f, 9f, 0f);
}

private Vector3 GetPhase2Target(Vector3 currentPos)
{
    if (Mathf.Abs(currentPos.y - 9f) < 0.5f && currentPos.x < -5f) return new Vector3(-5f, 9f, 0f);
    if (Mathf.Abs(currentPos.x - (-5f)) < 0.5f && currentPos.y < 9f) return new Vector3(-5f, 9f, 0f);
    if (Mathf.Abs(currentPos.y - 9f) < 0.5f && currentPos.x < 6f) return new Vector3(6f, 9f, 0f);
    return new Vector3(6f, 9f, 0f);
}

private Vector3 GetPhase3Target(Vector3 currentPos)
{
    if (Mathf.Abs(currentPos.x - 6f) < 0.5f && currentPos.y > -17f) return new Vector3(6f, -17f, 0f);
    if (Mathf.Abs(currentPos.y - (-17f)) < 0.5f && currentPos.x > -19f) return new Vector3(-19f, -17f, 0f);
    if (Mathf.Abs(currentPos.x - (-19f)) < 0.5f && currentPos.y > -10f) return new Vector3(-19f, -10f, 0f);
    if (Mathf.Abs(currentPos.y - (-10f)) < 0.5f && currentPos.x < 6f) return new Vector3(6f, -10f, 0f);
    if (Mathf.Abs(currentPos.x - 6f) < 0.5f && currentPos.y > -17f) return new Vector3(6f, -17f, 0f);
    return new Vector3(6f, -17f, 0f);
}

private Vector3 GetPhase4Target(Vector3 currentPos)
{
    if (Mathf.Abs(currentPos.y - (-17f)) < 0.5f && currentPos.x > -8f) return new Vector3(-8f, -17f, 0f);
    if (Mathf.Abs(currentPos.x - (-8f)) < 0.5f && currentPos.y > -17f) return new Vector3(-8f, -17f, 0f);
    if (Mathf.Abs(currentPos.y - (-17f)) < 0.5f && currentPos.x > -19f) return new Vector3(-19f, -17f, 0f);
    return new Vector3(-19f, -17f, 0f);
}

private Vector3 GetDirectionToNextPoint(Vector3[] possibleDirections, Vector3 currentPos, Vector3 target)
{
    Vector3 bestDir = Vector3.zero;
    float bestDistance = float.MaxValue;
    
    foreach (Vector3 dir in possibleDirections)
    {
        Vector3 testPos = GridToWorldPosition(currentGridPos + WorldToGridDirection(dir));
        float distance = Vector3.Distance(testPos, target);
        
        if (distance < bestDistance)
        {
            bestDistance = distance;
            bestDir = dir;
        }
    }
    
    return bestDir != Vector3.zero ? bestDir : GetRandomDirection(possibleDirections);
}

    private Vector3 FindDirectionToTarget(Vector3[] possibleDirections, Vector3 target)
    {
        Vector3 currentPos = transform.position;
        
        Vector3 bestDir = Vector3.zero;
        float bestDistance = float.MaxValue;
        
        foreach (Vector3 dir in possibleDirections)
        {
            Vector3 testPos = GridToWorldPosition(currentGridPos + WorldToGridDirection(dir));
            float distance = Vector3.Distance(testPos, target);
            
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestDir = dir;
            }
        }
        
        Debug.Log($"Ghost4 找到目标方向: {bestDir}, 距离: {bestDistance}");
        return bestDir != Vector3.zero ? bestDir : GetRandomDirection(possibleDirections);
    }

    private bool ArrayContains(Vector3[] array, Vector3 direction)
    {
        foreach (Vector3 dir in array)
        {
            if (dir == direction) return true;
        }
        return false;
    }

    private Vector3 GetRandomDirection(Vector3[] possibleDirections)
    {
        if (possibleDirections.Length > 0)
        {
            return possibleDirections[Random.Range(0, possibleDirections.Length)];
        }
        return Vector3.up;
    }
    
    private bool HasMultipleExits(Vector2Int gridPos, Vector3 incomingDirection)
    {
        Vector3[] checkDirections = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        int validDirections = 0;
        
        foreach (Vector3 dir in checkDirections)
        {
            Vector2Int testGridPos = gridPos + WorldToGridDirection(dir);
            if (IsPositionWalkable(testGridPos))
            {
                validDirections++;
                if (validDirections > 1) return true;
            }
        }
        
        return false;
    }
    
    private void RecordPosition(Vector2Int gridPos)
    {
        positionHistory.Enqueue(gridPos);
        while (positionHistory.Count > HISTORY_SIZE)
        {
            positionHistory.Dequeue();
        }
    }
    
    private Vector2Int MapToOriginalQuadrant(Vector2Int fullPos)
    {
        int x = fullPos.x;
        int y = fullPos.y;
        
        int fullWidth = originalMapWidth * 2;
        int fullHeight = (originalMapHeight * 2) - 2;
        
        if (x < 0 || x >= fullWidth || y < 0 || y >= fullHeight)
            return new Vector2Int(-1, -1);
        
        bool isRight = x >= originalMapWidth;
        bool isBottom = y >= originalMapHeight - 1;
        
        int origX = isRight ? (originalMapWidth - 1) - (x - originalMapWidth) : x;
        int origY = isBottom ? (originalMapHeight - 1) - (y - (originalMapHeight - 1)) : y;
        
        return new Vector2Int(origX, origY);
    }
    
    private Vector3 GridToWorldPosition(Vector2Int grid)
    {
        if (levelGenerator == null) return Vector3.zero;
        float x = levelGenerator.startPosition.x + grid.x * levelGenerator.tileSize;
        float y = levelGenerator.startPosition.y - grid.y * levelGenerator.tileSize;
        return new Vector3(x, y, 0f);
    }
    
    private Vector2Int WorldToGridPosition(Vector3 world)
    {
        if (levelGenerator == null) return Vector2Int.zero;
        int gx = Mathf.RoundToInt((world.x - levelGenerator.startPosition.x) / levelGenerator.tileSize);
        int gy = Mathf.RoundToInt((levelGenerator.startPosition.y - world.y) / levelGenerator.tileSize);
        return new Vector2Int(gx, gy);
    }
    
    private Vector2Int WorldToGridDirection(Vector3 worldDirection)
    {
        if (worldDirection == Vector3.up) return Vector2Int.down;
        if (worldDirection == Vector3.down) return Vector2Int.up;
        if (worldDirection == Vector3.left) return Vector2Int.left;
        if (worldDirection == Vector3.right) return Vector2Int.right;
        return Vector2Int.zero;
    }
    
    private Vector3 RoundToGridDirection(Vector3 direction)
    {
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);
        
        if (absX > absY)
        {
            return new Vector3(Mathf.Sign(direction.x), 0, 0);
        }
        else
        {
            return new Vector3(0, Mathf.Sign(direction.y), 0);
        }
    }
    
    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        // 设置移动状态
        animator.SetBool("IsMoving", isMoving || currentState == GhostState.Dead);
        
        // 设置方向
        Vector3 direction = currentState == GhostState.Dead ? 
            (new Vector3((SPAWN_MIN_X + SPAWN_MAX_X) / 2, (SPAWN_MIN_Y + SPAWN_MAX_Y) / 2, 0) - transform.position).normalized : 
            currentDirection;
            
        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);
        
        // 设置状态参数 - 确保互斥
        animator.SetBool("Normal", currentState == GhostState.Normal);
        animator.SetBool("Scared", currentState == GhostState.Scared);
        animator.SetBool("Recovering", currentState == GhostState.Recovering);
        animator.SetBool("Dead", currentState == GhostState.Dead);
        
        Debug.Log($"Ghost {ghostID} 动画更新 - 状态: {currentState}, 移动: {isMoving}");
    }
    
    public void SetNormal() { 
        currentState = GhostState.Normal; 
        currentSpeed = normalSpeed; 
        Debug.Log($"Ghost {ghostID} 设置为正常状态");
    }
    
    public void SetScared() { 
        currentState = GhostState.Scared; 
        currentSpeed = scaredSpeed; 
        Debug.Log($"Ghost {ghostID} 设置为恐惧状态");
    }
    
    public void SetRecovering() { 
        currentState = GhostState.Recovering; 
        currentSpeed = recoveringSpeed; 
        Debug.Log($"Ghost {ghostID} 设置为恢复状态");
    }
    
    public void SetDead() { 
        currentState = GhostState.Dead; 
        currentSpeed = deadSpeed; 
        
        // 强制设置动画参数
        if (animator != null)
        {
            animator.SetBool("Dead", true);
            animator.SetBool("Normal", false);
            animator.SetBool("Scared", false);
            animator.SetBool("Recovering", false);
            animator.SetBool("IsMoving", true);
        }
        
        Debug.Log($"Ghost {ghostID} 设置为死亡状态");
    }
    
    public void Die() 
    { 
        SetDead(); 
        Debug.Log($"Ghost {ghostID} 死亡");
        
        // 确保立即更新动画
        UpdateAnimation();
    }
    
    public void ResetToInitialPosition()
    {
        Debug.Log($"Ghost {ghostID} 重置到初始位置: {initialPosition}");
        transform.position = initialPosition;
        currentGridPos = WorldToGridPosition(initialPosition);
        hasExitedSpawn = false;
        isMoving = false;
        
        positionHistory.Clear();
        RecordPosition(currentGridPos);
        
        if (ghostID == 1 || ghostID == 3)
        {
            currentDirection = Vector3.up;
        }
        else
        {
            currentDirection = Vector3.down;
        }
        lastDirection = -currentDirection;
        
        // 重置幽灵4的状态
        if (ghostID == 4)
        {
            currentClockwiseIndex = 0;
            currentDirection = clockwiseDirections[currentClockwiseIndex];
            currentCornerIndex = 0;
            hasReachedFirstCorner = false;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && currentState != GhostState.Dead)
        {
            if (currentState == GhostState.Normal)
            {
                if (gameManager != null) gameManager.PacStudentDied();
            }
            else if (currentState == GhostState.Scared || currentState == GhostState.Recovering)
            {
                SetDead();
                if (gameManager != null) gameManager.AddScore(300);
            }
        }
    }
}