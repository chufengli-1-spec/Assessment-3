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
        if (HighScoreManager.Instance != null)
        {
            if (highScoreText != null)
                highScoreText.text = HighScoreManager.Instance.GetHighScoreDisplay();
            
            if (highScoreTimeText != null)
                highScoreTimeText.text = HighScoreManager.Instance.GetHighScoreTimeDisplay();
        }
        else
        {
            GameObject highScoreManager = new GameObject("HighScoreManager");
            highScoreManager.AddComponent<HighScoreManager>();
            
            StartCoroutine(DelayedUpdate());
        }
    }
    
    private System.Collections.IEnumerator DelayedUpdate()
    {
        yield return null;
        UpdateHighScoreDisplay();
    }
    
    public void RefreshDisplay()
    {
        UpdateHighScoreDisplay();
    }
}