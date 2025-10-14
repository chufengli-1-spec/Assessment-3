using UnityEngine;
using System.Collections;

public class GhostController : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float normalSpeed = 2f;
    public float scaredSpeed = 1.5f;
    public float recoveringSpeed = 1.8f;
    
    [Header("Animation")]
    public Animator animator;
    
    private GhostState currentState = GhostState.Normal;
    private Vector3 initialPosition;
    private float currentSpeed;
    
    public GhostState CurrentState { get { return currentState; } }
    
    void Start()
    {
        initialPosition = transform.position;
        currentSpeed = normalSpeed;
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // 初始状态设置为 Normal
        SetNormal();
    }
    
    void Update()
    {
        // 注释掉移动，敌人原地不动
        // MoveGhost();
    }
    
    public void SetNormal()
    {
        if (currentState == GhostState.Dead) return;
        
        currentState = GhostState.Normal;
        currentSpeed = normalSpeed;
        UpdateAnimator();
    }
    
    public void SetScared()
    {
        if (currentState == GhostState.Dead) return;
        
        currentState = GhostState.Scared;
        currentSpeed = scaredSpeed;
        UpdateAnimator();
    }
    
    public void SetRecovering()
    {
        if (currentState == GhostState.Dead) return;
        
        currentState = GhostState.Recovering;
        currentSpeed = recoveringSpeed;
        UpdateAnimator();
    }
    
    public void SetDead()
    {
        if (currentState == GhostState.Dead) return;
        
        Debug.Log($"{gameObject.name}: Setting state to Dead");
        currentState = GhostState.Dead;
        UpdateAnimator();
        
        // 3秒后复活
        StartCoroutine(RespawnAfterDelay(3f));
    }
    
    public void Die()
    {
        SetDead();
    }
    
    private IEnumerator RespawnAfterDelay(float delay)
    {
        Debug.Log($"{gameObject.name}: Starting respawn timer for {delay} seconds");
        
        yield return new WaitForSeconds(delay);
        
        if (currentState == GhostState.Dead)
        {
            Debug.Log($"{gameObject.name}: Respawning ghost");
            
            ResetToInitialPosition();
            
            // 检查能量丸效果是否还在
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && gameManager.IsPowerPillActive)
            {
                Debug.Log($"{gameObject.name}: Power pill still active, checking remaining time: {gameManager.PowerPillTimeRemaining}");
                
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
        }
    }
    
    private void UpdateAnimator()
    {
        if (animator == null) return;

        bool isNormal = (currentState == GhostState.Normal);
        bool isScared = (currentState == GhostState.Scared);
        bool isRecovering = (currentState == GhostState.Recovering);
        bool isDead = (currentState == GhostState.Dead);

        animator.SetBool("Normal", isNormal);
        animator.SetBool("Scared", isScared);
        animator.SetBool("Recovering", isRecovering);
        animator.SetBool("Dead", isDead);
        
        Debug.Log($"{gameObject.name}: Animator updated - Normal: {isNormal}, Scared: {isScared}, Recovering: {isRecovering}, Dead: {isDead}");
    }
    
    private void MoveGhost()
    {
        // 注释移动逻辑，敌人原地不动
        // transform.Translate(Vector3.right * currentSpeed * Time.deltaTime);
    }
    
    public void ResetToInitialPosition()
    {
        transform.position = initialPosition;
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HandlePlayerCollision();
        }
    }
    
    private void HandlePlayerCollision()
{
    Debug.Log($"{gameObject.name}: HandlePlayerCollision - Current state: {currentState}");
    
    switch (currentState)
    {
        case GhostState.Normal:
            // PacStudent 死亡
            Debug.Log($"{gameObject.name}: Normal state - PacStudent should die");
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.PacStudentDied();
            }
            break;
            
        case GhostState.Scared:
        case GhostState.Recovering:
            // 幽灵被吃 - 按照新要求处理
            Debug.Log($"{gameObject.name}: Scared/Recovering state - Ghost eaten, adding 300 points");
            SetDead();
            
            // 通知 GameManager 加分（不需要播放音乐）
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.AddScore(300); // 改为300分
                // 移除了 PlayGhostEatenMusic() 调用
            }
            break;
            
        case GhostState.Dead:
            // 死亡状态不处理碰撞
            Debug.Log($"{gameObject.name}: Dead state - Ignoring collision");
            break;
    }
}
}

public enum GhostState
{
    Normal,
    Scared,
    Recovering,
    Dead
}