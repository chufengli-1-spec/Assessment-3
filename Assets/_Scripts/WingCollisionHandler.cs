using UnityEngine;
using System.Collections;

public class WingCollisionHandler : MonoBehaviour
{
    private WingController wingController;
    private bool hasBeenCollected = false;
    private Collider2D wingCollider;

    public void Initialize(WingController controller)
    {
        wingController = controller;
        wingCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (!gameObject.CompareTag("Wing"))
        {
            gameObject.tag = "Wing";
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
            if (wingCollider != null)
            {
                wingCollider.enabled = false;
            }
            
            PacStudentController playerController = other.GetComponent<PacStudentController>();
            if (playerController == null)
            {
                playerController = other.GetComponentInParent<PacStudentController>();
            }
            
            if (playerController != null)
            {
                playerController.ActivateSpeedBoost(5f); 
            }
            
            ShowCollectEffect();
            
            if (wingController != null)
            {
                wingController.OnWingCollected();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void ShowCollectEffect()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }
}