using UnityEngine;

public class MapBuilder : MonoBehaviour
{
    public GameObject[] tilePrefabs;
    public int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };

    public float tileSize = 1f;
    public float padding = 0.5f; // 边缘留白

    void Start()
    {
        AdjustCameraToFitMap();
        BuildMap();
    }

    void AdjustCameraToFitMap()
    {
        Camera mainCamera = Camera.main;
        
        // 计算地图的实际尺寸
        float mapWidth = levelMap.GetLength(1) * tileSize;
        float mapHeight = levelMap.GetLength(0) * tileSize;
        
        // 计算地图边界
        float minX = 0;
        float maxX = mapWidth;
        float minY = -mapHeight;
        float maxY = 0;
        
        // 计算地图中心（考虑瓦片中心点）
        float centerX = (minX + maxX) / 2f - tileSize / 2f;
        float centerY = (minY + maxY) / 2f + tileSize / 2f;
        
        // 设置相机位置
        mainCamera.transform.position = new Vector3(centerX, centerY, -10f);
        
        // 计算需要的相机大小（考虑边缘留白）
        float requiredWidth = (maxX - minX) + padding * 2f;
        float requiredHeight = (maxY - minY) + padding * 2f;
        
        // 根据屏幕宽高比调整相机大小
        float screenAspect = (float)Screen.width / Screen.height;
        
        if (requiredWidth / requiredHeight > screenAspect)
        {
            // 以宽度为准
            mainCamera.orthographicSize = requiredWidth / (2f * screenAspect);
        }
        else
        {
            // 以高度为准
            mainCamera.orthographicSize = requiredHeight / 2f;
        }
        
        Debug.Log($"相机设置完成: 大小={mainCamera.orthographicSize}, 位置={mainCamera.transform.position}");
    }

    void BuildMap()
    {
        for (int y = 0; y < levelMap.GetLength(0); y++)
        {
            for (int x = 0; x < levelMap.GetLength(1); x++)
            {
                int tileType = levelMap[y, x];
                if (tileType == 0) continue;

                Vector3 pos = new Vector3(x * tileSize, -y * tileSize, 0);
                Instantiate(tilePrefabs[tileType], pos, Quaternion.identity, transform);
            }
        }
    }
}