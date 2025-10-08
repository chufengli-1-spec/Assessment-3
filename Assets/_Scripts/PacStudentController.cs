using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    
    private KeyCode lastInput;
    private KeyCode currentInput;
    
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float lerpTime;
    private bool isLerping = false;
    
    private LevelGenerator levelGenerator;
    private Animator animator;

    private int originalMapWidth;
    private int originalMapHeight;

    void Start()
    {
        levelGenerator = FindObjectOfType<LevelGenerator>();
        animator = GetComponent<Animator>();
        
        if (levelGenerator == null)
        {
            Debug.LogError("LevelGenerator not found in scene!");
            return;
        }
        
        originalMapWidth = levelGenerator.levelMap.GetLength(1);
        originalMapHeight = levelGenerator.levelMap.GetLength(0);
        
        transform.position = new Vector3(-19f, 9f, 0f);
        currentGridPos = WorldToGridPosition(transform.position);
        
        lastInput = KeyCode.D;
        currentInput = KeyCode.D;
        
        UpdateAnimationDirection();
        
        Debug.Log($"PacStudent initialized at world position: {transform.position}, grid position: {currentGridPos}");
    }

    void Update()
    {
        if (levelGenerator == null) return;
        
        HandleInput();
        
        if (isLerping)
        {
            TryChangeDirectionWhileMoving();
        }
        
        if (!isLerping)
        {
            TryMoveWithInput();
        }
        else
        {
            ContinueLerping();
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            lastInput = KeyCode.W;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            lastInput = KeyCode.A;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            lastInput = KeyCode.S;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            lastInput = KeyCode.D;
        }
    }

    private void TryChangeDirectionWhileMoving()
    {
        Vector2Int lastInputDirection = GetDirectionFromKeyCode(lastInput);
        Vector2Int targetPos = currentGridPos + lastInputDirection;
        
        if (lastInput != currentInput && IsPositionWalkable(targetPos))
        {
            float progressToNextCell = Vector3.Distance(transform.position, startPosition) / 
                                      Vector3.Distance(targetPosition, startPosition);
            
            if (progressToNextCell < 0.7f)
            {
                currentInput = lastInput;
                StartLerping(lastInputDirection);
            }
        }
    }

    private void TryMoveWithInput()
    {
        Vector2Int lastInputDirection = GetDirectionFromKeyCode(lastInput);
        Vector2Int targetPos = currentGridPos + lastInputDirection;
        
        if (IsPositionWalkable(targetPos))
        {
            currentInput = lastInput;
            StartLerping(lastInputDirection);
        }
        else
        {
            Vector2Int currentInputDirection = GetDirectionFromKeyCode(currentInput);
            targetPos = currentGridPos + currentInputDirection;
            
            if (IsPositionWalkable(targetPos))
            {
                StartLerping(currentInputDirection);
            }
        }
    }

    private void StartLerping(Vector2Int direction)
    {
        targetGridPos = currentGridPos + direction;
        
        if (!IsPositionWalkable(targetGridPos))
        {
            return;
        }
        
        startPosition = GridToWorldPosition(currentGridPos);
        targetPosition = GridToWorldPosition(targetGridPos);
        
        lerpTime = 0f;
        isLerping = true;
        
        UpdateAnimationDirection();
    }

    private void ContinueLerping()
    {
        lerpTime += Time.deltaTime * moveSpeed;
        
        if (lerpTime > 1f) lerpTime = 1f;
        
        transform.position = Vector3.Lerp(startPosition, targetPosition, lerpTime);
        
        if (lerpTime >= 1f)
        {
            transform.position = targetPosition;
            currentGridPos = targetGridPos;
            isLerping = false;
            
            Vector2Int currentInputDirection = GetDirectionFromKeyCode(currentInput);
            if (IsPositionWalkable(currentGridPos + currentInputDirection))
            {
                StartLerping(currentInputDirection);
            }
        }
    }

    private bool IsPositionWalkable(Vector2Int gridPosition)
    {
        Vector2Int originalCoords = MapToOriginalQuadrant(gridPosition);
        
        if (originalCoords.x < 0 || originalCoords.x >= originalMapWidth || 
            originalCoords.y < 0 || originalCoords.y >= originalMapHeight)
        {
            return false;
        }
        
        int tileType = levelGenerator.levelMap[originalCoords.y, originalCoords.x];
        
        return IsTileWalkable(tileType);
    }

    private Vector2Int MapToOriginalQuadrant(Vector2Int fullLevelPos)
    {
        int x = fullLevelPos.x;
        int y = fullLevelPos.y;
        
        bool isRightQuadrant = x >= originalMapWidth;
        bool isBottomQuadrant = y >= originalMapHeight - 1;
        
        int originalX, originalY;
        
        if (!isRightQuadrant && !isBottomQuadrant)
        {
            originalX = x;
            originalY = y;
        }
        else if (isRightQuadrant && !isBottomQuadrant)
        {
            originalX = (originalMapWidth - 1) - (x - originalMapWidth);
            originalY = y;
        }
        else if (!isRightQuadrant && isBottomQuadrant)
        {
            originalX = x;
            originalY = (originalMapHeight - 2) - (y - (originalMapHeight - 1));
        }
        else
        {
            originalX = (originalMapWidth - 1) - (x - originalMapWidth);
            originalY = (originalMapHeight - 2) - (y - (originalMapHeight - 1));
        }
        
        return new Vector2Int(originalX, originalY);
    }

    private bool IsTileWalkable(int tileType)
    {
        switch (tileType)
        {
            case 5: // Pellet - walkable
            case 6: // Power Pellet - walkable
                return true;
            case 0: // Empty - walkable (according to requirements)
                return true;
            case 1: // Outside Corner - wall
            case 2: // Outside Wall - wall
            case 3: // Inside Corner - wall
            case 4: // Inside Wall - wall
            case 7: // T-Junction - wall
            case 8: // Ghost Exit - treat as wall for now
                return false;
            default:
                return false;
        }
    }

    private Vector2Int GetDirectionFromKeyCode(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W: return Vector2Int.down;   
            case KeyCode.S: return Vector2Int.up;     
            case KeyCode.A: return Vector2Int.left;   
            case KeyCode.D: return Vector2Int.right;  
            default: return Vector2Int.right;
        }
    }

    private Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        if (levelGenerator == null)
            return Vector3.zero;
        
        int x = gridPosition.x;
        int y = gridPosition.y;
        
        int fullWidth = originalMapWidth * 2;
        int fullHeight = (originalMapHeight * 2) - 2;
        
        float worldX = levelGenerator.startPosition.x + x * levelGenerator.tileSize;
        float worldY = levelGenerator.startPosition.y - y * levelGenerator.tileSize;
        
        return new Vector3(worldX, worldY, 0f);
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        if (levelGenerator == null)
            return Vector2Int.zero;
        
        int gridX = Mathf.RoundToInt((worldPosition.x - levelGenerator.startPosition.x) / levelGenerator.tileSize);
        int gridY = Mathf.RoundToInt((levelGenerator.startPosition.y - worldPosition.y) / levelGenerator.tileSize);
        
        return new Vector2Int(gridX, gridY);
    }

    private void UpdateAnimationDirection()
    {
        if (animator == null) return;
        
        Vector2Int direction = GetDirectionFromKeyCode(currentInput);
        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);
        animator.SetFloat("Speed", isLerping ? 1f : 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || levelGenerator == null) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        Gizmos.color = Color.yellow;
        Vector3 cellCenter = GridToWorldPosition(currentGridPos);
        Gizmos.DrawWireCube(cellCenter, Vector3.one * levelGenerator.tileSize * 0.8f);
        
        if (isLerping)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }

    public Vector2Int GetCurrentGridPosition()
    {
        return currentGridPos;
    }

    public bool IsMoving()
    {
        return isLerping;
    }

    public KeyCode GetCurrentDirection()
    {
        return currentInput;
    }
}