using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text scoreText;
    public GameObject livesContainer;  // 改为引用容器对象
    public Text gameTimerText;
    public Text ghostScaredTimerText;  // 改为使用这个显示恐惧时间
    public GameObject ghostTimerPanel; // 如果需要的话保留面板
    
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
    
    // 游戏计时相关
    private float gameTime = 0f;
    private bool isGameRunning = true;
    
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
        
        // 初始化生命显示
        UpdateLivesDisplay();
        
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
        if (isGameRunning)
        {
            // 更新游戏时间
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
        
        // 更新恐惧时间显示
        UpdateGhostScaredTimerUI();
    }
    
    private void UpdatePowerPillTimer()
    {
        powerPillTimer -= Time.deltaTime;
        UpdateGhostScaredTimerUI();
        
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
            
            // 隐藏恐惧时间显示
            UpdateGhostScaredTimerUI();
        }
    }
    
    // 新增：更新游戏时间显示
    private void UpdateGameTimerUI()
    {
        if (gameTimerText != null)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            gameTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    // 修改：更新幽灵恐惧时间显示
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
    
    // 新增：更新生命显示（通过控制子对象激活状态）
    private void UpdateLivesDisplay()
    {
        if (livesContainer != null)
        {
            int childCount = livesContainer.transform.childCount;
            
            // 激活对应数量的生命图标
            for (int i = 0; i < childCount; i++)
            {
                GameObject lifeIcon = livesContainer.transform.GetChild(i).gameObject;
                lifeIcon.SetActive(i < lives);
            }
        }
    }
    
    public void PacStudentDied()
    {
        lives--;
        UpdateUI();
        UpdateLivesDisplay(); // 更新生命显示
        
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
    
    // 修改：更新UI方法
    public void UpdateUI()
    {
        // 更新分数
        if (scoreText != null)
            scoreText.text = "Score: " + score;
        
        // 生命显示已经在UpdateLivesDisplay中处理
        // 游戏时间在Update中持续更新
        // 恐惧时间在UpdatePowerPillTimer中更新
    }
    
    private void GameOver()
    {
        // 处理游戏结束逻辑
        isGameRunning = false;
        Debug.Log("Game Over! Final Score: " + score + " Time: " + Mathf.FloorToInt(gameTime) + "s");
        
        // 可以在这里显示游戏结束UI
        Time.timeScale = 0; // 暂停游戏
    }
    
    // 新增：重新开始游戏
    public void RestartGame()
    {
        Time.timeScale = 1;
        lives = startingLives;
        score = 0;
        gameTime = 0f;
        isGameRunning = true;
        isPowerPillActive = false;
        
        UpdateUI();
        UpdateLivesDisplay();
        UpdateGameTimerUI();
        UpdateGhostScaredTimerUI();
    }
}