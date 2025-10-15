using UnityEngine;

public class PowerPill : MonoBehaviour
{
    [Header("Power Pill Settings")]
    public int scoreValue = 50;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ActivatePowerPillMode();
            }
            
            Collider2D pillCollider = GetComponent<Collider2D>();
            if (pillCollider != null)
            {
                pillCollider.enabled = false;
            }
            
            SpriteRenderer pillRenderer = GetComponent<SpriteRenderer>();
            if (pillRenderer != null)
            {
                pillRenderer.enabled = false;
            }
            
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
            
            Destroy(gameObject, 1f);
        }
    }

    [ContextMenu("Test Power Pill Collection")]
    public void TestPowerPillCollection()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ActivatePowerPillMode();
        }
    }

    private void OnDestroy()
    {
    }
}