using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButton : MonoBehaviour
{
    public void ReturnToStartScene()
    {
        // 加载开始场景
        SceneManager.LoadScene("StartScene");
    }
    
    public void ExitGame()
    {
        // 退出游戏（在打包后的版本中有效）
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}