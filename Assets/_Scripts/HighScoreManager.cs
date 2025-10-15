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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadHighScore();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        HighScoreTime = PlayerPrefs.GetFloat(HIGH_SCORE_TIME_KEY, 0f);
    }
    
    public bool CheckAndSaveHighScore(int score, float time)
    {
        bool isNewHighScore = false;
        
        if (score > HighScore)
        {
            isNewHighScore = true;
        }
        else if (score == HighScore && (time < HighScoreTime || HighScoreTime == 0f))
        {
            isNewHighScore = true;
        }
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
            PlayerPrefs.Save();
            
            return true;
        }
        
        return false;
    }
    
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
    
    public string GetHighScoreDisplay()
    {
        return $"High Score: {HighScore}";
    }
    
    public string GetHighScoreTimeDisplay()
    {
        return $"Best Time: {FormatTime(HighScoreTime)}";
    }
    
    [ContextMenu("Debug High Score Info")]
    public void DebugHighScoreInfo()
    {
    }
    
    [ContextMenu("Reset High Score")]
    public void ResetHighScore()
    {
        HighScore = 0;
        HighScoreTime = 0f;
        PlayerPrefs.DeleteKey(HIGH_SCORE_KEY);
        PlayerPrefs.DeleteKey(HIGH_SCORE_TIME_KEY);
        PlayerPrefs.Save();
    }
}