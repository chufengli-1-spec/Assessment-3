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
    public float deadSpeed = 1.35f;
    
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
    private const int HISTORY_SIZE = 4;
    
    private PacStudentController pacStudent;
    private LevelGenerator levelGenerator;
    private GameManager gameManager;
    private int originalMapWidth;
    private int originalMapHeight;
    
    // 修正的产卵区边界和出口位置
    private readonly float SPAWN_MIN_X = -10f;
    private readonly float SPAWN_MAX_X = -3f;
    private readonly float SPAWN_MIN_Y = -6f;
    private readonly float SPAWN_MAX_Y = -2f;
    
    // 出口位置
    private readonly Vector3 TOP_EXIT_LEFT = new Vector3(-7f, -2f, 0f);
    private readonly Vector3 TOP_EXIT_RIGHT = new Vector3(-6f, -2f, 0f);
    private readonly Vector3 BOTTOM_EXIT_LEFT = new Vector3(-7f, -6f, 0f);
    private readonly Vector3 BOTTOM_EXIT_RIGHT = new Vector3(-6f, -6f, 0f);
    
    // 出口目标位置（产卵区外）
    private readonly Vector3 TOP_TARGET = new Vector3(-6f, -1f, 0f);  // 上方出口目标
    private readonly Vector3 BOTTOM_TARGET = new Vector3(-6f, -7f, 0f); // 下方出口目标
    
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
        
        // 初始化位置历史
        RecordPosition(currentGridPos);
        
        // 设置初始方向
        if (ghostID == 1 || ghostID == 3)
        {
            currentDirection = Vector3.up; // 向上离开
        }
        else
        {
            currentDirection = Vector3.down; // 向下离开
        }
        lastDirection = -currentDirection;
        
        SetNormal();
        
        Debug.Log($"Ghost {ghostID} initialized at {transform.position}");
    }
    
    void Update()
    {
        if (gameManager != null && !gameManager.IsGameRunning()) return;
        
        if (currentState == GhostState.Dead)
        {
            MoveTowardsSpawn();
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
    
    private void MakeMovementDecision()
    {
        if (isMoving) return;
        
        // 核心逻辑：如果在产卵区内，强制前往出口
        if (IsInSpawnArea(transform.position) && !hasExitedSpawn)
        {
            ForceExitSpawn();
            return;
        }
        
        // 正常移动逻辑
        Vector3[] possibleDirections = GetPossibleDirections();
        
        if (possibleDirections.Length == 0) 
        {
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
        Vector3 targetPosition;
        if (ghostID == 1 || ghostID == 3)
        {
            // 幽灵1和3：前往上方出口目标
            targetPosition = TOP_TARGET;
        }
        else
        {
            // 幽灵2和4：前往下方出口目标
            targetPosition = BOTTOM_TARGET;
        }
        
        // 计算方向
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 gridDirection = RoundToGridDirection(directionToTarget);
        
        // 检查该方向是否可行
        Vector2Int targetGridPos = currentGridPos + WorldToGridDirection(gridDirection);
        if (IsPositionWalkable(targetGridPos))
        {
            lastDirection = currentDirection;
            currentDirection = gridDirection;
            StartMovement(gridDirection);
        }
        else
        {
            // 如果首选方向不行，尝试其他能到达目标的方向
            Vector3[] possibleDirections = GetPossibleDirections();
            Vector3 bestDirection = FindBestDirectionToTarget(possibleDirections, targetPosition);
            
            if (bestDirection != Vector3.zero)
            {
                lastDirection = currentDirection;
                currentDirection = bestDirection;
                StartMovement(bestDirection);
            }
            else
            {
                // 实在找不到路径，使用随机方向
                if (possibleDirections.Length > 0)
                {
                    lastDirection = currentDirection;
                    currentDirection = possibleDirections[0];
                    StartMovement(possibleDirections[0]);
                }
            }
        }
        
        // 检查是否已经离开产卵区
        Vector2Int testPos = currentGridPos + WorldToGridDirection(currentDirection);
        Vector3 testWorldPos = GridToWorldPosition(testPos);
        if (!IsInSpawnArea(testWorldPos))
        {
            hasExitedSpawn = true;
        }
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
    
    private void StartMovement(Vector3 direction)
    {
        Vector2Int directionGrid = WorldToGridDirection(direction);
        targetGridPos = currentGridPos + directionGrid;
        
        if (!IsPositionWalkable(targetGridPos))
        {
            MakeMovementDecision();
            return;
        }
        
        startPosition = GridToWorldPosition(currentGridPos);
        targetPosition = GridToWorldPosition(targetGridPos);
        
        lerpProgress = 0f;
        isMoving = true;
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
            
            // 记录位置历史
            RecordPosition(currentGridPos);
            
            // 检查是否成功离开产卵区
            if (IsInSpawnArea(transform.position))
            {
                hasExitedSpawn = false;
            }
            else
            {
                hasExitedSpawn = true;
            }
        }
    }
    
    private void MoveTowardsSpawn()
    {
        Vector3 spawnCenter = new Vector3((SPAWN_MIN_X + SPAWN_MAX_X) / 2, (SPAWN_MIN_Y + SPAWN_MAX_Y) / 2, 0);
        Vector3 directionToSpawn = (spawnCenter - transform.position).normalized;
        transform.position += directionToSpawn * deadSpeed * Time.deltaTime;
        
        if (IsInSpawnArea(transform.position))
        {
            OnReachedSpawnArea();
        }
    }
    
    private void OnReachedSpawnArea()
    {
        if (currentState == GhostState.Dead)
        {
            ResetToInitialPosition();
            
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
        }
    }
    
    private Vector3[] GetPossibleDirections()
    {
        List<Vector3> directions = new List<Vector3>();
        Vector3[] checkDirections = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        
        // 严格禁用回头：只收集所有非反向的有效方向
        foreach (Vector3 dir in checkDirections)
        {
            if (IsDirectionValid(dir) && dir != -lastDirection)
            {
                directions.Add(dir);
            }
        }
        
        // 如果有非反向方向可用，直接返回
        if (directions.Count > 0)
        {
            return directions.ToArray();
        }
        
        // 只有在完全没有其他方向时，才考虑反向
        Vector3 reverseDirection = -lastDirection;
        if (IsDirectionValid(reverseDirection))
        {
            directions.Add(reverseDirection);
        }
        
        return directions.ToArray();
    }
    
    private bool HasMultipleExits(Vector2Int gridPos, Vector3 incomingDirection)
    {
        Vector3[] checkDirections = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        int validDirections = 0;
        
        foreach (Vector3 dir in checkDirections)
        {
            // 排除回头路（相对于 incomingDirection）
            if (dir != -incomingDirection)
            {
                Vector2Int testGridPos = gridPos + WorldToGridDirection(dir);
                if (IsPositionWalkable(testGridPos))
                {
                    validDirections++;
                    if (validDirections > 1) return true; // 有多个出口
                }
            }
        }
        
        return false; // 只有一个或没有出口
    }
    
    private bool IsDirectionValid(Vector3 direction)
    {
        Vector2Int directionGrid = WorldToGridDirection(direction);
        Vector2Int newGridPos = currentGridPos + directionGrid;
        return IsPositionWalkable(newGridPos);
    }
    
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
        return tile == 0 || tile == 5 || tile == 6 || tile == 8;
    }
    
    private bool IsInSpawnArea(Vector3 position)
    {
        return position.x >= SPAWN_MIN_X && position.x <= SPAWN_MAX_X && 
               position.y >= SPAWN_MIN_Y && position.y <= SPAWN_MAX_Y;
    }
    
    private void HandleNoPossibleDirections()
    {
        // 强制选择一个方向
        Vector3[] allDirections = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        foreach (Vector3 dir in allDirections)
        {
            Vector2Int testGridPos = currentGridPos + WorldToGridDirection(dir);
            if (IsPositionWalkable(testGridPos))
            {
                lastDirection = currentDirection;
                currentDirection = dir;
                StartMovement(dir);
                return;
            }
        }
    }
    
    private Vector3 ChooseDirection(Vector3[] possibleDirections)
    {
        if (pacStudent == null) return GetRandomDirection(possibleDirections);
        
        switch (currentState)
        {
            case GhostState.Scared:
            case GhostState.Recovering:
                return GetGhost1Direction(possibleDirections); // 恐惧状态也用幽灵1行为
            case GhostState.Normal:
                switch (ghostID)
                {
                    case 1: return GetGhost1Direction(possibleDirections); // 幽灵1：选择更远的方向
                    case 2: return GetGhost2Direction(possibleDirections); // 幽灵2：选择更近的方向
                    case 3: return GetRandomDirection(possibleDirections); // 幽灵3：随机
                    case 4: return GetRandomDirection(possibleDirections); // 幽灵4：随机（暂时）
                    default: return GetRandomDirection(possibleDirections);
                }
            default:
                return GetRandomDirection(possibleDirections);
        }
    }
    
    private Vector3 GetGhost1Direction(Vector3[] possibleDirections)
    {
        // 优先选择不会导致震荡的方向
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
        
        // 优先在安全方向中选择
        Vector3[] candidates = safeDirections.Count > 0 ? safeDirections.ToArray() : riskyDirections.ToArray();
        
        if (candidates.Length == 0) return possibleDirections[0];
        
        // 原有的距离计算逻辑
        Vector3 playerPos = pacStudent.transform.position;
        Vector3 currentPos = transform.position;
        
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
        // 优先选择不会导致震荡的方向
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
        
        // 优先在安全方向中选择
        Vector3[] candidates = safeDirections.Count > 0 ? safeDirections.ToArray() : riskyDirections.ToArray();
        
        if (candidates.Length == 0) return possibleDirections[0];
        
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
        // 幽灵3的随机移动也要避免立即回头
        List<Vector3> nonReverseDirections = new List<Vector3>();
        
        foreach (Vector3 direction in possibleDirections)
        {
            if (direction != -lastDirection)
            {
                nonReverseDirections.Add(direction);
            }
        }
        
        // 优先选择非反向方向
        if (nonReverseDirections.Count > 0)
        {
            return nonReverseDirections[Random.Range(0, nonReverseDirections.Count)];
        }
        
        // 只有反向可选时才选择反向
        return possibleDirections[Random.Range(0, possibleDirections.Length)];
    }
    
    // 位置历史记录方法
    private void RecordPosition(Vector2Int gridPos)
    {
        positionHistory.Enqueue(gridPos);
        if (positionHistory.Count > HISTORY_SIZE)
        {
            positionHistory.Dequeue();
        }
    }
    
    private bool IsPositionInHistory(Vector2Int gridPos)
    {
        return positionHistory.Contains(gridPos);
    }
    
    // 其他辅助方法
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
    
    private void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetBool("IsMoving", isMoving || currentState == GhostState.Dead);
        
        Vector3 direction = currentState == GhostState.Dead ? 
            (new Vector3((SPAWN_MIN_X + SPAWN_MAX_X) / 2, (SPAWN_MIN_Y + SPAWN_MAX_Y) / 2, 0) - transform.position).normalized : 
            currentDirection;
            
        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);
    }
    
    public void SetNormal() { currentState = GhostState.Normal; currentSpeed = normalSpeed; UpdateAnimator(); }
    public void SetScared() { currentState = GhostState.Scared; currentSpeed = scaredSpeed; UpdateAnimator(); }
    public void SetRecovering() { currentState = GhostState.Recovering; currentSpeed = recoveringSpeed; UpdateAnimator(); }
    public void SetDead() { currentState = GhostState.Dead; currentSpeed = deadSpeed; UpdateAnimator(); }
    public void Die() { SetDead(); }
    
    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("Normal", currentState == GhostState.Normal);
        animator.SetBool("Scared", currentState == GhostState.Scared);
        animator.SetBool("Recovering", currentState == GhostState.Recovering);
        animator.SetBool("Dead", currentState == GhostState.Dead);
    }
    
    public void ResetToInitialPosition()
    {
        transform.position = initialPosition;
        currentGridPos = WorldToGridPosition(initialPosition);
        hasExitedSpawn = false;
        
        // 清空位置历史
        positionHistory.Clear();
        RecordPosition(currentGridPos);
        
        // 重新设置初始方向
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