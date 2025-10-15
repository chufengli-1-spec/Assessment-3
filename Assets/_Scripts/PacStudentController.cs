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

    [Header("Teleporter Settings")]
    public AudioClip teleportSound; // 新增：传送音效
    public GameObject teleportParticle; // 新增：传送粒子效果

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
    private bool isTeleporting = false; // 新增：防止重复传送

    private LevelGenerator levelGenerator;
    private Animator animator;
    private AudioSource audioSource;

    private int originalMapWidth;
    private int originalMapHeight;

    private Vector3 lastValidPosition;
    private bool hasWallCollisionThisFrame = false;
    private float lastWallCollisionTime = 0f; // 上次墙壁碰撞时间

    // 传送门位置常量
    private readonly Vector3 LEFT_TELEPORTER_POS = new Vector3(-20f, -4f, 0f);
    private readonly Vector3 RIGHT_TELEPORTER_POS = new Vector3(7f, -4f, 0f);
    private readonly Vector3 LEFT_TELEPORT_TARGET = new Vector3(-19f, -4f, 0f);
    private readonly Vector3 RIGHT_TELEPORT_TARGET = new Vector3(6f, -4f, 0f);
    private const float TELEPORT_DETECTION_RANGE = 0.3f;

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
        
        Debug.Log("PacStudentController initialized");
    }

    void Update()
    {
        if (levelGenerator == null || isDead) return;

        HandleInput();

        // 新增：传送检测（在移动逻辑之前）
        if (!isTeleporting)
        {
            CheckForTeleport();
        }

        if (isLerping)
            TryChangeDirectionWhileMoving();

        if (!isLerping)
            TryMoveWithInput();
        else
            ContinueLerping();

        UpdateAnimationAndAudio();

        hasWallCollisionThisFrame = false;
    }

    // 新增：传送检测方法
    private void CheckForTeleport()
    {
        Vector3 currentPos = transform.position;
        
        // 检查左侧传送门
        if (Vector3.Distance(currentPos, LEFT_TELEPORTER_POS) <= TELEPORT_DETECTION_RANGE)
        {
            StartTeleport(false); // 从左侧传送到右侧
        }
        // 检查右侧传送门
        else if (Vector3.Distance(currentPos, RIGHT_TELEPORTER_POS) <= TELEPORT_DETECTION_RANGE)
        {
            StartTeleport(true); // 从右侧传送到左侧
        }
    }

    // 新增：开始传送过程
    private void StartTeleport(bool fromRightToLeft)
    {
        if (isTeleporting) return;
        
        isTeleporting = true;
        isLerping = false; // 停止当前的移动

        // 播放传送音效
        if (teleportSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }

        // 生成传送粒子效果
        if (teleportParticle != null)
        {
            Instantiate(teleportParticle, transform.position, Quaternion.identity);
        }

        // 执行传送
        if (fromRightToLeft)
        {
            TeleportToLeft();
        }
        else
        {
            TeleportToRight();
        }

        // 传送完成后恢复状态
        StartCoroutine(CompleteTeleport());
    }

    // 新增：传送到左侧
    private void TeleportToLeft()
    {
        Debug.Log("Teleporting from right to left tunnel");
        
        transform.position = LEFT_TELEPORT_TARGET;
        
        // 更新位置状态
        currentGridPos = WorldToGridPosition(transform.position);
        lastValidPosition = transform.position;
        
        // 传送后强制向右移动（向内）
        lastInput = KeyCode.D;
        currentInput = KeyCode.D;
        
        // 立即开始移动
        Vector2Int direction = GetDirectionFromKeyCode(currentInput);
        if (IsPositionWalkable(currentGridPos + direction))
        {
            StartLerping(direction);
        }
        
        // 传送结束时的粒子效果
        if (teleportParticle != null)
        {
            Instantiate(teleportParticle, transform.position, Quaternion.identity);
        }
    }

    // 新增：传送到右侧
    private void TeleportToRight()
    {
        Debug.Log("Teleporting from left to right tunnel");
        
        transform.position = RIGHT_TELEPORT_TARGET;
        
        // 更新位置状态
        currentGridPos = WorldToGridPosition(transform.position);
        lastValidPosition = transform.position;
        
        // 传送后强制向左移动（向内）
        lastInput = KeyCode.A;
        currentInput = KeyCode.A;
        
        // 立即开始移动
        Vector2Int direction = GetDirectionFromKeyCode(currentInput);
        if (IsPositionWalkable(currentGridPos + direction))
        {
            StartLerping(direction);
        }
        
        // 传送结束时的粒子效果
        if (teleportParticle != null)
        {
            Instantiate(teleportParticle, transform.position, Quaternion.identity);
        }
    }

    // 新增：完成传送的协程
    private System.Collections.IEnumerator CompleteTeleport()
    {
        // 短暂延迟以确保传送完成
        yield return new WaitForSeconds(0.1f);
        isTeleporting = false;
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

        Debug.Log($"TryMoveWithInput - LastInput: {lastInput}, Direction: {lastInputDirection}");
        Debug.Log($"CurrentPos: {currentGridPos}, TargetPos: {targetPos}, Walkable: {IsPositionWalkable(targetPos)}");

        if (IsPositionWalkable(targetPos))
        {
            currentInput = lastInput;
            StartLerping(lastInputDirection);
        }
        else
        {
            Vector2Int currentInputDirection = GetDirectionFromKeyCode(currentInput);
            targetPos = currentGridPos + currentInputDirection;
            
            Debug.Log($"Primary direction blocked, trying current: {currentInput}, Direction: {currentInputDirection}");
            Debug.Log($"CurrentPos: {currentGridPos}, TargetPos: {targetPos}, Walkable: {IsPositionWalkable(targetPos)}");
            
            if (IsPositionWalkable(targetPos))
            {
                StartLerping(currentInputDirection);
            }
            else
            {
                // 如果两个方向都不能走，触发墙壁碰撞
                Debug.Log("Both directions blocked, calling HandleWallCollision");
                HandleWallCollision(lastInputDirection);
            }
        }
    }

    private void StartLerping(Vector2Int direction)
    {
        targetGridPos = currentGridPos + direction;
        
        Debug.Log($"StartLerping - Direction: {direction}, TargetGridPos: {targetGridPos}");
        
        // 在开始移动前进行碰撞检测
        if (!IsPositionWalkable(targetGridPos))
        {
            Debug.Log("StartLerping: Target position not walkable, triggering collision");
            HandleWallCollision(direction);
            return;
        }
        
        startPosition = GridToWorldPosition(currentGridPos);
        targetPosition = GridToWorldPosition(targetGridPos);
        
        lerpTime = 0f;
        isLerping = true;
        
        UpdateAnimationDirection();
        
        Debug.Log($"StartLerping: Moving from {startPosition} to {targetPosition}");
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

            Vector2Int currentInputDirection = GetDirectionFromKeyCode(currentInput);
            if (IsPositionWalkable(currentGridPos + currentInputDirection))
                StartLerping(currentInputDirection);
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
            CheckAndUpdateAudioType();
        }
    }

    private void PlayMovementAudio()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = GetAppropriateAudioClip();

        if (clipToPlay != null)
        {
            if (audioSource.clip != clipToPlay)
                audioSource.clip = clipToPlay;

            audioSource.Play();
        }
    }

    private void StopMovementAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void CheckAndUpdateAudioType()
    {
        if (audioSource == null) return;

        AudioClip appropriateClip = GetAppropriateAudioClip();
        bool shouldPlayPelletAudio = (appropriateClip == pelletEatingAudio);

        if (shouldPlayPelletAudio != isPlayingPelletAudio)
        {
            if (shouldPlayPelletAudio && pelletEatingAudio != null)
            {
                audioSource.clip = pelletEatingAudio;
                isPlayingPelletAudio = true;
            }
            else if (!shouldPlayPelletAudio && movementAudio != null)
            {
                audioSource.clip = movementAudio;
                isPlayingPelletAudio = false;
            }
        }
    }

    private AudioClip GetAppropriateAudioClip()
    {
        if (IsTargetPositionHasPellet()) return pelletEatingAudio;
        return movementAudio;
    }

    private bool IsTargetPositionHasPellet()
    {
        if (!isLerping) return false;

        Vector2Int coords = MapToOriginalQuadrant(targetGridPos);
        if (coords.x < 0 || coords.x >= originalMapWidth || coords.y < 0 || coords.y >= originalMapHeight)
            return false;

        int tile = levelGenerator.levelMap[coords.y, coords.x];
        
        // 如果是豆子或能量丸，收集它
        if (tile == 5 || tile == 6)
        {
            CollectPelletAtPosition(targetGridPos);
            return true;
        }
        
        return false;
    }

    private void CollectPelletAtPosition(Vector2Int gridPosition)
    {
        // 找到该位置的豆子游戏对象并销毁
        Vector3 worldPos = GridToWorldPosition(gridPosition);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.1f);
        
        foreach (Collider2D collider in colliders)
        {
            if (collider != null && (collider.CompareTag("Pellet") || collider.CompareTag("PowerPill")))
            {
                Destroy(collider.gameObject);
                
                // 根据类型加分
                if (collider.CompareTag("Pellet"))
                {
                    CollectPellet(10);
                }
                else if (collider.CompareTag("PowerPill"))
                {
                    CollectPellet(50);
                    if (gameManager != null)
                    {
                        gameManager.ActivatePowerPillMode();
                    }
                }
                break;
            }
        }
    }

    private bool IsPositionWalkable(Vector2Int gridPosition)
    {
        Vector2Int coords = MapToOriginalQuadrant(gridPosition);
        
        if (coords.x < 0 || coords.x >= originalMapWidth || 
            coords.y < 0 || coords.y >= originalMapHeight)
        {
            Debug.Log($"IsPositionWalkable: Position {gridPosition} -> OUT OF BOUNDS");
            return false;
        }

        int tile = levelGenerator.levelMap[coords.y, coords.x];
        bool walkable = IsTileWalkable(tile);
        
        Debug.Log($"IsPositionWalkable: Position {gridPosition} -> Original {coords} -> Tile {tile} -> Walkable: {walkable}");
        
        return walkable;
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
        return result;
    }

    private bool IsTileWalkable(int tile)
    {
        switch (tile)
        {
            case 0: // Empty - walkable
            case 5: // Pellet - walkable
            case 6: // Power Pellet - walkable
            case 8: // Ghost Exit - walkable
                return true;
            case 1: // Outside Corner - wall
            case 2: // Outside Wall - wall
            case 3: // Inside Corner - wall
            case 4: // Inside Wall - wall
            case 7: // T-Junction - wall
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
        if (hasWallCollisionThisFrame) 
        {
            Debug.Log("HandleWallCollision: Already processed this frame");
            return;
        }
        
        // 检查冷却时间
        if (Time.time - lastWallCollisionTime < wallCollisionCooldown)
        {
            Debug.Log($"HandleWallCollision: On cooldown ({Time.time - lastWallCollisionTime:F2}s)");
            return;
        }

        hasWallCollisionThisFrame = true;
        lastWallCollisionTime = Time.time;
        
        Debug.Log($"=== WALL COLLISION TRIGGERED ===");
        Debug.Log($"Direction: {collisionDir}");
        Debug.Log($"Position: {transform.position}");
        Debug.Log($"Grid Position: {currentGridPos}");

        isLerping = false;
        transform.position = lastValidPosition;
        currentGridPos = WorldToGridPosition(lastValidPosition);

        // 粒子效果生成
        if (wallCollisionParticle != null)
        {
            Vector3 collisionPoint = transform.position + new Vector3(collisionDir.x, collisionDir.y, 0) * 0.3f;
            GameObject particleEffect = Instantiate(wallCollisionParticle, collisionPoint, Quaternion.identity);
            Debug.Log($"Wall collision particle created at: {collisionPoint}");
            
            // 确保粒子系统会自动播放
            ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Debug.Log("Particle system played");
            }
        }
        else
        {
            Debug.LogError("WallCollisionParticle is null! Please assign in inspector.");
        }

        // 播放碰撞音效
        if (wallCollisionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wallCollisionSound);
            Debug.Log("Wall collision sound played");
        }
        else
        {
            Debug.LogError("WallCollisionSound or AudioSource is null!");
        }

        Debug.Log("Wall collision handled successfully!");
    }

    public void CollectPellet(int points)
    {
        if (gameManager != null)
            gameManager.AddScore(points);
    }

    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        isLerping = false;

        Debug.Log("PacStudent: Death sequence starting");

        // 播放死亡动画
        if (animator != null)
        {
            animator.Play(DIE_STATE);
            Debug.Log("PacStudent: Death animation played - " + DIE_STATE);
        }
        else
        {
            Debug.LogError("PacStudent: Animator is null!");
        }

        // 播放死亡粒子效果
        if (deathParticle != null)
        {
            Instantiate(deathParticle, transform.position, Quaternion.identity);
            Debug.Log("PacStudent: Death particle effect created");
        }

        StopMovementAudio();
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
        
        Debug.Log("PacStudent respawned");
    }

    public KeyCode GetCurrentDirection() { return currentInput; }
    public Vector2Int GetCurrentGridPosition() { return currentGridPos; }
}