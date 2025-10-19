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
            
            // 对玩家造成伤害
            PacStudentController playerController = other.GetComponent<PacStudentController>();
            if (playerController == null)
            {
                playerController = other.GetComponentInParent<PacStudentController>();
            }
            
            if (playerController != null)
            {
                // 调用游戏管理器扣血
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.PlayerTakeDamage(1); // 扣一格血
                }
            }
            
            // 播放爆炸音效和特效
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
        // 这里可以添加爆炸粒子效果
        // 例如：Instantiate(explosionEffect, transform.position, Quaternion.identity);
        
        // 临时：改变颜色表示爆炸
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.black;
        }
    }
}