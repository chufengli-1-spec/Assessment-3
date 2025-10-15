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
    
    private bool isCountdownActive = false;
    
    void Start()
    {
        if (blockingImage != null) 
        {
            blockingImage.SetActive(false);
        }
        if (countdownText != null) 
        {
            countdownText.gameObject.SetActive(false);
        }
        
        StartCountdown();
    }
    
    public void StartCountdown()
    {
        if (!isCountdownActive)
        {
            StartCoroutine(CountdownRoutine());
        }
    }
    
    private IEnumerator CountdownRoutine()
    {
        isCountdownActive = true;
        
        if (blockingImage != null) 
        {
            blockingImage.SetActive(true);
        }
        
        if (countdownText != null) 
        {
            countdownText.gameObject.SetActive(true);
        }
        
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        if (countdownText != null) 
        {
            countdownText.text = "3";
        }
        yield return new WaitForSeconds(countdownInterval);
        
        if (countdownText != null) 
        {
            countdownText.text = "2";
        }
        yield return new WaitForSeconds(countdownInterval);
        
        if (countdownText != null) 
        {
            countdownText.text = "1";
        }
        yield return new WaitForSeconds(countdownInterval);
        
        if (countdownText != null) 
        {
            countdownText.text = "GO!";
        }
        yield return new WaitForSeconds(countdownInterval);
        
        if (blockingImage != null) 
        {
            blockingImage.SetActive(false);
        }
        if (countdownText != null) 
        {
            countdownText.gameObject.SetActive(false);
        }
        
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStart();
        }
        
        isCountdownActive = false;
    }
    
    public bool IsCountdownActive()
    {
        return isCountdownActive;
    }
    
    [ContextMenu("Test Countdown")]
    public void TestCountdown()
    {
        StartCountdown();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCountdown();
        }
    }
}