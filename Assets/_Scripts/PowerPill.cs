using UnityEngine;

public class PowerPill : MonoBehaviour
{
    [Header("Power Pill Settings")]
    public int scoreValue = 50;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"=== POWER PILL COLLISION START ===");
            Debug.Log($"PowerPill: Collision detected with {collision.name} (Tag: {collision.tag})");
            
            // 获取 GameManager 并激活能量丸模式（不在脚本中处理分数）
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                Debug.Log($"PowerPill: Activating power mode only - score is handled by PacStudentController");
                // 只激活能量丸模式，分数由 PacStudentController 处理
                gameManager.ActivatePowerPillMode();
            }
            else
            {
                Debug.LogError("PowerPill: GameManager not found!");
            }
            
            // 立即禁用碰撞器防止重复触发
            Collider2D pillCollider = GetComponent<Collider2D>();
            if (pillCollider != null)
            {
                pillCollider.enabled = false;
                Debug.Log("PowerPill: Collider disabled");
            }
            
            // 隐藏渲染器
            SpriteRenderer pillRenderer = GetComponent<SpriteRenderer>();
            if (pillRenderer != null)
            {
                pillRenderer.enabled = false;
                Debug.Log("PowerPill: Renderer disabled");
            }
            
            // 播放收集音效
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
                Debug.Log("PowerPill: Audio played");
            }
            else
            {
                Debug.Log("PowerPill: No audio source found");
            }
            
            Debug.Log($"=== POWER PILL COLLECTION COMPLETE ===");
            
            // 延迟销毁以播放音效
            Destroy(gameObject, 1f);
        }
        else
        {
            Debug.Log($"PowerPill: Collision with non-player object: {collision.name} (Tag: {collision.tag})");
        }
    }

    // 添加一个方法来手动测试能量丸功能
    [ContextMenu("Test Power Pill Collection")]
    public void TestPowerPillCollection()
    {
        Debug.Log("=== MANUAL POWER PILL TEST ===");
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ActivatePowerPillMode();
            Debug.Log("Manual test: Power mode activated via PowerPill script");
        }
        else
        {
            Debug.LogError("Manual test: GameManager not found!");
        }
    }

    // 在销毁时记录
    private void OnDestroy()
    {
        Debug.Log("PowerPill: Object destroyed");
    }
}