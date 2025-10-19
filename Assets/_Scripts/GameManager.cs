using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Text scoreText;
    public GameObject livesContainer;
    public Text gameTimerText;
    public Text ghostScaredTimerText;
    public GameObject ghostTimerPanel;
    
    public GameObject blockingImage;
    public TextMeshProUGUI countdownText;
    public float countdownInterval = 1f;
    
    public GameObject gameOverPanel;
    public Text gameOverText;
    
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip ghostEatenMusic;
    
    public int startingLives = 3;
    public float deathSequenceDuration = 3f;
    public float gameOverDisplayDuration = 3f;
    
    public PacStudentController playerController;
    
    private int score = 0;
    private int lives;
    private AudioSource audioSource;
    private float powerPillTimer = 0f;
    private bool isPowerPillActive = false;
    private bool isDeathSequenceActive = false;
    private bool isGameRunning = false;
    private bool isCountdownActive = false;
    private bool isGameOver = false;
    private float gameTime = 0f;
    
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
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        UpdateLivesDisplay();
        
        CountTotalPellets();
        
        InitializeGameStartUI();
        
        StartGameCountdown();
    }
    
    private void CountTotalPellets()
    {
        GameObject[] pellets = GameObject.FindGameObjectsWithTag("Pellet");
        GameObject[] powerPills = GameObject.FindGameObjectsWithTag("PowerPill");
        totalPellets = pellets.Length + powerPills.Length;
        collectedPellets = 0;
    }
    
    public void OnPelletCollected(bool isPowerPill = false)
    {
        if (isGameOver || isDeathSequenceActive || !isGameRunning) return;
        
        collectedPellets++;
        
        if (collectedPellets >= 223)
        {
            StartCoroutine(GameOverSequence(true));
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
        
        if (blockingImage != null) 
            blockingImage.SetActive(true);
        if (countdownText != null) 
            countdownText.gameObject.SetActive(true);
        
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        SetGhostsActive(false);
        
        if (countdownText != null) 
            countdownText.text = "3";
        yield return new WaitForSeconds(countdownInterval);
        
        if (countdownText != null) 
            countdownText.text = "2";
        yield return new WaitForSeconds(countdownInterval);
        
        if (countdownText != null) 
            countdownText.text = "1";
        yield return new WaitForSeconds(countdownInterval);
        
        if (countdownText != null) 
            countdownText.text = "GO!";
        yield return new WaitForSeconds(countdownInterval);
        
        if (blockingImage != null) 
            blockingImage.SetActive(false);
        if (countdownText != null) 
            countdownText.gameObject.SetActive(false);
        
        OnGameStart();
        
        isCountdownActive = false;
    }
    
    public void OnGameStart()
    {
        isGameRunning = true;
        gameTime = 0f;
        UpdateGameTimerUI();
        
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        SetGhostsActive(true);
        
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
        
        PacStudentController pacStudent = FindObjectOfType<PacStudentController>();
        if (pacStudent != null)
        {
            pacStudent.Die();
        }
        
        yield return new WaitForSeconds(deathSequenceDuration);
        
        if (lives <= 0)
        {
            StartCoroutine(GameOverSequence(false));
        }
        else
        {
            if (pacStudent != null)
            {
                pacStudent.Respawn();
            }
            
            GhostController[] ghosts = FindObjectsOfType<GhostController>();
            foreach (GhostController ghost in ghosts)
            {
                ghost.ResetToInitialPosition();
                ghost.SetNormal();
            }
            
            yield return new WaitForSeconds(1f);
            StartCoroutine(RespawnCountdown());
        }
        
        isDeathSequenceActive = false;
    }
    
    private IEnumerator GameOverSequence(bool isWin)
    {
        isGameOver = true;
        isGameRunning = false;
        
        StopAllMovement();
        
        DisableUIButtons();
        
        UpdateGameTimerUI();
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        ShowGameOverUI(isWin);
        
        SaveHighScoreIfNeeded();
        
        yield return new WaitForSeconds(gameOverDisplayDuration);
        
        ReturnToStartScene();
    }
    
    private void StopAllMovement()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts)
        {
            ghost.enabled = false;
        }
    }
    
    private void DisableUIButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button button in allButtons)
        {
            button.interactable = false;
        }
    }
    
    private void SaveHighScoreIfNeeded()
    {
        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.CheckAndSaveHighScore(score, gameTime);
        }
    }
    
    private void ShowGameOverUI(bool isWin)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (blockingImage != null)
            {
                blockingImage.SetActive(true);
            }
        }
    }
    
    private IEnumerator RespawnCountdown()
    {
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
        
        if (lives <= 0)
        {
            StartCoroutine(GameOverSequence(false));
        }
        else
        {
            StartCoroutine(DeathSequence());
        }
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
        if (ghostScaredTimerText != null)
        {
            if (isPowerPillActive && powerPillTimer > 0)
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
    
    private void ReturnToStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }

    public void RestartGame()
    {
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

        UpdateUI();
        UpdateLivesDisplay();
        UpdateGameTimerUI();
        UpdateGhostScaredTimerUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (blockingImage != null)
            blockingImage.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

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

        CountTotalPellets();

        StartGameCountdown();
    }
    
    public void PlayerTakeDamage(int damageAmount)
    {
        if (isDeathSequenceActive || isGameOver || !isGameRunning || isCountdownActive)
            return;
        
        lives -= damageAmount;
        UpdateUI();
        UpdateLivesDisplay();
        
        if (lives <= 0)
        {
            lives = 0;
            StartCoroutine(GameOverSequence(false));
        }
        else
        {
            StartCoroutine(DeathSequence());
        }
    }

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