using UnityEngine;
using System.Collections;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;

    [Header("Audio Settings")]
    public AudioClip movementAudio;      
    public AudioClip pelletEatingAudio;  
    public float audioInterval = 1.0f;

    [Header("Collision Effects")]
    public GameObject wallCollisionParticle;
    public AudioClip wallCollisionSound;
    public GameObject deathParticle;
    public float wallCollisionCooldown = 0.5f; // 墙壁碰撞冷却时间

    [Header("References")]
    public GameManager gameManager;

    private KeyCode lastInput;
    private KeyCode currentInput;

    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float lerpTime;
    private bool isLerping = false;
    private bool isDead = false;

    private LevelGenerator levelGenerator;
    private Animator animator;
    private AudioSource audioSource;

    private int originalMapWidth;
    private int originalMapHeight;

    private Vector3 lastValidPosition;
    private bool hasWallCollisionThisFrame = false;
    private float lastWallCollisionTime = 0f; // 上次墙壁碰撞时间

    private const string WALK_DOWN_STATE = "Sheep_Walk_Down";
    private const string WALK_RIGHT_STATE = "Sheep_Walk_Right";
    private const string WALK_LEFT_STATE = "Sheep_Walk_Left";
    private const string DIE_STATE = "Sheep_Die";

    private const string IS_MOVING = "IsMoving";
    private const string MOVE_X = "MoveX";
    private const string MOVE_Y = "MoveY";

    private bool wasMoving = false;
    private bool isPlayingPelletAudio = false;
    private float audioTimer = 0f;

    public bool IsDead { get { return isDead; } }
    public bool IsMoving { get { return isLerping; } }
    public Vector2Int CurrentGridPosition { get { return currentGridPos; } }
    public KeyCode CurrentDirection { get { return currentInput; } }

    void Start()
    {
        levelGenerator = FindObjectOfType<LevelGenerator>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (levelGenerator == null)
        {
            Debug.LogError("LevelGenerator not found in scene!");
            return;
        }

        originalMapWidth = levelGenerator.levelMap.GetLength(1);
        originalMapHeight = levelGenerator.levelMap.GetLength(0);

        transform.position = new Vector3(-19f, 9f, 0f);
        currentGridPos = WorldToGridPosition(transform.position);
        lastValidPosition = transform.position;

        lastInput = KeyCode.D;
        currentInput = KeyCode.D;

        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        UpdateAnimationDirection();
    }

    void Update()
    {
        if (levelGenerator == null || isDead) return;

        HandleInput();

        if (isLerping)
            TryChangeDirectionWhileMoving();

        if (!isLerping)
            TryMoveWithInput();
        else
            ContinueLerping();

        UpdateAnimationAndAudio();

        hasWallCollisionThisFrame = false;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) lastInput = KeyCode.W;
        if (Input.GetKeyDown(KeyCode.A)) lastInput = KeyCode.A;
        if (Input.GetKeyDown(KeyCode.S)) lastInput = KeyCode.S;
        if (Input.GetKeyDown(KeyCode.D)) lastInput = KeyCode.D;
    }

    private void TryChangeDirectionWhileMoving()
    {
        Vector2Int lastInputDirection = GetDirectionFromKeyCode(lastInput);
        Vector2Int targetPos = currentGridPos + lastInputDirection;

        if (lastInput != currentInput && IsPositionWalkable(targetPos))
        {
            float progress = Vector3.Distance(transform.position, startPosition) /
                             Vector3.Distance(targetPosition, startPosition);

            if (progress < 0.7f)
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
            else
            {
                // 如果两个方向都不能走，触发墙壁碰撞
                HandleWallCollision(lastInputDirection);
            }
        }
    }

    private void StartLerping(Vector2Int direction)
    {
        targetGridPos = currentGridPos + direction;
        
        // 在开始移动前进行碰撞检测
        if (!IsPositionWalkable(targetGridPos))
        {
            HandleWallCollision(direction);
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
            lastValidPosition = targetPosition;
            isLerping = false;

            // 在移动完成时收集豆子
            CollectPelletAtPositionIfExists(targetGridPos);
            
            Vector2Int currentInputDirection = GetDirectionFromKeyCode(currentInput);
            if (IsPositionWalkable(currentGridPos + currentInputDirection))
                StartLerping(currentInputDirection);
        }
    }

    private void CollectPelletAtPositionIfExists(Vector2Int gridPosition)
    {
        Vector3 worldPos = GridToWorldPosition(gridPosition);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.1f);
        
        foreach (Collider2D collider in colliders)
        {
            if (collider != null && (collider.CompareTag("Pellet") || collider.CompareTag("PowerPill")))
            {
                Destroy(collider.gameObject);
                
                if (collider.CompareTag("Pellet"))
                {
                    CollectPellet(10);
                    // 播放吃豆声音
                    if (pelletEatingAudio != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(pelletEatingAudio);
                    }
                }
                else if (collider.CompareTag("PowerPill"))
                {
                    CollectPellet(50);
                    if (gameManager != null)
                    {
                        gameManager.ActivatePowerPillMode();
                    }
                    // 播放吃能量丸声音
                    if (pelletEatingAudio != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(pelletEatingAudio);
                    }
                }
                break;
            }
        }
    }

    private void UpdateAnimationAndAudio()
    {
        UpdateAnimationState();
        UpdateAudioState();
        wasMoving = isLerping;
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        animator.SetBool(IS_MOVING, isLerping);

        if (isLerping && !wasMoving)
            UpdateAnimationDirection();
    }

    private void UpdateAnimationDirection()
    {
        if (animator == null) return;

        Vector2Int direction = GetDirectionFromKeyCode(currentInput);

        animator.SetFloat(MOVE_X, direction.x);
        animator.SetFloat(MOVE_Y, direction.y);
        SetAnimationStateByDirection(direction);
    }

    private void SetAnimationStateByDirection(Vector2Int direction)
    {
        if (animator == null) return;

        if (direction == Vector2Int.down) animator.Play(WALK_DOWN_STATE);
        else if (direction == Vector2Int.right) animator.Play(WALK_RIGHT_STATE);
        else if (direction == Vector2Int.left) animator.Play(WALK_LEFT_STATE);
        else if (direction == Vector2Int.up) animator.Play(WALK_DOWN_STATE);
    }

    private void UpdateAudioState()
    {
        if (audioSource == null) return;

        if (isLerping && !wasMoving)
        {
            PlayMovementAudio();
            audioTimer = 0f;
        }
        else if (!isLerping && wasMoving)
            StopMovementAudio();

        if (isLerping)
        {
            audioTimer += Time.deltaTime;
            if (audioTimer >= audioInterval)
            {
                PlayMovementAudio();
                audioTimer = 0f;
            }
        }
    }

    private void PlayMovementAudio()
    {
        if (audioSource == null || movementAudio == null) return;

        // 只在没有播放吃豆声音时播放移动声音
        if (!audioSource.isPlaying || audioSource.clip != pelletEatingAudio)
        {
            audioSource.clip = movementAudio;
            audioSource.Play();
        }
    }

    private void StopMovementAudio()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == movementAudio)
            audioSource.Stop();
    }

    private Vector2Int MapToOriginalQuadrant(Vector2Int fullPos)
    {
        int x = fullPos.x;
        int y = fullPos.y;
        
        int fullWidth = originalMapWidth * 2;
        int fullHeight = (originalMapHeight * 2) - 2;
        
        if (x < 0 || x >= fullWidth || y < 0 || y >= fullHeight)
        {
            Debug.Log($"MapToOriginalQuadrant: {fullPos} -> OUT OF BOUNDS (fullSize: {fullWidth}x{fullHeight})");
            return new Vector2Int(-1, -1);
        }
        
        bool isRightQuadrant = x >= originalMapWidth;
        bool isBottomQuadrant = y >= originalMapHeight - 1;
        
        int originalX, originalY;
        string quadrantName = "";
        
        if (!isRightQuadrant && !isBottomQuadrant)
        {
            // 左上象限
            quadrantName = "TopLeft";
            originalX = x;
            originalY = y;
        }
        else if (isRightQuadrant && !isBottomQuadrant)
        {
            // 右上象限
            quadrantName = "TopRight";
            originalX = (originalMapWidth - 1) - (x - originalMapWidth);
            originalY = y;
        }
        else if (!isRightQuadrant && isBottomQuadrant)
        {
            // 左下象限
            quadrantName = "BottomLeft";
            originalX = x;
            
            // 计算在底部象限中的局部坐标（从0开始）
            int bottomLocalY = y - (originalMapHeight - 1);
            // 镜像映射：将底部象限的坐标映射回原始地图
            originalY = (originalMapHeight - 1) - bottomLocalY;
            
            // 边界检查
            if (originalY < 0 || originalY >= originalMapHeight)
            {
                Debug.Log($"MapToOriginalQuadrant: {fullPos} -> {quadrantName} -> OriginalY {originalY} OUT OF RANGE (0-{originalMapHeight-1})");
                return new Vector2Int(-1, -1);
            }
        }
        else
        {
            // 右下象限
            quadrantName = "BottomRight";
            originalX = (originalMapWidth - 1) - (x - originalMapWidth);
            
            // 计算在底部象限中的局部坐标（从0开始）
            int bottomLocalY = y - (originalMapHeight - 1);
            // 镜像映射：将底部象限的坐标映射回原始地图
            originalY = (originalMapHeight - 1) - bottomLocalY;
            
            // 边界检查
            if (originalY < 0 || originalY >= originalMapHeight)
            {
                Debug.Log($"MapToOriginalQuadrant: {fullPos} -> {quadrantName} -> OriginalY {originalY} OUT OF RANGE (0-{originalMapHeight-1})");
                return new Vector2Int(-1, -1);
            }
        }
        
        Vector2Int result = new Vector2Int(originalX, originalY);
        
        // 详细调试信息（只输出特定区域以减少日志数量）
        if (y >= 13 && y <= 18) // 只调试底部区域
        {
            Vector3 worldPos = GridToWorldPosition(fullPos);
            string validity = (result.x >= 0 && result.x < originalMapWidth && result.y >= 0 && result.y < originalMapHeight) ? "VALID" : "INVALID";
            
            if (validity == "VALID")
            {
                int tile = levelGenerator.levelMap[result.y, result.x];
                Debug.Log($"MapToOriginalQuadrant: Full{fullPos} -> World{worldPos} -> {quadrantName} -> Original{result} " +
                         $"[Tile: {tile}, Walkable: {IsTileWalkable(tile)}] {validity}");
            }
            else
            {
                Debug.Log($"MapToOriginalQuadrant: Full{fullPos} -> World{worldPos} -> {quadrantName} -> Original{result} {validity}");
            }
        }
        
        return result;
    }

    private bool IsPositionWalkable(Vector2Int gridPosition)
    {
        Vector2Int coords = MapToOriginalQuadrant(gridPosition);
        
        if (coords.x < 0 || coords.x >= originalMapWidth || 
            coords.y < 0 || coords.y >= originalMapHeight)
            return false;

        int tile = levelGenerator.levelMap[coords.y, coords.x];
        return IsTileWalkable(tile);
    }

    private bool IsTileWalkable(int tile)
    {
        switch (tile)
        {
            case 0: // Empty - walkable
            case 5: // Pellet - walkable
            case 6: // Power Pellet - walkable
                return true;
            case 1: // Outside Corner - wall
            case 2: // Outside Wall - wall
            case 3: // Inside Corner - wall
            case 4: // Inside Wall - wall
            case 7: // T-Junction - wall
            case 8: // Ghost Exit - wall
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

    private Vector3 GridToWorldPosition(Vector2Int grid)
    {
        if (levelGenerator == null)
            return Vector3.zero;
            
        float x = levelGenerator.startPosition.x + grid.x * levelGenerator.tileSize;
        float y = levelGenerator.startPosition.y - grid.y * levelGenerator.tileSize;
        return new Vector3(x, y, 0f);
    }

    private Vector2Int WorldToGridPosition(Vector3 world)
    {
        if (levelGenerator == null)
            return Vector2Int.zero;
            
        int gx = Mathf.RoundToInt((world.x - levelGenerator.startPosition.x) / levelGenerator.tileSize);
        int gy = Mathf.RoundToInt((levelGenerator.startPosition.y - world.y) / levelGenerator.tileSize);
        return new Vector2Int(gx, gy);
    }

    // =================== 碰撞相关方法 =====================
    public void HandleWallCollision(Vector2Int collisionDir)
    {
        if (hasWallCollisionThisFrame) return;
        
        // 检查冷却时间
        if (Time.time - lastWallCollisionTime < wallCollisionCooldown)
            return;

        hasWallCollisionThisFrame = true;
        lastWallCollisionTime = Time.time; // 更新最后碰撞时间
        
        isLerping = false;
        transform.position = lastValidPosition;
        currentGridPos = WorldToGridPosition(lastValidPosition);

        if (wallCollisionParticle != null)
        {
            Vector3 point = transform.position + new Vector3(collisionDir.x, collisionDir.y, 0) * 0.5f;
            Instantiate(wallCollisionParticle, point, Quaternion.identity);
        }

        if (wallCollisionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wallCollisionSound);
        }

        Debug.Log("Wall collision detected! Stopped at: " + currentGridPos);
    }

    public void CollectPellet(int points)
    {
        if (gameManager != null)
            gameManager.AddScore(points);
    }

    public void Die()
    {
        isDead = true;
        isLerping = false;

        if (animator != null)
            animator.Play(DIE_STATE);

        if (deathParticle != null)
            Instantiate(deathParticle, transform.position, Quaternion.identity);

        StopMovementAudio();

        if (gameManager != null)
            gameManager.PacStudentDied();
    }

    public void Respawn()
    {
        isDead = false;
        transform.position = new Vector3(-19f, 9f, 0f);
        currentGridPos = WorldToGridPosition(transform.position);
        lastValidPosition = transform.position;

        lastInput = KeyCode.D;
        currentInput = KeyCode.D;

        UpdateAnimationDirection();
    }

    public KeyCode GetCurrentDirection() { return currentInput; }
    public Vector2Int GetCurrentGridPosition() { return currentGridPos; }
}