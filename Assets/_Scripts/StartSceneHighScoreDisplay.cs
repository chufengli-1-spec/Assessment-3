using UnityEngine;
using UnityEngine.UI;

public class StartSceneHighScoreDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Text highScoreText;
    public Text highScoreTimeText;
    
    void Start()
    {
        UpdateHighScoreDisplay();
    }
    
    void UpdateHighScoreDisplay()
    {
        // 检查是否有HighScoreManager实例
        if (HighScoreManager.Instance != null)
        {
            if (highScoreText != null)
                highScoreText.text = HighScoreManager.Instance.GetHighScoreDisplay();
            
            if (highScoreTimeText != null)
                highScoreTimeText.text = HighScoreManager.Instance.GetHighScoreTimeDisplay();
            
            Debug.Log("High score display updated successfully");
        }
        else
        {
            Debug.LogWarning("HighScoreManager not found. Creating one...");
            
            // 如果没有找到，创建一个HighScoreManager
            GameObject highScoreManager = new GameObject("HighScoreManager");
            highScoreManager.AddComponent<HighScoreManager>();
            
            // 延迟一帧后再次尝试更新显示
            StartCoroutine(DelayedUpdate());
        }
    }
    
    private System.Collections.IEnumerator DelayedUpdate()
    {
        yield return null; // 等待一帧
        UpdateHighScoreDisplay();
    }
    
    // 可选：添加刷新按钮功能
    public void RefreshDisplay()
    {
        UpdateHighScoreDisplay();
    }
}