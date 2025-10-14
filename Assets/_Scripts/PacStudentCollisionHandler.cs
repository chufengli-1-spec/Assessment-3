using UnityEngine;

public class PacStudentCollisionHandler : MonoBehaviour
{
    private PacStudentController pacStudent;
    private GameManager gameManager;
    
    private void Start()
    {
        pacStudent = GetComponent<PacStudentController>();
        gameManager = FindObjectOfType<GameManager>();
        
        if (pacStudent == null)
        {
            Debug.LogError("PacStudentController not found!");
        }
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in scene!");
        }
        
        Debug.Log("PacStudentCollisionHandler initialized - Cherry handling disabled");
    }
    
    // 只处理触发碰撞（用于收集物品和幽灵）
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pacStudent == null || pacStudent.IsDead) return;
        
        // 调试信息：显示所有碰撞
        Debug.Log($"PacStudentCollisionHandler: Collided with {other.name} (Tag: {other.tag})");
        
        switch (other.tag)
        {
            case "Pellet":
                HandlePelletCollision(other.gameObject);
                break;
                
            // 樱桃碰撞完全禁用 - 由 CherryCollisionHandler 处理
            case "Cherry":
                Debug.Log("Cherry collision detected in PacStudentCollisionHandler - IGNORED (handled by CherryCollisionHandler)");
                break;
                
            case "PowerPill":
                HandlePowerPillCollision(other.gameObject);
                break;
                
            case "Ghost":
                HandleGhostCollision(other.gameObject);
                break;
                
            default:
                Debug.Log($"Unknown collision tag: {other.tag}");
                break;
        }
    }
    
    private void HandlePelletCollision(GameObject pellet)
    {
        // 确保豆子被销毁
        if (pellet != null)
        {
            Destroy(pellet);
        }
        
        // 加分
        if (gameManager != null)
        {
            gameManager.AddScore(10);
            Debug.Log("Pellet collected! +10 points");
        }
        else if (pacStudent != null)
        {
            // 备用方案
            pacStudent.CollectPellet(10);
            Debug.Log("Pellet collected! +10 points (via backup)");
        }
        else
        {
            Debug.LogWarning("Pellet collected but no way to add score!");
        }
    }
    
    // 完全移除 HandleCherryCollision 方法以防止任何可能的调用
    // 樱桃碰撞由 CherryCollisionHandler 专门处理
    
    private void HandlePowerPillCollision(GameObject powerPill)
    {
        // 销毁能量丸
        if (powerPill != null)
        {
            Destroy(powerPill);
        }
        
        // 加分
        if (gameManager != null)
        {
            gameManager.AddScore(50);
            Debug.Log("Power Pill collected! +50 points");
        }
        else if (pacStudent != null)
        {
            // 备用方案
            pacStudent.CollectPellet(50);
            Debug.Log("Power Pill collected! +50 points (via backup)");
        }
        
        // 激活能量丸模式
        if (gameManager != null)
        {
            gameManager.ActivatePowerPillMode();
            Debug.Log("Power Pill mode activated!");
        }
        
        Debug.Log("Power Pill collected! Ghosts are now scared.");
    }
    
    private void HandleGhostCollision(GameObject ghost)
    {
        GhostController ghostController = ghost.GetComponent<GhostController>();
        
        if (ghostController != null && pacStudent != null)
        {
            Debug.Log($"Ghost collision - State: {ghostController.CurrentState}");
            
            switch (ghostController.CurrentState)
            {
                case GhostState.Normal:
                    HandleGhostNormalCollision(ghostController);
                    break;
                    
                case GhostState.Scared:
                case GhostState.Recovering:
                    HandleGhostScaredCollision(ghostController);
                    break;
                    
                case GhostState.Dead:
                    // 死亡幽灵无碰撞响应
                    Debug.Log("Collided with dead ghost - no effect");
                    break;
            }
        }
        else
        {
            Debug.LogWarning("Ghost collision but missing components");
        }
    }
    
    private void HandleGhostNormalCollision(GhostController ghost)
    {
        // PacStudent 死亡
        Debug.Log("Collided with normal ghost - PacStudent dies");
        pacStudent.Die();
    }
    
    private void HandleGhostScaredCollision(GhostController ghost)
    {
        // 幽灵死亡
        Debug.Log("Collided with scared ghost - ghost dies");
        ghost.Die();
        
        // 加分
        if (gameManager != null)
        {
            gameManager.AddScore(300);
            Debug.Log("Scared ghost eaten! +300 points");
        }
        else if (pacStudent != null)
        {
            // 备用方案
            pacStudent.CollectPellet(300);
            Debug.Log("Scared ghost eaten! +300 points (via backup)");
        }
        
        
    }
    
    // 调试方法：检查当前状态
    public void DebugStatus()
    {
        Debug.Log($"PacStudentCollisionHandler Status:");
        Debug.Log($"- PacStudent: {pacStudent != null}");
        Debug.Log($"- GameManager: {gameManager != null}");
        Debug.Log($"- PacStudent Alive: {pacStudent != null && !pacStudent.IsDead}");
    }
}