using UnityEngine;

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }
    
    private const string HIGH_SCORE_KEY = "HighScore";
    private const string HIGH_SCORE_TIME_KEY = "HighScoreTime";
    
    public int HighScore { get; private set; }
    public float HighScoreTime { get; private set; }
    
    private void Awake()
    {
        // 单例模式，确保只有一个实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadHighScore();
            Debug.Log($"HighScoreManager initialized. Score: {HighScore}, Time: {HighScoreTime}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 加载最高分
    private void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        HighScoreTime = PlayerPrefs.GetFloat(HIGH_SCORE_TIME_KEY, 0f);
        
        // 调试信息
        Debug.Log($"PlayerPrefs - HighScore: {PlayerPrefs.GetInt(HIGH_SCORE_KEY, -1)}, HighScoreTime: {PlayerPrefs.GetFloat(HIGH_SCORE_TIME_KEY, -1f)}");
        Debug.Log($"Loaded - HighScore: {HighScore}, HighScoreTime: {HighScoreTime}");
    }
    
    // 检查并保存最高分
    public bool CheckAndSaveHighScore(int score, float time)
    {
        Debug.Log($"Checking high score - Current: {score} (time: {time}), Previous: {HighScore} (time: {HighScoreTime})");
        
        bool isNewHighScore = false;
        
        // 如果当前分数更高
        if (score > HighScore)
        {
            isNewHighScore = true;
        }
        // 如果分数相同但时间更短（或者之前没有记录）
        else if (score == HighScore && (time < HighScoreTime || HighScoreTime == 0f))
        {
            isNewHighScore = true;
        }
        // 如果之前没有记录，任何分数都是新高分
        else if (HighScore == 0 && score > 0)
        {
            isNewHighScore = true;
        }
        
        if (isNewHighScore)
        {
            HighScore = score;
            HighScoreTime = time;
            
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
            PlayerPrefs.SetFloat(HIGH_SCORE_TIME_KEY, time);
            PlayerPrefs.Save(); // 移除了错误的bool赋值
            
            Debug.Log($"NEW HIGH SCORE SAVED! Score: {score}, Time: {FormatTime(time)}");
            Debug.Log($"Save successful");
            
            return true;
        }
        
        Debug.Log($"No new high score. Current best: {HighScore} in {FormatTime(HighScoreTime)}");
        return false;
    }
    
    // 格式化时间 - 总是返回00:00:00格式
    public string FormatTime(float time)
    {
        if (time <= 0f) 
        {
            return "00:00:00";
        }
        
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int centiseconds = Mathf.FloorToInt((time * 100f) % 100f);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centiseconds);
    }
    
    // 获取最高分显示文本
    public string GetHighScoreDisplay()
    {
        return $"High Score: {HighScore}";
    }
    
    // 获取最高分时间显示文本 - 总是显示时间格式
    public string GetHighScoreTimeDisplay()
    {
        return $"Best Time: {FormatTime(HighScoreTime)}";
    }
    
    // 调试方法：显示当前保存的数据
    [ContextMenu("Debug High Score Info")]
    public void DebugHighScoreInfo()
    {
        Debug.Log($"=== HIGH SCORE DEBUG INFO ===");
        Debug.Log($"HighScore: {HighScore}");
        Debug.Log($"HighScoreTime: {HighScoreTime}");
        Debug.Log($"PlayerPrefs HighScore: {PlayerPrefs.GetInt(HIGH_SCORE_KEY, -1)}");
        Debug.Log($"PlayerPrefs HighScoreTime: {PlayerPrefs.GetFloat(HIGH_SCORE_TIME_KEY, -1f)}");
        Debug.Log($"Formatted Time: {FormatTime(HighScoreTime)}");
    }
    
    // 重置最高分（用于测试）
    [ContextMenu("Reset High Score")]
    public void ResetHighScore()
    {
        HighScore = 0;
        HighScoreTime = 0f;
        PlayerPrefs.DeleteKey(HIGH_SCORE_KEY);
        PlayerPrefs.DeleteKey(HIGH_SCORE_TIME_KEY);
        PlayerPrefs.Save();
        Debug.Log("High score reset to 0");
    }
}