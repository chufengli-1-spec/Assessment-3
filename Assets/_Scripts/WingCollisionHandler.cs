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
            
            // 获取玩家控制器并应用加速效果
            PacStudentController playerController = other.GetComponent<PacStudentController>();
            if (playerController == null)
            {
                playerController = other.GetComponentInParent<PacStudentController>();
            }
            
            if (playerController != null)
            {
                playerController.ActivateSpeedBoost(5f); // 加速5秒
            }
            
            // 播放收集音效（可选）
            // AudioSource.PlayClipAtPoint(collectSound, transform.position);
            
            // 显示收集特效（可选）
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
        // 这里可以添加粒子效果或动画
        // 例如：Instantiate(collectEffect, transform.position, Quaternion.identity);
        
        // 临时：改变颜色然后消失
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }
}