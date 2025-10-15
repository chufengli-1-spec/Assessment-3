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
    public float wallCollisionCooldown = 0.5f;

    [Header("Teleporter Settings")]
    public AudioClip teleportSound;
    public GameObject teleportParticle;

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
    private bool isTeleporting = false;

    private LevelGenerator levelGenerator;
    private Animator animator;
    private AudioSource audioSource;

    private int originalMapWidth;
    private int originalMapHeight;

    private Vector3 lastValidPosition;
    private bool hasWallCollisionThisFrame = false;
    private float lastWallCollisionTime = 0f;

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
        if (FindObjectOfType<GameStartCountdown>()?.IsCountdownActive() == true)
            return;
             
        if (levelGenerator == null || isDead) return;

        HandleInput();

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

    private void CheckForTeleport()
    {
        Vector3 currentPos = transform.position;
        
        if (Vector3.Distance(currentPos, LEFT_TELEPORTER_POS) <= TELEPORT_DETECTION_RANGE)
        {
            StartTeleport(false);
        }
        else if (Vector3.Distance(currentPos, RIGHT_TELEPORTER_POS) <= TELEPORT_DETECTION_RANGE)
        {
            StartTeleport(true);
        }
    }

    private void StartTeleport(bool fromRightToLeft)
    {
        if (isTeleporting) return;
        
        isTeleporting = true;
        isLerping = false;

        if (teleportSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }

        if (teleportParticle != null)
        {
            Instantiate(teleportParticle, transform.position, Quaternion.identity);
        }

        if (fromRightToLeft)
        {
            TeleportToLeft();
        }
        else
        {
            TeleportToRight();
        }

        StartCoroutine(CompleteTeleport());
    }

    private void TeleportToLeft()
    {
        transform.position = LEFT_TELEPORT_TARGET;
        
        currentGridPos = WorldToGridPosition(transform.position);
        lastValidPosition = transform.position;
        
        lastInput = KeyCode.D;
        currentInput = KeyCode.D;
        
        Vector2Int direction = GetDirectionFromKeyCode(currentInput);
        if (IsPositionWalkable(currentGridPos + direction))
        {
            StartLerping(direction);
        }
        
        if (teleportParticle != null)
        {
            Instantiate(teleportParticle, transform.position, Quaternion.identity);
        }
    }

    private void TeleportToRight()
    {
        transform.position = RIGHT_TELEPORT_TARGET;
        
        currentGridPos = WorldToGridPosition(transform.position);
        lastValidPosition = transform.position;
        
        lastInput = KeyCode.A;
        currentInput = KeyCode.A;
        
        Vector2Int direction = GetDirectionFromKeyCode(currentInput);
        if (IsPositionWalkable(currentGridPos + direction))
        {
            StartLerping(direction);
        }
        
        if (teleportParticle != null)
        {
            Instantiate(teleportParticle, transform.position, Quaternion.identity);
        }
    }

    private System.Collections.IEnumerator CompleteTeleport()
    {
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
                HandleWallCollision(lastInputDirection);
            }
        }
    }

    private void StartLerping(Vector2Int direction)
    {
        targetGridPos = currentGridPos + direction;
        
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
        
        if (tile == 5 || tile == 6)
        {
            CollectPelletAtPosition(targetGridPos);
            return true;
        }
        
        return false;
    }

    private void CollectPelletAtPosition(Vector2Int gridPosition)
    {
        GameStartCountdown countdown = FindObjectOfType<GameStartCountdown>();
        if (countdown != null && countdown.IsCountdownActive())
            return;
             
        Vector3 worldPos = GridToWorldPosition(gridPosition);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.1f);
        
        foreach (Collider2D collider in colliders)
        {
            if (collider != null && (collider.CompareTag("Pellet") || collider.CompareTag("PowerPill")))
            {
                bool isPowerPill = collider.CompareTag("PowerPill");
                
                Destroy(collider.gameObject);
                
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.OnPelletCollected(isPowerPill);
                }
                
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
            return false;
        }

        int tile = levelGenerator.levelMap[coords.y, coords.x];
        bool walkable = IsTileWalkable(tile);
        
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
            return new Vector2Int(-1, -1);
        }
        
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
            
            int bottomLocalY = y - (originalMapHeight - 1);
            originalY = (originalMapHeight - 1) - bottomLocalY;
            
            if (originalY < 0 || originalY >= originalMapHeight)
            {
                return new Vector2Int(-1, -1);
            }
        }
        else
        {
            originalX = (originalMapWidth - 1) - (x - originalMapWidth);
            
            int bottomLocalY = y - (originalMapHeight - 1);
            originalY = (originalMapHeight - 1) - bottomLocalY;
            
            if (originalY < 0 || originalY >= originalMapHeight)
            {
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
            case 0:
            case 5:
            case 6:
            case 8:
                return true;
            case 1:
            case 2:
            case 3:
            case 4:
            case 7:
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

    public void HandleWallCollision(Vector2Int collisionDir)
    {
        if (hasWallCollisionThisFrame) 
        {
            return;
        }
        
        if (Time.time - lastWallCollisionTime < wallCollisionCooldown)
        {
            return;
        }

        hasWallCollisionThisFrame = true;
        lastWallCollisionTime = Time.time;
        
        isLerping = false;
        transform.position = lastValidPosition;
        currentGridPos = WorldToGridPosition(lastValidPosition);

        if (wallCollisionParticle != null)
        {
            Vector3 collisionPoint = transform.position + new Vector3(collisionDir.x, collisionDir.y, 0) * 0.3f;
            GameObject particleEffect = Instantiate(wallCollisionParticle, collisionPoint, Quaternion.identity);
            
            ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
        }

        if (wallCollisionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wallCollisionSound);
        }
    }

    public void CollectPellet(int points)
    {
        GameStartCountdown countdown = FindObjectOfType<GameStartCountdown>();
        if (countdown != null && countdown.IsCountdownActive())
            return;
        
        if (gameManager != null)
            gameManager.AddScore(points);
    }

    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        isLerping = false;

        if (animator != null)
        {
            animator.Play(DIE_STATE);
        }

        if (deathParticle != null)
        {
            Instantiate(deathParticle, transform.position, Quaternion.identity);
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
    }

    public KeyCode GetCurrentDirection() { return currentInput; }
    public Vector2Int GetCurrentGridPosition() { return currentGridPos; }
}