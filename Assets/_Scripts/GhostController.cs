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
            
            Debug.Log($"Ghost {ghostID} 移动完成，新位置: {transform.position}, 网格: {currentGridPos}");
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
                    case 4: return GetRandomDirection(possibleDirections);
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