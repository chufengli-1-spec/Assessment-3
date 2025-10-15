using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameStartCountdown : MonoBehaviour
{
    [Header("UI References")]
    public GameObject blockingImage;
    public Text countdownText;
    
    [Header("Countdown Settings")]
    public float countdownInterval = 1f;
    
    [Header("Game References")]
    public PacStudentController playerController;
    // 移除了背景音乐相关的字段
    
    private bool isCountdownActive = false;
    
    void Start()
    {
        Debug.Log("=== GameStartCountdown Start() ===");
        
        // 检查引用是否赋值
        if (blockingImage == null)
            Debug.LogError("BlockingImage is not assigned in inspector!");
        else
            Debug.Log("BlockingImage reference: " + blockingImage.name);
            
        if (countdownText == null)
            Debug.LogError("CountdownText is not assigned in inspector!");
        else
            Debug.Log("CountdownText reference: " + countdownText.name);
            
        if (playerController == null)
            Debug.LogError("PlayerController is not assigned in inspector!");
        else
            Debug.Log("PlayerController reference: " + playerController.name);

        // 确保开始时UI是隐藏的
        if (blockingImage != null) 
        {
            blockingImage.SetActive(false);
            Debug.Log("BlockingImage initially set to inactive");
        }
        if (countdownText != null) 
        {
            countdownText.gameObject.SetActive(false);
            Debug.Log("CountdownText initially set to inactive");
        }
        
        // 开始倒计时
        Debug.Log("Starting countdown...");
        StartCountdown();
    }
    
    public void StartCountdown()
    {
        if (!isCountdownActive)
        {
            Debug.Log("StartCountdown() called - starting coroutine");
            StartCoroutine(CountdownRoutine());
        }
        else
        {
            Debug.LogWarning("StartCountdown() called but countdown is already active");
        }
    }
    
    private IEnumerator CountdownRoutine()
    {
        isCountdownActive = true;
        Debug.Log("=== COUNTDOWN ROUTINE STARTED ===");
        
        // 显示UI元素
        if (blockingImage != null) 
        {
            blockingImage.SetActive(true);
            Debug.Log("✓ BlockingImage set to ACTIVE");
        }
        else
        {
            Debug.LogError("✗ BlockingImage is null - cannot activate");
        }
        
        if (countdownText != null) 
        {
            countdownText.gameObject.SetActive(true);
            Debug.Log("✓ CountdownText set to ACTIVE");
        }
        else
        {
            Debug.LogError("✗ CountdownText is null - cannot activate");
        }
        
        // 禁用玩家控制
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("✓ Player controller DISABLED");
        }
        else
        {
            Debug.LogError("✗ PlayerController is null - cannot disable");
        }
        
        // 倒计时：3
        if (countdownText != null) 
        {
            countdownText.text = "3";
            Debug.Log("🕒 Countdown: 3");
        }
        yield return new WaitForSeconds(countdownInterval);
        Debug.Log("1 second passed...");
        
        // 倒计时：2
        if (countdownText != null) 
        {
            countdownText.text = "2";
            Debug.Log("🕒 Countdown: 2");
        }
        yield return new WaitForSeconds(countdownInterval);
        Debug.Log("2 seconds passed...");
        
        // 倒计时：1
        if (countdownText != null) 
        {
            countdownText.text = "1";
            Debug.Log("🕒 Countdown: 1");
        }
        yield return new WaitForSeconds(countdownInterval);
        Debug.Log("3 seconds passed...");
        
        // 显示GO!
        if (countdownText != null) 
        {
            countdownText.text = "GO!";
            Debug.Log("🏁 Countdown: GO!");
        }
        yield return new WaitForSeconds(countdownInterval);
        Debug.Log("4 seconds passed - countdown complete");
        
        // 隐藏UI元素
        if (blockingImage != null) 
        {
            blockingImage.SetActive(false);
            Debug.Log("✓ BlockingImage set to INACTIVE");
        }
        if (countdownText != null) 
        {
            countdownText.gameObject.SetActive(false);
            Debug.Log("✓ CountdownText set to INACTIVE");
        }
        
        // 启用玩家控制
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("✓ Player controller ENABLED");
        }
        
        // 移除了背景音乐播放代码
        
        // 通知GameManager游戏开始
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStart();
            Debug.Log("✓ GameManager.OnGameStart() called");
        }
        else
        {
            Debug.LogError("✗ GameManager not found in scene!");
        }
        
        isCountdownActive = false;
        Debug.Log("=== COUNTDOWN ROUTINE COMPLETED ===");
    }
    
    public bool IsCountdownActive()
    {
        return isCountdownActive;
    }
    
    // 添加一个测试方法，可以在编辑器中调用
    [ContextMenu("Test Countdown")]
    public void TestCountdown()
    {
        Debug.Log("=== MANUAL COUNTDOWN TEST ===");
        StartCountdown();
    }
    
    // 在Update中检查状态（用于调试）
    void Update()
    {
        // 按T键手动测试倒计时
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("T key pressed - manually starting countdown");
            StartCountdown();
        }
    }
}