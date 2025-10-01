using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneManager : MonoBehaviour
{
    public Button level1Button;
    public Button level2Button;

    private string level1SceneName = "Level1";
    private string level2SceneName = "DesignIterationScene";

    void Start()
    {
        if (level1Button == null)
        {
            Debug.LogError("Level1Button is not assigned in the Inspector!");
            return;
        }

        if (level2Button == null)
        {
            Debug.LogError("Level2Button is not assigned in the Inspector!");
            return;
        }

        level1Button.onClick.AddListener(LoadLevel1);
        level2Button.onClick.AddListener(LoadLevel2);
    }

    void LoadLevel1()
    {
        Debug.Log("Loading Level1 scene...");
        
        if (IsSceneInBuildSettings(level1SceneName))
        {
            SceneManager.LoadScene(level1SceneName);
        }
        else
        {
            Debug.LogError($"Scene '{level1SceneName}' is not in Build Settings!");
        }
    }

    void LoadLevel2()
    {
        Debug.Log("Loading Design Iteration scene...");
        
        if (IsSceneInBuildSettings(level2SceneName))
        {
            SceneManager.LoadScene(level2SceneName);
        }
        else
        {
            Debug.Log("Level 2 is not available yet.");
        }
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneInBuild == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}