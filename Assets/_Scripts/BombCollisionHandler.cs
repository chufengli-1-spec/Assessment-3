using UnityEngine;

public class BombCollisionHandler : MonoBehaviour
{
    private BombController bombController;
    private bool hasBeenCollected = false;
    private Collider2D bombCollider;

    public void Initialize(BombController controller)
    {
        bombController = controller;
        bombCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (!gameObject.CompareTag("Bomb"))
        {
            gameObject.tag = "Bomb";
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
            if (bombCollider != null)
            {
                bombCollider.enabled = false;
            }
            
            PacStudentController playerController = other.GetComponent<PacStudentController>();
            if (playerController == null)
            {
                playerController = other.GetComponentInParent<PacStudentController>();
            }
            
            if (playerController != null)
            {
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.PlayerTakeDamage(1); 
                }
            }
            
            ShowExplosionEffect();
            
            if (bombController != null)
            {
                bombController.OnBombCollected();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void ShowExplosionEffect()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.black;
        }
    }
}