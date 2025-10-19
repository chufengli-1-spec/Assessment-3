using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneManager : MonoBehaviour
{
    public Button level1Button;
    public Button level2Button;

    private string level1SceneName = "Level1";
    private string level2SceneName = "Level2";

    void Start()
    {
        if (level1Button == null)
        {
            return;
        }

        if (level2Button == null)
        {
            return;
        }

        level1Button.onClick.AddListener(LoadLevel1);
        level2Button.onClick.AddListener(LoadLevel2);
    }

    void LoadLevel1()
    {
        if (IsSceneInBuildSettings(level1SceneName))
        {
            SceneManager.LoadScene(level1SceneName);
        }
        else
        {
        }
    }

    void LoadLevel2()
    {
        if (IsSceneInBuildSettings(level2SceneName))
        {
            SceneManager.LoadScene(level2SceneName);
        }
        else
        {
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