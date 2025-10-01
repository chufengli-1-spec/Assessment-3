using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneManager : MonoBehaviour
{
    // 拖拽赋值：在Inspector中将这两个按钮拖拽到对应的变量上
    public Button level1Button;
    public Button level2Button;

    // 定义场景名称常量
    private string level1SceneName = "Level1";
    private string level2SceneName = "DesignIterationScene";

    void Start()
    {
        // 检查按钮是否被正确赋值
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

        // 为按钮的点击事件添加监听器
        level1Button.onClick.AddListener(LoadLevel1);
        level2Button.onClick.AddListener(LoadLevel2);
    }

    void LoadLevel1()
    {
        Debug.Log("Loading Level1 scene...");
        
        // 检查场景是否存在
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
        
        // 检查场景是否存在
        if (IsSceneInBuildSettings(level2SceneName))
        {
            SceneManager.LoadScene(level2SceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{level2SceneName}' is not in Build Settings or not implemented yet.");
            // 如果Level2还没完成，可以取消注释下面的行：
            // Debug.Log("Level 2 is not available yet.");
        }
    }

    // 辅助方法：检查场景是否在构建设置中
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