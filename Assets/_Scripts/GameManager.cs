using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text scoreText;
    public GameObject livesContainer;
    public Text gameTimerText;
    public Text ghostScaredTimerText;
    public GameObject ghostTimerPanel;
    
    [Header("Audio")]
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip ghostEatenMusic;
    
    [Header("Game Settings")]
    public int startingLives = 3;
    public float deathSequenceDuration = 3f;
    
    private int score = 0;
    private int lives;
    private AudioSource audioSource;
    private float powerPillTimer = 0f;
    private bool isPowerPillActive = false;
    private bool isDeathSequenceActive = false;
    
    // 游戏计时相关
    private float gameTime = 0f;
    private bool isGameRunning = true;
    
    public bool IsPowerPillActive { get { return isPowerPillActive; } }
    public float PowerPillTimeRemaining { get { return powerPillTimer; } }
    public bool IsDeathSequenceActive { get { return isDeathSequenceActive; } }
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        lives = startingLives;
        UpdateUI();
        
        if (ghostTimerPanel != null)
            ghostTimerPanel.SetActive(false);
        
        UpdateLivesDisplay();
        
        if (normalMusic != null)
        {
            audioSource.clip = normalMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    private void Update()
    {
        if (isGameRunning && !isDeathSequenceActive)
        {
            gameTime += Time.deltaTime;
            UpdateGameTimerUI();
        }
        
        if (isPowerPillActive)
        {
            UpdatePowerPillTimer();
        }
    }
    
    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }
    
    public void ActivatePowerPillMode()
    {
        if (isDeathSequenceActive) return;
        
        isPowerPillActive = true;
        powerPillTimer = 10f;
        
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts)
        {
            if (ghost.CurrentState != GhostState.Dead)
            {
                ghost.SetScared();
            }
        }
        
        if (scaredMusic != null)
        {
            audioSource.clip = scaredMusic;
            audioSource.Play();
        }
        
        UpdateGhostScaredTimerUI();
    }
    
    private void UpdatePowerPillTimer()
    {
        powerPillTimer -= Time.deltaTime;
        UpdateGhostScaredTimerUI();
        
        if (powerPillTimer <= 3f)
        {
            GhostController[] ghosts = FindObjectsOfType<GhostController>();
            foreach (GhostController ghost in ghosts)
            {
                if (ghost.CurrentState == GhostState.Scared)
                {
                    ghost.SetRecovering();
                }
            }
        }
        
        if (powerPillTimer <= 0f)
        {
            isPowerPillActive = false;
            powerPillTimer = 0f;
            
            GhostController[] ghosts = FindObjectsOfType<GhostController>();
            foreach (GhostController ghost in ghosts)
            {
                if (ghost.CurrentState != GhostState.Dead)
                {
                    ghost.SetNormal();
                }
            }
            
            if (normalMusic != null)
            {
                audioSource.clip = normalMusic;
                audioSource.Play();
            }
            
            UpdateGhostScaredTimerUI();
        }
    }
    
    private IEnumerator DeathSequence()
{
    isDeathSequenceActive = true;
    Debug.Log("=== DEATH SEQUENCE STARTED ===");
    
    // 先触发 PacStudent 的死亡动画
    PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
    if (pacStudent != null)
    {
        pacStudent.Die(); // 这会播放死亡动画和粒子效果
    }
    
    // 等待死亡动画播放完成（3秒）
    yield return new WaitForSeconds(deathSequenceDuration);
    
    // 检查是否还有生命值
    if (lives <= 0)
    {
        // 没有生命了，游戏结束
        Debug.Log("No lives remaining, game over");
        GameOver();
    }
    else
    {
        // 还有生命，重置游戏状态
        Debug.Log("Respawning with remaining lives");
        
        // 重置 PacStudent
        if (pacStudent != null)
        {
            pacStudent.Respawn();
        }
        
        // 重置幽灵到初始位置和状态
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts)
        {
            ghost.ResetToInitialPosition();
            ghost.SetNormal();
        }
    }
    
    isDeathSequenceActive = false;
    Debug.Log("=== DEATH SEQUENCE COMPLETED ===");
}

public void PacStudentDied()
{
    if (isDeathSequenceActive) return;
    
    lives--;
    UpdateUI();
    UpdateLivesDisplay();
    
    Debug.Log($"PacStudent died! Lives remaining: {lives}");
    
    // 无论是否游戏结束，都要播放死亡动画
    StartCoroutine(DeathSequence());
}
    
    private void UpdateGameTimerUI()
    {
        if (gameTimerText != null)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            gameTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    private void UpdateGhostScaredTimerUI()
    {
        if (ghostScaredTimerText != null)
        {
            if (isPowerPillActive)
            {
                ghostScaredTimerText.text = Mathf.CeilToInt(powerPillTimer).ToString();
                ghostScaredTimerText.gameObject.SetActive(true);
            }
            else
            {
                ghostScaredTimerText.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateLivesDisplay()
    {
        if (livesContainer != null)
        {
            int childCount = livesContainer.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject lifeIcon = livesContainer.transform.GetChild(i).gameObject;
                lifeIcon.SetActive(i < lives);
            }
        }
    }
    
   
    
    public void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }
    
    private void GameOver()
    {
        isGameRunning = false;
        Debug.Log("Game Over! Final Score: " + score + " Time: " + Mathf.FloorToInt(gameTime) + "s");
        Time.timeScale = 0;
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1;
        lives = startingLives;
        score = 0;
        gameTime = 0f;
        isGameRunning = true;
        isPowerPillActive = false;
        isDeathSequenceActive = false;
        
        UpdateUI();
        UpdateLivesDisplay();
        UpdateGameTimerUI();
        UpdateGhostScaredTimerUI();
    }
}