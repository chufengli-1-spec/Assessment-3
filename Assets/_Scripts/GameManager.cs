using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text scoreText;
    public Text livesText;
    public Text ghostTimerText;
    public GameObject ghostTimerPanel;
    
    [Header("Audio")]
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip ghostEatenMusic;
    
    [Header("Game Settings")]
    public int startingLives = 3;
    
    private int score = 0;
    private int lives;
    private AudioSource audioSource;
    private float powerPillTimer = 0f;
    private bool isPowerPillActive = false;
    
    public bool IsPowerPillActive { get { return isPowerPillActive; } }
    public float PowerPillTimeRemaining { get { return powerPillTimer; } }
    
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
        
        // 播放初始音乐
        if (normalMusic != null)
        {
            audioSource.clip = normalMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    private void Update()
    {
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
        isPowerPillActive = true;
        powerPillTimer = 10f;
        
        // 改变幽灵状态为害怕
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts)
        {
            if (ghost.CurrentState != GhostState.Dead)
            {
                ghost.SetScared();
            }
        }
        
        // 改变背景音乐
        if (scaredMusic != null)
        {
            audioSource.clip = scaredMusic;
            audioSource.Play();
        }
        
        // 显示幽灵计时器UI
        if (ghostTimerPanel != null)
        {
            ghostTimerPanel.SetActive(true);
            UpdateGhostTimerUI();
        }
    }
    
    private void UpdatePowerPillTimer()
    {
        powerPillTimer -= Time.deltaTime;
        UpdateGhostTimerUI();
        
        if (powerPillTimer <= 3f)
        {
            // 设置幽灵为恢复状态
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
            
            // 重置幽灵为正常状态（除了死亡的）
            GhostController[] ghosts = FindObjectsOfType<GhostController>();
            foreach (GhostController ghost in ghosts)
            {
                if (ghost.CurrentState != GhostState.Dead)
                {
                    ghost.SetNormal();
                }
            }
            
            // 改变背景音乐回正常
            if (normalMusic != null)
            {
                audioSource.clip = normalMusic;
                audioSource.Play();
            }
            
            // 隐藏幽灵计时器UI
            if (ghostTimerPanel != null)
            {
                ghostTimerPanel.SetActive(false);
            }
        }
    }
    
    private void UpdateGhostTimerUI()
    {
        if (ghostTimerText != null)
        {
            ghostTimerText.text = Mathf.CeilToInt(powerPillTimer).ToString();
        }
    }
    
    public void PacStudentDied()
    {
        lives--;
        UpdateUI();
        
        if (lives <= 0)
        {
            GameOver();
        }
        else
        {
            // 3秒后重生
            Invoke("RespawnPacStudent", 3f);
        }
    }
    
    private void RespawnPacStudent()
    {
        PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
        if (pacStudent != null)
        {
            pacStudent.Respawn();
        }
        
        // 重置幽灵到初始位置和状态
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts)
        {
            ghost.ResetToInitialPosition();
        }
    }
    
    public void PlayGhostEatenMusic()
    {
        if (ghostEatenMusic != null)
        {
            audioSource.clip = ghostEatenMusic;
            audioSource.Play();
        }
        
        // 3秒后回到害怕音乐（如果能量丸效果还在）
        Invoke("ReturnToScaredMusic", 3f);
    }
    
    private void ReturnToScaredMusic()
    {
        if (isPowerPillActive && scaredMusic != null)
        {
            audioSource.clip = scaredMusic;
            audioSource.Play();
        }
        else if (normalMusic != null)
        {
            audioSource.clip = normalMusic;
            audioSource.Play();
        }
    }
    
    public void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
        if (livesText != null)
            livesText.text = "Lives: " + lives;
    }
    
    private void GameOver()
    {
        // 处理游戏结束逻辑
        Debug.Log("Game Over! Final Score: " + score);
        Time.timeScale = 0; // 暂停游戏
    }
}