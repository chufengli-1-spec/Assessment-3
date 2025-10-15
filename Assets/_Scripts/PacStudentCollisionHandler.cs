using UnityEngine;

public class PacStudentCollisionHandler : MonoBehaviour
{
    private PacStudentController pacStudent;
    private GameManager gameManager;
    
    private void Start()
    {
        pacStudent = GetComponent<PacStudentController>();
        gameManager = FindObjectOfType<GameManager>();
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pacStudent == null || pacStudent.IsDead) return;
        
        switch (other.tag)
        {
            case "Pellet":
                HandlePelletCollision(other.gameObject);
                break;
                
            case "Cherry":
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
        if (pellet != null)
        {
            Destroy(pellet);
        }
        
        if (gameManager != null)
        {
            gameManager.AddScore(10);
        }
        else if (pacStudent != null)
        {
            pacStudent.CollectPellet(10);
        }
    }
    
    private void HandlePowerPillCollision(GameObject powerPill)
    {
        if (powerPill != null)
        {
            Destroy(powerPill);
        }
        
        if (gameManager != null)
        {
            gameManager.AddScore(50);
        }
        else if (pacStudent != null)
        {
            pacStudent.CollectPellet(50);
        }
        
        if (gameManager != null)
        {
            gameManager.ActivatePowerPillMode();
        }
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
                    break;
            }
        }
    }
    
    private void HandleGhostNormalCollision(GhostController ghost)
    {
        pacStudent.Die();
    }
    
    private void HandleGhostScaredCollision(GhostController ghost)
    {
        ghost.Die();
        
        if (gameManager != null)
        {
            gameManager.AddScore(300);
        }
        else if (pacStudent != null)
        {
            pacStudent.CollectPellet(300);
        }
    }
    
    public void DebugStatus()
    {
    }
}