using UnityEngine;
using System.Collections;

public class GhostController : MonoBehaviour
{
    public GhostState CurrentState { get; private set; } = GhostState.Normal;
    
    [Header("Animator")]
    public Animator animator;
    
    [Header("Settings")]
    public float normalSpeed = 2f;
    public float scaredSpeed = 1.5f;
    public float deadSpeed = 4f;
    
    [Header("Movement")]
    public Vector2Int currentDirection = Vector2Int.right;
    
    private Vector3 initialPosition;
    private bool isRespawning = false;
    
    // 添加移动组件引用（如果需要）
    private MonoBehaviour movementComponent;
    
    private void Start()
    {
        initialPosition = transform.position;
        
        // 尝试获取任何移动相关的组件
        movementComponent = GetComponent<MonoBehaviour>();
        
        UpdateAnimatorState();
    }
    
    public void SetNormal()
    {
        if (CurrentState == GhostState.Dead) return;
        
        CurrentState = GhostState.Normal;
        // 如果有移动组件，可以在这里设置速度
        UpdateAnimatorState();
    }
    
    public void SetScared()
    {
        if (CurrentState == GhostState.Dead) return;
        
        CurrentState = GhostState.Scared;
        // 如果有移动组件，可以在这里设置速度
        UpdateAnimatorState();
    }
    
    public void SetRecovering()
    {
        if (CurrentState == GhostState.Dead) return;
        
        CurrentState = GhostState.Recovering;
        // 如果有移动组件，可以在这里设置速度
        UpdateAnimatorState();
    }
    
    public void Die()
    {
        CurrentState = GhostState.Dead;
        // 如果有移动组件，可以在这里设置速度
        UpdateAnimatorState();
        
        // 开始重生计时器
        StartCoroutine(RespawnAfterDelay(3f));
    }
    
    private IEnumerator RespawnAfterDelay(float delay)
    {
        isRespawning = true;
        yield return new WaitForSeconds(delay);
        isRespawning = false;
        
        // 返回初始位置
        transform.position = initialPosition;
        
        // 根据能量丸状态决定新状态
        GameManager gameManager = FindObjectOfType<GameManager>();
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
    }
    
    public void ResetToInitialPosition()
    {
        transform.position = initialPosition;
        SetNormal();
    }
    
    private void UpdateAnimatorState()
    {
        if (animator != null)
        {
            animator.SetInteger("GhostState", (int)CurrentState);
        }
    }
    
    // 简单的移动方向获取
    public Vector2Int GetCurrentDirection()
    {
        return currentDirection;
    }
    
    // 设置速度的方法（如果需要）
    public void SetSpeed(float speed)
    {
        // 这里可以添加移动逻辑
        // 例如：如果有Rigidbody，可以设置速度
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 根据当前方向设置速度
            rb.velocity = new Vector2(currentDirection.x, currentDirection.y) * speed;
        }
    }
}

public enum GhostState
{
    Normal = 0,
    Scared = 1,
    Recovering = 2,
    Dead = 3
}