using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // 添加场景管理

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
    
    [Header("Game Over UI")]
    public GameObject gameOverPanel; // 新增：游戏结束面板
    public Text gameOverText; // 新增：游戏结束文本
    
    [Header("Audio")]
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip ghostEatenMusic;
    
    [Header("Game Settings")]
    public int startingLives = 3;
    public float deathSequenceDuration = 3f;
    public float gameOverDisplayDuration = 3f; // 新增：游戏结束显示时间
    
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
    private bool isGameOver = false; // 新增：游戏结束状态
    private float gameTime = 0f;
    
    // 豆子计数
    private int totalPellets = 0;
    private int collectedPellets = 0;
    
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
        
        // 初始化游戏结束UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        UpdateLivesDisplay();
        
        // 计算总豆子数
        CountTotalPellets();
        
        // 初始化游戏开始UI
        InitializeGameStartUI();
        
        // 开始倒计时
        StartGameCountdown();
    }
    
    // 计算场景中的总豆子数
    private void CountTotalPellets()
    {
        GameObject[] pellets = GameObject.FindGameObjectsWithTag("Pellet");
        GameObject[] powerPills = GameObject.FindGameObjectsWithTag("PowerPill");
        totalPellets = pellets.Length + powerPills.Length;
        Debug.Log($"Total pellets in scene: {totalPellets}");
    }
    
    // 当豆子被收集时调用
    public void OnPelletCollected(bool isPowerPill = false)
    {
        collectedPellets++;
        Debug.Log($"Pellet collected: {collectedPellets}/{totalPellets}");
        
        // 检查是否所有豆子都被吃完
        if (collectedPellets >= totalPellets)
        {
            StartCoroutine(GameOverSequence(true)); // 胜利结束
        }
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
        UpdateGameTimerUI();
        
        Debug.Log("Game Started! Timer reset to 00:00:00");
        
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
        if (isGameRunning && !isDeathSequenceActive && !isCountdownActive && !isGameOver)
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
        if (isDeathSequenceActive || !isGameRunning || isGameOver) return;
        
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
            StartCoroutine(GameOverSequence(false)); // 失败结束
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
    
    // 新增：游戏结束序列
    private IEnumerator GameOverSequence(bool isWin)
    {
        isGameOver = true;
        isGameRunning = false;
        
        Debug.Log($"=== GAME OVER SEQUENCE STARTED === (Win: {isWin})");
        
        // 停止所有移动
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        SetGhostsActive(false);
        
        // 停止计时器
        UpdateGameTimerUI();
        
        // 停止音乐
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // 显示游戏结束UI
        ShowGameOverUI(isWin);
        
        // 保存最高分
        if (HighScoreManager.Instance != null)
        {
            bool newHighScore = HighScoreManager.Instance.CheckAndSaveHighScore(score, gameTime);
            if (newHighScore)
            {
                Debug.Log("New high score saved!");
            }
        }
        
        // 等待3秒
        yield return new WaitForSeconds(gameOverDisplayDuration);
        
        // 返回开始场景
        ReturnToStartScene();
    }
    
    // 显示游戏结束UI
    private void ShowGameOverUI(bool isWin)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            string resultText = isWin ? "VICTORY!" : "GAME OVER";
            string timeString = FormatTime(gameTime);
            
            if (gameOverText != null)
            {
                gameOverText.text = $"{resultText}\nFinal Score: {score}\nTime: {timeString}";
            }
            
            // 同时使用blockingImage
            if (blockingImage != null)
            {
                blockingImage.SetActive(true);
            }
        }
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
        if (isDeathSequenceActive || isGameOver) return;
        
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
    
    // 格式化时间
    private string FormatTime(float time)
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
    
    // 返回开始场景
    private void ReturnToStartScene()
    {
        Debug.Log("Returning to Start Scene...");
        SceneManager.LoadScene("StartScene"); // 替换为你的开始场景名称
    }
    
    public void RestartGame()
    {
        // 重置所有游戏状态
        Time.timeScale = 1;
        lives = startingLives;
        score = 0;
        collectedPellets = 0;
        gameTime = 0f;
        isGameRunning = false;
        isGameOver = false;
        isPowerPillActive = false;
        isDeathSequenceActive = false;
        isCountdownActive = false;
        
        // 重置UI
        UpdateUI();
        UpdateLivesDisplay();
        UpdateGameTimerUI();
        UpdateGhostScaredTimerUI();
        
        // 隐藏游戏结束UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (blockingImage != null)
            blockingImage.SetActive(false);
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
        
        // 重新计算豆子
        CountTotalPellets();
        
        // 重新开始倒计时
        StartGameCountdown();
    }
    
    // 公共方法供其他脚本访问游戏状态
    public bool IsGameRunning()
    {
        return isGameRunning && !isCountdownActive && !isGameOver;
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