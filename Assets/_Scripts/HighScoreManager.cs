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
            DontDestroyOnLoad(gameObject); // 跨场景不销毁
            LoadHighScore();
        }
        else
        {
            Destroy(gameObject); // 如果已存在，销毁新的实例
        }
    }
    
    // 加载最高分
    private void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        HighScoreTime = PlayerPrefs.GetFloat(HIGH_SCORE_TIME_KEY, 0f);
        Debug.Log($"Loaded High Score: {HighScore}, Time: {FormatTime(HighScoreTime)}");
    }
    
    // 检查并保存最高分
    public bool CheckAndSaveHighScore(int score, float time)
    {
        if (score > HighScore || (score == HighScore && time < HighScoreTime))
        {
            HighScore = score;
            HighScoreTime = time;
            
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
            PlayerPrefs.SetFloat(HIGH_SCORE_TIME_KEY, time);
            PlayerPrefs.Save();
            
            Debug.Log($"NEW HIGH SCORE! {score} in {FormatTime(time)}");
            return true;
        }
        return false;
    }
    
    // 格式化时间
    public string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int centiseconds = Mathf.FloorToInt((time * 100f) % 100f);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centiseconds);
    }
    
    // 获取最高分信息
    public string GetHighScoreDisplay()
    {
        return $"High Score: {HighScore}";
    }
    
    public string GetHighScoreTimeDisplay()
    {
        return HighScoreTime > 0f ? $"Best Time: {FormatTime(HighScoreTime)}" : "Best Time: --:--:--";
    }
}