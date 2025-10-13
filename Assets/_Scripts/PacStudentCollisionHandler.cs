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
    }
    
    // 只处理触发碰撞（用于收集物品和幽灵）
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pacStudent == null || pacStudent.IsDead) return;
        
        switch (other.tag)
        {
            case "Pellet":
                HandlePelletCollision(other.gameObject);
                break;
                
            case "Cherry":
                HandleCherryCollision(other.gameObject);
                break;
                
            case "PowerPill":
                HandlePowerPillCollision(other.gameObject);
                break;
                
            case "Ghost":
                HandleGhostCollision(other.gameObject);
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
        if (pacStudent != null)
        {
            pacStudent.CollectPellet(10);
        }
        
        Debug.Log("Pellet collected!");
    }
    
    private void HandleCherryCollision(GameObject cherry)
    {
        // 销毁樱桃
        if (cherry != null)
        {
            Destroy(cherry);
        }
        
        // 加分
        if (pacStudent != null)
        {
            pacStudent.CollectPellet(100);
        }
        
        Debug.Log("Cherry collected! +100 points");
    }
    
    private void HandlePowerPillCollision(GameObject powerPill)
    {
        // 销毁能量丸
        if (powerPill != null)
        {
            Destroy(powerPill);
        }
        
        // 加分
        if (pacStudent != null)
        {
            pacStudent.CollectPellet(50);
        }
        
        // 激活能量丸模式
        if (gameManager != null)
        {
            gameManager.ActivatePowerPillMode();
        }
        
        Debug.Log("Power Pill collected! Ghosts are now scared.");
    }
    
    private void HandleGhostCollision(GameObject ghost)
    {
        GhostController ghostController = ghost.GetComponent<GhostController>();
        
        if (ghostController != null && pacStudent != null)
        {
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
        if (pacStudent != null)
        {
            pacStudent.CollectPellet(300);
        }
        
        // 播放幽灵被吃音乐
        if (gameManager != null)
        {
            gameManager.PlayGhostEatenMusic();
        }
    }
}