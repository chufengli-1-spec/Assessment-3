using UnityEngine;

public class CherryCollisionHandler : MonoBehaviour
{
    private CherryController cherryController;
    private GameManager gameManager;
    private bool hasBeenCollected = false;
    private Collider2D cherryCollider;

    public void Initialize(CherryController controller)
    {
        cherryController = controller;
        gameManager = FindObjectOfType<GameManager>();
        cherryCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (!gameObject.CompareTag("Cherry"))
        {
            gameObject.tag = "Cherry";
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenCollected)
        {
            return;
        }
        
        bool isPlayer = other.CompareTag("Player") || 
                       other.name.ToLower().Contains("sheep") || 
                       other.name.ToLower().Contains("pac") ||
                       other.GetComponent<PacStudentController>() != null;
        
        if (isPlayer) 
        {
            hasBeenCollected = true;
            if (cherryCollider != null)
            {
                cherryCollider.enabled = false;
            }
            
            if (gameManager != null)
            {
                gameManager.AddScore(100);
            }
            else
            {
                gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.AddScore(100);
                }
            }
            
            if (cherryController != null)
            {
                cherryController.OnCherryCollected();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}