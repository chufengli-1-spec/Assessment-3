using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float padding = 2f; // 边界留白

    void Start()
    {
        SetupCamera();
    }

void SetupCamera()
    {
        // 设置相机位置和大小以显示整个关卡
        Camera mainCamera = Camera.main;
        
        // 计算关卡的中心位置
        Vector3 levelCenter = new Vector3(14f, -15f, 0f);
        
        // 设置相机位置
        mainCamera.transform.position = new Vector3(levelCenter.x, levelCenter.y, -10f);
        
        // 计算所需的正交大小以显示整个关卡
        float levelWidth = 28f; // 关卡宽度（瓦片数）
        float levelHeight = 31f; // 关卡高度（瓦片数）
        
        float screenRatio = (float)Screen.width / Screen.height;
        float levelRatio = levelWidth / levelHeight;
        
        if (screenRatio >= levelRatio)
        {
            // 屏幕更宽，以高度为基准
            mainCamera.orthographicSize = (levelHeight + padding) / 2f;
        }
        else
        {
            // 屏幕更高，以宽度为基准
            float size = (levelWidth + padding) / (2f * screenRatio);
            mainCamera.orthographicSize = size;
        }
    }
}