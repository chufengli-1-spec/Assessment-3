using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    
    [Header("Audio Settings")]
    public AudioClip movementAudio;          // 移动时的音频
    public AudioClip pelletEatingAudio;      // 吃豆子时的音频
    
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
    private AudioSource audioSource;

    private int originalMapWidth;
    private int originalMapHeight;
    
    // 动画状态名称
    private const string WALK_DOWN_STATE = "Sheep_Walk_Down";
    private const string WALK_RIGHT_STATE = "Sheep_Walk_Right";
    private const string WALK_LEFT_STATE = "Sheep_Walk_Left";
    private const string DIE_STATE = "Sheep_Die";
    
    // 动画参数名称
    private const string IS_MOVING = "IsMoving";
    private const string MOVE_X = "MoveX";
    private const string MOVE_Y = "MoveY";
    
    // 音频状态跟踪
    private bool wasMoving = false;
    private bool isPlayingPelletAudio = false;

    void Start()
    {
        levelGenerator = FindObjectOfType<LevelGenerator>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
        
        // 配置音频源
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D音频
        
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
        
        // 更新动画和音频状态
        UpdateAnimationAndAudio();
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

    /// <summary>
    /// 更新动画和音频状态
    /// </summary>
    private void UpdateAnimationAndAudio()
    {
        // 更新动画状态
        UpdateAnimationState();
        
        // 更新音频状态
        UpdateAudioState();
        
        // 记录上一帧的移动状态
        wasMoving = isLerping;
    }

    /// <summary>
    /// 更新动画状态
    /// </summary>
    private void UpdateAnimationState()
    {
        if (animator == null) return;
        
        // 设置移动状态参数 - 这个会触发动画状态转换
        animator.SetBool(IS_MOVING, isLerping);
        
        // 只有在改变方向或者开始移动时才更新方向
        if (isLerping && !wasMoving)
        {
            UpdateAnimationDirection();
        }
    }

    /// <summary>
    /// 更新动画方向
    /// </summary>
    private void UpdateAnimationDirection()
    {
        if (animator == null) return;
        
        Vector2Int direction = GetDirectionFromKeyCode(currentInput);
        
        // 设置方向参数
        animator.SetFloat(MOVE_X, direction.x);
        animator.SetFloat(MOVE_Y, direction.y);
        
        // 根据方向设置对应的动画状态
        SetAnimationStateByDirection(direction);
    }

    /// <summary>
    /// 根据方向设置动画状态
    /// </summary>
    private void SetAnimationStateByDirection(Vector2Int direction)
    {
        if (animator == null) return;
        
        // 注意：在Unity 2D中，Y轴向上为正，但在网格系统中Y轴向下为正
        // 所以需要根据您的具体动画来调整
        
        if (direction == Vector2Int.down) // W - 向上移动
        {
            // 对应 Sheep_Walk_Down 状态（根据您的命名，可能需要调整）
            animator.Play(WALK_DOWN_STATE);
        }
        else if (direction == Vector2Int.right) // D - 向右移动
        {
            animator.Play(WALK_RIGHT_STATE);
        }
        else if (direction == Vector2Int.left) // A - 向左移动
        {
            animator.Play(WALK_LEFT_STATE);
        }
        else if (direction == Vector2Int.up) // S - 向下移动
        {
            // 如果没有专门的向上动画，使用向下动画或默认动画
            animator.Play(WALK_DOWN_STATE);
        }
    }

    /// <summary>
    /// 更新音频状态
    /// </summary>
    private void UpdateAudioState()
    {
        if (audioSource == null) return;
        
        // 检查移动状态变化
        if (isLerping && !wasMoving)
        {
            // 开始移动：播放音频
            PlayMovementAudio();
        }
        else if (!isLerping && wasMoving)
        {
            // 停止移动：停止音频
            StopMovementAudio();
        }
        
        // 如果正在移动，检查是否需要切换音频类型
        if (isLerping)
        {
            CheckAndUpdateAudioType();
        }
    }

    /// <summary>
    /// 播放移动音频
    /// </summary>
    private void PlayMovementAudio()
    {
        if (audioSource == null) return;
        
        // 确定使用哪种音频
        AudioClip clipToPlay = GetAppropriateAudioClip();
        
        if (clipToPlay != null && clipToPlay != audioSource.clip)
        {
            audioSource.clip = clipToPlay;
        }
        
        // 不修改音调和音量，保持原始设置
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    /// <summary>
    /// 停止移动音频
    /// </summary>
    private void StopMovementAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 检查并更新音频类型（普通移动 vs 吃豆子）
    /// </summary>
    private void CheckAndUpdateAudioType()
    {
        if (audioSource == null || !audioSource.isPlaying) return;
        
        AudioClip appropriateClip = GetAppropriateAudioClip();
        bool shouldPlayPelletAudio = (appropriateClip == pelletEatingAudio);
        
        // 如果音频类型需要改变
        if (shouldPlayPelletAudio != isPlayingPelletAudio)
        {
            if (shouldPlayPelletAudio && pelletEatingAudio != null)
            {
                // 切换到吃豆子音频
                audioSource.clip = pelletEatingAudio;
                isPlayingPelletAudio = true;
            }
            else if (!shouldPlayPelletAudio && movementAudio != null)
            {
                // 切换到普通移动音频
                audioSource.clip = movementAudio;
                isPlayingPelletAudio = false;
            }
            
            // 重新播放新音频
            audioSource.Stop();
            audioSource.Play();
        }
        
        // 不修改音调，保持原始音调
    }

    /// <summary>
    /// 获取合适的音频片段
    /// </summary>
    private AudioClip GetAppropriateAudioClip()
    {
        // 检查目标位置是否有豆子
        if (IsTargetPositionHasPellet())
        {
            return pelletEatingAudio;
        }
        
        // 默认使用普通移动音频
        return movementAudio;
    }

    /// <summary>
    /// 检查目标位置是否有豆子
    /// </summary>
    private bool IsTargetPositionHasPellet()
    {
        if (!isLerping) return false;
        
        Vector2Int originalCoords = MapToOriginalQuadrant(targetGridPos);
        
        if (originalCoords.x < 0 || originalCoords.x >= originalMapWidth || 
            originalCoords.y < 0 || originalCoords.y >= originalMapHeight)
        {
            return false;
        }
        
        int tileType = levelGenerator.levelMap[originalCoords.y, originalCoords.x];
        
        // 检查是否是豆子或能量豆
        return tileType == 5 || tileType == 6; // 5 = Pellet, 6 = Power Pellet
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

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.Play(DIE_STATE);
        }
        
        // 停止移动音频
        StopMovementAudio();
    }
}