using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text scoreText;
    public GameObject livesContainer;
    public Text gameTimerText;
    public Text ghostScaredTimerText;
    public GameObject ghostTimerPanel;
    
    [Header("Game Start UI")]
    public GameObject blockingImage;
    public TextMeshProUGUI countdownText;
    public float countdownInterval = 1f;
    
    [Header("Audio")]
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip ghostEatenMusic;
    
    [Header("Game Settings")]
    public int startingLives = 3;
    public float deathSequenceDuration = 3f;
    
    [Header("Game References")]
    public PacStudentController playerController;
    
    // 游戏状态
    private int score = 0;
    private int lives;
    private AudioSource audioSource;
    private float powerPillTimer = 0f;
    private bool isPowerPillActive = false;
    private bool isDeathSequenceActive = false;
    private bool isGameRunning = false;
    private bool isCountdownActive = false;
    private float gameTime = 0f;
    
    public bool IsPowerPillActive { get { return isPowerPillActive; } }
    public float PowerPillTimeRemaining { get { return powerPillTimer; } }
    public bool IsDeathSequenceActive { get { return isDeathSequenceActive; } }
    public bool IsGameStarted { get { return isGameRunning && !isCountdownActive; } }
    
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
        
        // 初始化游戏开始UI
        InitializeGameStartUI();
        
        // 开始倒计时
        StartGameCountdown();
    }
    
    private void InitializeGameStartUI()
    {
        if (blockingImage != null) 
            blockingImage.SetActive(false);
        if (countdownText != null) 
            countdownText.gameObject.SetActive(false);
    }
    
    private void StartGameCountdown()
    {
        if (!isCountdownActive)
        {
            StartCoroutine(CountdownRoutine());
        }
    }
    
    private IEnumerator CountdownRoutine()
    {
        isCountdownActive = true;
        isGameRunning = false;
        gameTime = 0f;
        UpdateGameTimerUI();
        
        Debug.Log("Starting game countdown...");
        
        // 显示UI元素
        if (blockingImage != null) 
            blockingImage.SetActive(true);
        if (countdownText != null) 
            countdownText.gameObject.SetActive(true);
        
        // 禁用玩家控制
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // 禁用幽灵移动
        SetGhostsActive(false);
        
        // 倒计时：3
        if (countdownText != null) 
            countdownText.text = "3";
        yield return new WaitForSeconds(countdownInterval);
        
        // 倒计时：2
        if (countdownText != null) 
            countdownText.text = "2";
        yield return new WaitForSeconds(countdownInterval);
        
        // 倒计时：1
        if (countdownText != null) 
            countdownText.text = "1";
        yield return new WaitForSeconds(countdownInterval);
        
        // 显示GO!
        if (countdownText != null) 
            countdownText.text = "GO!";
        yield return new WaitForSeconds(countdownInterval);
        
        // 隐藏UI元素
        if (blockingImage != null) 
            blockingImage.SetActive(false);
        if (countdownText != null) 
            countdownText.gameObject.SetActive(false);
        
        // 开始游戏
        OnGameStart();
        
        isCountdownActive = false;
    }
    
    public void OnGameStart()
    {
        isGameRunning = true;
        gameTime = 0f;
        
        Debug.Log("Game Started!");
        
        // 启用玩家控制
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // 启用幽灵移动
        SetGhostsActive(true);
        
        // 开始背景音乐
        if (normalMusic != null)
        {
            audioSource.clip = normalMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    private void SetGhostsActive(bool active)
    {
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts)
        {
            if (active)
            {
                ghost.enabled = true;
                ghost.SetNormal();
            }
            else
            {
                ghost.enabled = false;
            }
        }
    }
    
    private void Update()
    {
        if (isGameRunning && !isDeathSequenceActive && !isCountdownActive)
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
        GameStartCountdown countdown = FindObjectOfType<GameStartCountdown>();
        if (countdown != null && countdown.IsCountdownActive())
            return;
        
        score += points;
        UpdateUI();
    }
    
    public void ActivatePowerPillMode()
    {
        if (isDeathSequenceActive || !isGameRunning) return;
        
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
            
            if (normalMusic != null && isGameRunning)
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
        
        // 等待死亡动画播放完成
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
            
            // 重新开始倒计时（短暂暂停后继续游戏）
            yield return new WaitForSeconds(1f);
            StartCoroutine(RespawnCountdown());
        }
        
        isDeathSequenceActive = false;
        Debug.Log("=== DEATH SEQUENCE COMPLETED ===");
    }
    
    private IEnumerator RespawnCountdown()
    {
        // 简短的复活倒计时
        if (countdownText != null) 
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "READY!";
            yield return new WaitForSeconds(1f);
            countdownText.gameObject.SetActive(false);
        }
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
            int centiseconds = Mathf.FloorToInt((gameTime * 100f) % 100f);
            gameTimerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centiseconds);
        }
    }
    
    private void UpdateGhostScaredTimerUI()
    {
        if (ghostScaredTimerText != null && ghostTimerPanel != null)
        {
            if (isPowerPillActive)
            {
                ghostScaredTimerText.text = Mathf.CeilToInt(powerPillTimer).ToString();
                ghostTimerPanel.SetActive(true);
            }
            else
            {
                ghostTimerPanel.SetActive(false);
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
        
        // 显示游戏结束UI
        ShowGameOverUI();
    }
    
    private void ShowGameOverUI()
    {
        // 这里可以添加游戏结束的UI显示
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "GAME OVER\nScore: " + score + "\nTime: " + Mathf.FloorToInt(gameTime) + "s";
        }
    }
    
    public void RestartGame()
    {
        // 重置所有游戏状态
        Time.timeScale = 1;
        lives = startingLives;
        score = 0;
        gameTime = 0f;
        isGameRunning = false;
        isPowerPillActive = false;
        isDeathSequenceActive = false;
        isCountdownActive = false;
        
        // 重置UI
        UpdateUI();
        UpdateLivesDisplay();
        UpdateGameTimerUI();
        UpdateGhostScaredTimerUI();
        
        // 隐藏游戏结束文本
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        
        // 重置玩家和幽灵
        PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
        if (pacStudent != null)
        {
            pacStudent.Respawn();
            pacStudent.enabled = false;
        }
        
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts)
        {
            ghost.ResetToInitialPosition();
            ghost.SetNormal();
            ghost.enabled = false;
        }
        
        // 停止音乐
        if (audioSource != null)
            audioSource.Stop();
        
        // 重新开始倒计时
        StartGameCountdown();
    }
    
    // 公共方法供其他脚本访问游戏状态
    public bool IsGameRunning()
    {
        return isGameRunning && !isCountdownActive;
    }
    
    public float GetGameTimer()
    {
        return isGameRunning ? gameTime : 0f;
    }
    
    public int GetCurrentScore()
    {
        return score;
    }
    
    public int GetRemainingLives()
    {
        return lives;
    }
}