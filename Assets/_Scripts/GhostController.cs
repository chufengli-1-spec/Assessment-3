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
    
    [Header("Movement")]
    public float lerpTime = 0.3f;
    public LayerMask wallLayer;
    
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
    private bool isInSpawnArea = true;
    private bool hasExitedSpawn = false;
    
    private PacStudentController pacStudent;
    private LevelGenerator levelGenerator;
    private GameManager gameManager;
    private int originalMapWidth;
    private int originalMapHeight;
    
    // 产卵区边界
    private readonly Vector3 SPAWN_CENTER = new Vector3(-13f, -6f, 0f);
    private const float SPAWN_RADIUS = 3f;
    
    private readonly Vector3 LEFT_TELEPORTER_POS = new Vector3(-20f, -4f, 0f);
    private readonly Vector3 RIGHT_TELEPORTER_POS = new Vector3(7f, -4f, 0f);
    
    private const string IS_MOVING = "IsMoving";
    private const string MOVE_X = "MoveX";
    private const string MOVE_Y = "MoveY";
    
    // 防止原地摆动
    private int consecutiveSameDirectionMoves = 0;
    private Vector3 lastMoveDirection = Vector3.zero;
    
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
        
        SetNormal();
        
        // 强制设置初始出口方向
        ForceInitialExitDirection();
        
        Debug.Log($"=== GHOST {ghostID} INITIALIZATION ===");
        Debug.Log($"Ghost {ghostID} started at: {transform.position}");
        Debug.Log($"Ghost {ghostID} grid position: {currentGridPos}");
        Debug.Log($"Ghost {ghostID} initial direction: {currentDirection}");
        Debug.Log($"Ghost {ghostID} is in spawn area: {isInSpawnArea}");
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
        
        // 持续检查是否在产卵区内
        CheckSpawnAreaStatus();
    }
    
    private void CheckSpawnAreaStatus()
    {
        bool wasInSpawnArea = isInSpawnArea;
        isInSpawnArea = IsInSpawnArea(transform.position);
        
        if (wasInSpawnArea != isInSpawnArea)
        {
            Debug.Log($"Ghost {ghostID} spawn area status changed: {wasInSpawnArea} -> {isInSpawnArea}");
        }
    }
    
    private void ForceInitialExitDirection()
    {
        // 强制设置初始出口方向
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
    
    private void MakeMovementDecision()
{
    // 如果已经离开产卵区，使用正常移动逻辑
    if (hasExitedSpawn)
    {
        // 正常的幽灵移动决策逻辑
        List<Vector3> possibleDirections = GetPossibleDirections();
        
        if (possibleDirections.Count > 0)
        {
            // 选择方向（可以根据幽灵AI类型进行智能选择）
            Vector3 chosenDirection = ChooseDirection(possibleDirections);
            Vector3 targetWorldPos = transform.position + chosenDirection;
            StartMovement(targetWorldPos);
        }
        return;
    }
    
    // 如果在产卵区内且没有离开过，强制离开
    if (IsInSpawnArea(transform.position) && !hasExitedSpawn)
    {
        ForceExitSpawn();
    }
}
    
    private void ForceExitSpawn()
{
    Debug.Log($"=== GHOST {ghostID} FORCING EXIT ===");
    
    // 尝试直接向下移动离开产卵区
    Vector3 exitDirection = Vector3.down;
    Vector3 targetWorldPos = transform.position + exitDirection;
    Vector2Int targetGridPos = WorldToGridPosition(targetWorldPos);
    
    Debug.Log($"Ghost {ghostID} trying to exit with direction: {exitDirection}");
    Debug.Log($"Ghost {ghostID} exit grid position: {targetGridPos}");
    Debug.Log($"Ghost {ghostID} exit world position: {targetWorldPos}");
    
    // 检查目标位置是否可行走
    bool isWalkable = IsPositionWalkable(targetGridPos);
    Debug.Log($"Ghost {ghostID} exit position walkable: {isWalkable}");
    Debug.Log($"Ghost {ghostID} exit position in spawn: {IsInSpawnArea(targetWorldPos)}");
    
    if (isWalkable && !IsInSpawnArea(targetWorldPos))
    {
        // 直接移动到出口位置
        StartMovement(targetWorldPos);
        hasExitedSpawn = true;  // 关键：标记为已离开产卵区
        Debug.Log($"=== GHOST {ghostID} SUCCESSFULLY EXITED SPAWN AREA ===");
        return;
    }
    
    // 如果直接方向不行，尝试其他方向
    List<Vector3> possibleDirections = GetPossibleDirections();
    Debug.Log($"Ghost {ghostID} alternative directions count: {possibleDirections.Count}");
    
    foreach (Vector3 direction in possibleDirections)
    {
        targetWorldPos = transform.position + direction;
        targetGridPos = WorldToGridPosition(targetWorldPos);
        
        if (IsPositionWalkable(targetGridPos) && !IsInSpawnArea(targetWorldPos))
        {
            Debug.Log($"Ghost {ghostID} using alternative direction: {direction}");
            StartMovement(targetWorldPos);
            hasExitedSpawn = true;  // 关键：标记为已离开产卵区
            Debug.Log($"=== GHOST {ghostID} SUCCESSFULLY EXITED SPAWN AREA ===");
            return;
        }
    }
    
    Debug.Log($"Ghost {ghostID} failed to find exit path");
}
    
    private Vector3 GetDifferentDirection(Vector3[] possibleDirections, Vector3 currentDir)
    {
        if (possibleDirections.Length <= 1) return currentDir;
        
        List<Vector3> otherDirections = new List<Vector3>();
        foreach (Vector3 dir in possibleDirections)
        {
            if (dir != currentDir)
            {
                otherDirections.Add(dir);
            }
        }
        
        return otherDirections.Count > 0 ? 
            otherDirections[Random.Range(0, otherDirections.Count)] : 
            currentDir;
    }
    
    private void HandleNoPossibleDirections()
    {
        Debug.LogError($"Ghost {ghostID} has NO possible directions at {transform.position}");
        
        Vector3[] allDirections = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        List<Vector3> forcedDirections = new List<Vector3>();
        
        foreach (Vector3 dir in allDirections)
        {
            Vector2Int testGridPos = currentGridPos + WorldToGridDirection(dir);
            if (IsPositionWalkableBasic(testGridPos))
            {
                forcedDirections.Add(dir);
            }
        }
        
        if (forcedDirections.Count > 0)
        {
            currentDirection = forcedDirections[Random.Range(0, forcedDirections.Count)];
            Debug.Log($"Ghost {ghostID} forced direction: {currentDirection}");
            StartMovement(currentDirection);
        }
        else
        {
            Debug.LogError($"Ghost {ghostID} completely stuck at {transform.position}!");
        }
    }
    
    private Vector3[] FilterOutSpawnAreaDirections(Vector3[] directions)
    {
        List<Vector3> filtered = new List<Vector3>();
        
        foreach (Vector3 dir in directions)
        {
            Vector2Int testGridPos = currentGridPos + WorldToGridDirection(dir);
            Vector3 testWorldPos = GridToWorldPosition(testGridPos);
            
            if (!IsInSpawnArea(testWorldPos))
            {
                filtered.Add(dir);
            }
        }
        
        return filtered.ToArray();
    }
    
    private void StartMovement(Vector3 direction)
    {
        Vector2Int directionGrid = WorldToGridDirection(direction);
        targetGridPos = currentGridPos + directionGrid;
        
        Debug.Log($"Ghost {ghostID} attempting move: {currentGridPos} -> {targetGridPos} ({direction})");
        
        if (!IsPositionWalkable(targetGridPos))
        {
            Debug.LogWarning($"Ghost {ghostID} target position not walkable: {targetGridPos}");
            MakeMovementDecision();
            return;
        }
        
        startPosition = GridToWorldPosition(currentGridPos);
        targetPosition = GridToWorldPosition(targetGridPos);
        
        lerpProgress = 0f;
        isMoving = true;
        
        UpdateAnimationDirection();
        
        Debug.Log($"Ghost {ghostID} started moving to {targetPosition}");
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
            Debug.Log($"Ghost {ghostID} completed move to {targetPosition}");
        }
    }
    
    private void MoveTowardsSpawn()
    {
        Vector3 directionToSpawn = (SPAWN_CENTER - transform.position).normalized;
        transform.position += directionToSpawn * deadSpeed * Time.deltaTime;
        
        if (Vector3.Distance(transform.position, SPAWN_CENTER) < 0.1f)
        {
            OnReachedSpawnArea();
        }
        
        UpdateAnimationDirection();
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
            
            isInSpawnArea = true;
            hasExitedSpawn = false;
            ForceInitialExitDirection();
        }
    }
    
    private Vector3[] GetPossibleDirections()
    {
        List<Vector3> directions = new List<Vector3>();
        Vector3[] checkDirections = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        
        foreach (Vector3 dir in checkDirections)
        {
            if (IsDirectionValid(dir) && dir != -lastDirection)
            {
                directions.Add(dir);
            }
        }
        
        if (directions.Count == 0)
        {
            foreach (Vector3 dir in checkDirections)
            {
                if (IsDirectionValid(dir))
                {
                    directions.Add(dir);
                    break;
                }
            }
        }
        
        Debug.Log($"Ghost {ghostID} possible directions: {directions.Count}");
        foreach (var dir in directions)
        {
            Debug.Log($"  - {dir}");
        }
        
        return directions.ToArray();
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
        
        Vector3 worldPos = GridToWorldPosition(gridPosition);
        
        if (Vector3.Distance(worldPos, LEFT_TELEPORTER_POS) < 0.5f || 
            Vector3.Distance(worldPos, RIGHT_TELEPORTER_POS) < 0.5f)
        {
            return false;
        }
        
        Vector2Int coords = MapToOriginalQuadrant(gridPosition);
        
        if (coords.x < 0 || coords.x >= originalMapWidth || 
            coords.y < 0 || coords.y >= originalMapHeight)
        {
            return false;
        }

        int tile = levelGenerator.levelMap[coords.y, coords.x];
        
        bool isWalkable = tile == 0 || tile == 5 || tile == 6 || tile == 8;
        
        if (!isWalkable)
        {
            Debug.Log($"Ghost {ghostID} position not walkable: tile {tile} at {coords}");
            return false;
        }
        
        if (!isInSpawnArea && currentState != GhostState.Dead)
        {
            if (IsInSpawnArea(worldPos))
            {
                return false;
            }
        }
        
        return true;
    }
    
    private bool IsPositionWalkableBasic(Vector2Int gridPosition)
    {
        Vector3 worldPos = GridToWorldPosition(gridPosition);
        
        if (Vector3.Distance(worldPos, LEFT_TELEPORTER_POS) < 0.5f || 
            Vector3.Distance(worldPos, RIGHT_TELEPORTER_POS) < 0.5f)
        {
            return false;
        }
        
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
        float spawnMinX = -15f;
        float spawnMaxX = -11f;
        float spawnMinY = -8f;
        float spawnMaxY = -4f;
        
        bool inSpawn = position.x >= spawnMinX && position.x <= spawnMaxX && 
                       position.y >= spawnMinY && position.y <= spawnMaxY;
        
        return inSpawn;
    }
    
    // 其他方法保持不变...
    private Vector3 GetNormalStateDirection(Vector3[] possibleDirections)
    {
        if (pacStudent == null) return GetRandomDirection(possibleDirections);
        
        switch (ghostID)
        {
            case 1:
                return GetGhost1Direction(possibleDirections);
            case 2:
                return GetGhost2Direction(possibleDirections);
            case 3:
                return GetRandomDirection(possibleDirections);
            case 4:
                return GetGhost4Direction(possibleDirections);
            default:
                return GetRandomDirection(possibleDirections);
        }
    }
    
    private Vector3 GetGhost1Direction(Vector3[] possibleDirections)
    {
        float currentDistance = Vector3.Distance(transform.position, pacStudent.transform.position);
        List<Vector3> validDirections = new List<Vector3>();
        
        foreach (Vector3 direction in possibleDirections)
        {
            Vector3 newPos = GridToWorldPosition(currentGridPos + WorldToGridDirection(direction));
            float newDistance = Vector3.Distance(newPos, pacStudent.transform.position);
            if (newDistance >= currentDistance)
            {
                validDirections.Add(direction);
            }
        }
        
        return validDirections.Count > 0 ? 
            validDirections[Random.Range(0, validDirections.Count)] : 
            GetRandomDirection(possibleDirections);
    }
    
    private Vector3 GetGhost2Direction(Vector3[] possibleDirections)
    {
        float currentDistance = Vector3.Distance(transform.position, pacStudent.transform.position);
        List<Vector3> validDirections = new List<Vector3>();
        
        foreach (Vector3 direction in possibleDirections)
        {
            Vector3 newPos = GridToWorldPosition(currentGridPos + WorldToGridDirection(direction));
            float newDistance = Vector3.Distance(newPos, pacStudent.transform.position);
            if (newDistance <= currentDistance)
            {
                validDirections.Add(direction);
            }
        }
        
        return validDirections.Count > 0 ? 
            validDirections[Random.Range(0, validDirections.Count)] : 
            GetRandomDirection(possibleDirections);
    }
    
    private Vector3 GetGhost4Direction(Vector3[] possibleDirections)
    {
        Vector3[] priorityDirections = { Vector3.right, Vector3.down, Vector3.left, Vector3.up };
        
        foreach (Vector3 dir in priorityDirections)
        {
            foreach (Vector3 possibleDir in possibleDirections)
            {
                if (possibleDir == dir)
                {
                    return dir;
                }
            }
        }
        
        return GetRandomDirection(possibleDirections);
    }
    
    private Vector3 GetScaredStateDirection(Vector3[] possibleDirections)
    {
        return GetGhost1Direction(possibleDirections);
    }
    
    private Vector3 GetRandomDirection(Vector3[] possibleDirections)
    {
        return possibleDirections[Random.Range(0, possibleDirections.Length)];
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
    
    private void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetBool(IS_MOVING, isMoving || currentState == GhostState.Dead);
        if (isMoving || currentState == GhostState.Dead)
        {
            UpdateAnimationDirection();
        }
    }
    
    private void UpdateAnimationDirection()
    {
        if (animator == null) return;
        Vector3 direction = currentState == GhostState.Dead ? 
            (SPAWN_CENTER - transform.position).normalized : currentDirection;
        animator.SetFloat(MOVE_X, direction.x);
        animator.SetFloat(MOVE_Y, direction.y);
    }
    
    public void SetNormal()
    {
        currentState = GhostState.Normal;
        currentSpeed = normalSpeed;
        UpdateAnimator();
    }
    
    public void SetScared()
    {
        currentState = GhostState.Scared;
        currentSpeed = scaredSpeed;
        UpdateAnimator();
    }
    
    public void SetRecovering()
    {
        currentState = GhostState.Recovering;
        currentSpeed = recoveringSpeed;
        UpdateAnimator();
    }
    
    public void SetDead()
    {
        currentState = GhostState.Dead;
        currentSpeed = deadSpeed;
        UpdateAnimator();
    }
    
    public void Die()
    {
        SetDead();
    }
    
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
        isInSpawnArea = true;
        hasExitedSpawn = false;
        ForceInitialExitDirection();
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
    
    public void StopMovement()
    {
        enabled = false;
    }
}