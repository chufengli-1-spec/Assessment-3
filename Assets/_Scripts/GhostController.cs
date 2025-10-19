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
    public float normalSpeed = 2.7f;
    public float scaredSpeed = 1.35f;
    public float recoveringSpeed = 1.35f;
    public float deadSpeed = 4.0f;
    
    public int ghostID = 1;
    
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
    
    private Queue<Vector2Int> positionHistory = new Queue<Vector2Int>();
    private const int HISTORY_SIZE = 2;
    
    private Vector3[] clockwiseDirections = { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
    private int currentClockwiseIndex = 0;
    
    private Vector3[] cornerTargets = {
        new Vector3(-19f, -17f, 0f),
        new Vector3(-19f, 9f, 0f),
        new Vector3(6f, 9f, 0f),
        new Vector3(6f, -17f, 0f)
    };
    private int currentCornerIndex = 0;
    private bool hasReachedFirstCorner = false;
    
    private PacStudentController pacStudent;
    private LevelGenerator levelGenerator;
    private GameManager gameManager;
    private int originalMapWidth;
    private int originalMapHeight;
    
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
        
        if (ghostID == 1 || ghostID == 3)
        {
            currentDirection = Vector3.up;
        }
        else
        {
            currentDirection = Vector3.down;
        }
        lastDirection = -currentDirection;
        
        if (ghostID == 4)
        {
            currentClockwiseIndex = 0;
            currentDirection = clockwiseDirections[currentClockwiseIndex];
            currentCornerIndex = 0;
            hasReachedFirstCorner = false;
        }
        
        SetNormal();
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
        Vector3 spawnCenter = new Vector3((SPAWN_MIN_X + SPAWN_MAX_X) / 2, (SPAWN_MIN_Y + SPAWN_MAX_Y) / 2, 0);
        float distanceToSpawn = Vector3.Distance(transform.position, spawnCenter);
        
        if (distanceToSpawn < 0.5f)
        {
            OnReachedSpawnArea();
        }
        else
        {
            Vector3 directionToSpawn = (spawnCenter - transform.position).normalized;
            transform.position += directionToSpawn * deadSpeed * Time.deltaTime;
        }
    }
    
    private void MakeMovementDecision()
    {
        if (isMoving) return;
        
        if (IsInSpawnArea(transform.position) && !hasExitedSpawn)
        {
            ForceExitSpawn();
            return;
        }
        
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
            targetPosition = new Vector3(-6f, -1f, 0f);
        }
        else
        {
            targetPosition = new Vector3(-6f, -7f, 0f);
        }

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Vector3 gridDirection = RoundToGridDirection(directionToTarget);

        Vector2Int targetGridPos = currentGridPos + WorldToGridDirection(gridDirection);

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

        Vector2Int testPos = currentGridPos + WorldToGridDirection(currentDirection);
        Vector3 testWorldPos = GridToWorldPosition(testPos);
        if (!IsInSpawnArea(testWorldPos))
        {
            hasExitedSpawn = true;
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
        return tile == 0 || tile == 5 || tile == 6 || tile == 8;
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
        
        if (tile == 8 && currentState != GhostState.Dead)
        {
            if (IsInSpawnArea(GridToWorldPosition(currentGridPos)) && !hasExitedSpawn)
            {
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
        
        bool isWalkable = currentState == GhostState.Dead ? 
            IsPositionWalkableForDeadGhost(targetGridPos) : 
            IsPositionWalkable(targetGridPos);
        
        if (!isWalkable)
        {
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
            
            if (IsInSpawnArea(transform.position))
            {
                hasExitedSpawn = false;
            }
            else
            {
                hasExitedSpawn = true;
            }
            
            if (ghostID == 4)
            {
                CheckCornerReached();
            }
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
            }
            
            currentCornerIndex = (currentCornerIndex + 1) % cornerTargets.Length;
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
            
            UpdateAnimation();
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
        
        if (currentState != GhostState.Dead)
        {
            Vector2Int coords = MapToOriginalQuadrant(newGridPos);
            if (coords.x >= 0 && coords.x < originalMapWidth && 
                coords.y >= 0 && coords.y < originalMapHeight)
            {
                int tile = levelGenerator.levelMap[coords.y, coords.x];
                if (tile == 8)
                {
                    if (IsInSpawnArea(transform.position) && !hasExitedSpawn)
                    {
                    }
                    else
                    {
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
    
    if (!hasReachedFirstCorner)
    {
        Vector3 bottomLeftTarget = new Vector3(-19f, -17f, 0f);
        float distance = Vector3.Distance(currentPos, bottomLeftTarget);
        if (distance > 0.5f)
        {
            return FindDirectionToTarget(possibleDirections, bottomLeftTarget);
        }
        else
        {
            hasReachedFirstCorner = true;
            currentCornerIndex = 1;
        }
    }
    
    return GetExactWallPathDirection(possibleDirections, currentPos);
}

private Vector3 GetExactWallPathDirection(Vector3[] possibleDirections, Vector3 currentPos)
{
    switch (currentCornerIndex)
    {
        case 1:
            return GetPhase1Direction(possibleDirections, currentPos);
        case 2:
            return GetPhase2Direction(possibleDirections, currentPos);
        case 3:
            return GetPhase3Direction(possibleDirections, currentPos);
        case 0:
            return GetPhase4Direction(possibleDirections, currentPos);
        default:
            return GetRandomDirection(possibleDirections);
    }
}

private Vector3 GetPhase1Direction(Vector3[] possibleDirections, Vector3 currentPos)
{
    if (currentPos.y < 9f && Mathf.Abs(currentPos.x - (-19f)) < 0.5f)
    {
        if (ArrayContains(possibleDirections, Vector3.up)) return Vector3.up;
    }
    
    if (Mathf.Abs(currentPos.y - 9f) < 0.5f && currentPos.x < 6f)
    {
        if (ArrayContains(possibleDirections, Vector3.right)) return Vector3.right;
    }
    
    if (Mathf.Abs(currentPos.x - 6f) < 0.5f && currentPos.y > 2f)
    {
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    Vector3 targetPos1 = new Vector3(-14f, 2f, 0f);
    if (Vector3.Distance(currentPos, targetPos1) < 0.5f || 
        (Mathf.Abs(currentPos.y - 2f) < 0.5f && currentPos.x > -19f))
    {
        if (ArrayContains(possibleDirections, Vector3.left)) return Vector3.left;
    }
    
    if (Mathf.Abs(currentPos.x - (-19f)) < 0.5f && currentPos.y < 9f)
    {
        if (ArrayContains(possibleDirections, Vector3.up)) return Vector3.up;
    }
    
    Vector3 topLeftTarget = new Vector3(-19f, 9f, 0f);
    if (Vector3.Distance(currentPos, topLeftTarget) < 0.5f)
    {
        currentCornerIndex = 2;
    }
    
    return GetDirectionToNextPoint(possibleDirections, currentPos, GetPhase1Target(currentPos));
}

private Vector3 GetPhase2Direction(Vector3[] possibleDirections, Vector3 currentPos)
{
    Vector3 targetPos2 = new Vector3(-5f, 5f, 0f);
    if (Vector3.Distance(currentPos, targetPos2) > 0.5f && Mathf.Abs(currentPos.y - 9f) < 0.5f)
    {
        if (ArrayContains(possibleDirections, Vector3.right)) return Vector3.right;
    }
    
    if (Vector3.Distance(currentPos, targetPos2) < 0.5f || 
        (Mathf.Abs(currentPos.x - (-5f)) < 0.5f && currentPos.y < 9f))
    {
        if (ArrayContains(possibleDirections, Vector3.up)) return Vector3.up;
    }
    
    if (Mathf.Abs(currentPos.y - 9f) < 0.5f && currentPos.x < 6f)
    {
        if (ArrayContains(possibleDirections, Vector3.right)) return Vector3.right;
    }
    
    Vector3 topRightTarget = new Vector3(6f, 9f, 0f);
    if (Vector3.Distance(currentPos, topRightTarget) < 0.5f)
    {
        currentCornerIndex = 3;
    }
    
    return GetDirectionToNextPoint(possibleDirections, currentPos, GetPhase2Target(currentPos));
}

private Vector3 GetPhase3Direction(Vector3[] possibleDirections, Vector3 currentPos)
{
    if (currentPos.y > -17f && Mathf.Abs(currentPos.x - 6f) < 0.5f)
    {
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    if (Mathf.Abs(currentPos.y - (-17f)) < 0.5f && currentPos.x > -19f)
    {
        if (ArrayContains(possibleDirections, Vector3.left)) return Vector3.left;
    }
    
    Vector3 targetPos3 = new Vector3(1f, -10f, 0f);
    if (Mathf.Abs(currentPos.x - (-19f)) < 0.5f && currentPos.y > -10f)
    {
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    if (Vector3.Distance(currentPos, targetPos3) < 0.5f || 
        (Mathf.Abs(currentPos.y - (-10f)) < 0.5f && currentPos.x < 6f))
    {
        if (ArrayContains(possibleDirections, Vector3.right)) return Vector3.right;
    }
    
    if (Mathf.Abs(currentPos.x - 6f) < 0.5f && currentPos.y > -17f)
    {
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    Vector3 bottomRightTarget = new Vector3(6f, -17f, 0f);
    if (Vector3.Distance(currentPos, bottomRightTarget) < 0.5f)
    {
        currentCornerIndex = 0;
    }
    
    return GetDirectionToNextPoint(possibleDirections, currentPos, GetPhase3Target(currentPos));
}

private Vector3 GetPhase4Direction(Vector3[] possibleDirections, Vector3 currentPos)
{
    Vector3 targetPos4 = new Vector3(-8f, -13f, 0f);
    if (Vector3.Distance(currentPos, targetPos4) > 0.5f && Mathf.Abs(currentPos.y - (-17f)) < 0.5f)
    {
        if (ArrayContains(possibleDirections, Vector3.left)) return Vector3.left;
    }
    
    if (Vector3.Distance(currentPos, targetPos4) < 0.5f || 
        (Mathf.Abs(currentPos.x - (-8f)) < 0.5f && currentPos.y > -17f))
    {
        if (ArrayContains(possibleDirections, Vector3.down)) return Vector3.down;
    }
    
    if (Mathf.Abs(currentPos.y - (-17f)) < 0.5f && currentPos.x > -19f)
    {
        if (ArrayContains(possibleDirections, Vector3.left)) return Vector3.left;
    }
    
    Vector3 bottomLeftTarget = new Vector3(-19f, -17f, 0f);
    if (Vector3.Distance(currentPos, bottomLeftTarget) < 0.5f)
    {
        currentCornerIndex = 1;
    }
    
    return GetDirectionToNextPoint(possibleDirections, currentPos, GetPhase4Target(currentPos));
}

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
        
        animator.SetBool("IsMoving", isMoving || currentState == GhostState.Dead);
        
        Vector3 direction = currentState == GhostState.Dead ? 
            (new Vector3((SPAWN_MIN_X + SPAWN_MAX_X) / 2, (SPAWN_MIN_Y + SPAWN_MAX_Y) / 2, 0) - transform.position).normalized : 
            currentDirection;
            
        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);
        
        animator.SetBool("Normal", currentState == GhostState.Normal);
        animator.SetBool("Scared", currentState == GhostState.Scared);
        animator.SetBool("Recovering", currentState == GhostState.Recovering);
        animator.SetBool("Dead", currentState == GhostState.Dead);
    }
    
    public void SetNormal() { 
        currentState = GhostState.Normal; 
        currentSpeed = normalSpeed; 
    }
    
    public void SetScared() { 
        currentState = GhostState.Scared; 
        currentSpeed = scaredSpeed; 
    }
    
    public void SetRecovering() { 
        currentState = GhostState.Recovering; 
        currentSpeed = recoveringSpeed; 
    }
    
    public void SetDead() { 
        currentState = GhostState.Dead; 
        currentSpeed = deadSpeed; 
        
        if (animator != null)
        {
            animator.SetBool("Dead", true);
            animator.SetBool("Normal", false);
            animator.SetBool("Scared", false);
            animator.SetBool("Recovering", false);
            animator.SetBool("IsMoving", true);
        }
    }
    
    public void Die() 
    { 
        SetDead(); 
        UpdateAnimation();
    }
    
    public void ResetToInitialPosition()
    {
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