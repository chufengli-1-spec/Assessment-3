using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    // Original level map array
    private int[,] levelMap = 
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

    // Prefabs for each tile type
    public GameObject outsideCornerPrefab;
    public GameObject outsideWallPrefab;
    public GameObject insideCornerPrefab;
    public GameObject insideWallPrefab;
    public GameObject pelletPrefab;
    public GameObject powerPelletPrefab;
    public GameObject tJunctionPrefab;
    public GameObject ghostExitPrefab;

    // Tile size for positioning
    public float tileSize = 1f;
    
    // Starting position (top-left corner)
    public Vector3 startPosition = new Vector3(-20f, 10f, 0f);

    void Start()
    {
        // 删除手动摆放的所有关卡元素
        DeleteManualLevel();
        
        // 创建父对象用于组织生成的关卡
        GameObject levelParent = new GameObject("Generated Level");
        
        // 只生成左上角（不做镜像）
        GenerateLevel(levelParent);
        
        // 调整相机
        AdjustCamera();
    }

    void DeleteManualLevel()
    {
        Debug.Log("开始删除手动摆放的关卡元素...");
        
        // 直接删除四个已知的角落对象
        DeleteObjectByName("left_corner");
        DeleteObjectByName("right_corner");
        DeleteObjectByName("left bottom_corner");
        DeleteObjectByName("right bottom_corner");
        
        // 删除所有预制体实例
        DeleteAllPrefabInstances();
        
        Debug.Log("删除完成");
    }

    void DeleteObjectByName(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            DestroyImmediate(obj);
            Debug.Log($"已删除: {name}");
        }
        else
        {
            Debug.Log($"未找到: {name}");
        }
    }

    void DeleteAllPrefabInstances()
    {
        // 简化版本：直接查找并删除，不记录名称
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        string[] prefabNames = {
            "outsideCornerPrefab", "outsideWallPrefab", "insideCornerPrefab",
            "insideWallPrefab", "pelletPrefab", "powerPelletPrefab",
            "tJunctionPrefab", "ghostExitPrefab"
        };

        int deletedCount = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            
            foreach (string prefabName in prefabNames)
            {
                if (obj.name.Contains(prefabName))
                {
                    DestroyImmediate(obj);
                    deletedCount++;
                    break;
                }
            }
        }
        
        Debug.Log($"删除了 {deletedCount} 个预制体实例");
    }

    // 只生成左上角：startPosition 视为数组坐标 (0,0) 的世界位置（左上）
    void GenerateLevel(GameObject parent)
    {
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int tileType = levelMap[y, x];
                if (tileType == 0) continue;

                // 将 startPosition 视为数组 (0,0) 的左上角基准
                float posX = startPosition.x + x * tileSize;
                float posY = startPosition.y - y * tileSize;

                GameObject tilePrefab = GetPrefabForTileType(tileType);
                if (tilePrefab != null)
                {
                    GameObject tile = Instantiate(tilePrefab, new Vector3(posX, posY, 0f), Quaternion.identity, parent.transform);
                    tile.name = $"{GetTileTypeName(tileType)}_{x}_{y}";

                    // 不做镜像：传入 mirrorX=1, mirrorY=1
                    float rotation = CalculateRotation(tileType, x, y, 1, 1);
                    tile.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

                    // 调试输出（可选）
                    if (tileType == 8)
                        Debug.Log($"[DEBUG] GhostExit at map ({x},{y}) -> world ({posX},{posY}) rotation {rotation}");
                }
            }
        }
    }

    GameObject GetPrefabForTileType(int tileType)
    {
        switch (tileType)
        {
            case 1: return outsideCornerPrefab;
            case 2: return outsideWallPrefab;
            case 3: return insideCornerPrefab;
            case 4: return insideWallPrefab;
            case 5: return pelletPrefab;
            case 6: return powerPelletPrefab;
            case 7: return tJunctionPrefab;
            case 8: return ghostExitPrefab;
            default: return null;
        }
    }

    string GetTileTypeName(int tileType)
    {
        switch (tileType)
        {
            case 1: return "OutsideCorner";
            case 2: return "OutsideWall";
            case 3: return "InsideCorner";
            case 4: return "InsideWall";
            case 5: return "Pellet";
            case 6: return "PowerPellet";
            case 7: return "TJunction";
            case 8: return "GhostExit";
            default: return "Empty";
        }
    }

   float CalculateRotation(int tileType, int x, int y, int mirrorX, int mirrorY)
{
    float baseRotation = 0f;

    // 基础计算
    switch (tileType)
    {
        case 1: baseRotation = CalculateOutsideCornerRotation(x, y); break;
        case 2: baseRotation = CalculateOutsideWallRotation(x, y); break;
        case 3: baseRotation = CalculateInsideCornerRotation(x, y); break;
        case 4: baseRotation = CalculateInsideWallRotation(x, y); break;
        case 7: baseRotation = CalculateTJunctionRotation(x, y); break;
        case 8: baseRotation = CalculateGhostExitRotation(x, y); break;
        default: return 0f;
    }

    // 镜像修正
    if (mirrorX == -1) baseRotation = -baseRotation;
    if (mirrorY == -1) baseRotation = 180f - baseRotation;

    // 全局取反修正
    baseRotation = -baseRotation;

    // ---- 特例 ----
    // -20,-3 对应数组索引 x=0, y=13
    if (tileType == 2 && x == 0 && y == 13)
        baseRotation = -180f;
    // 其他特例保持原来逻辑
    else
    {
        float worldX = startPosition.x + x * tileSize;
        float worldY = startPosition.y - y * tileSize;

        if (worldX == -15f && worldY == 1f)
            baseRotation = -90f;
        else if (worldX == -12f && worldY == 1f)
            baseRotation = 90f;
        else if (worldX == -7f && worldY == 6f)
            baseRotation = 90f;
        else if (worldX == -7f && worldY == 0f)
            baseRotation = 90f;
        else if (tileType == 2 && worldY == -3f && worldX >= -20f && worldX <= -16f)
            baseRotation = -180f;
        else if (tileType == 4 && worldX == -12f && worldY == -1f)
            baseRotation = 90f;
        else if (tileType == 4 && worldX >= -13f && worldX <= -12f && worldY <= 3f && worldY >= -2f)
            baseRotation = 90f;
        else if (tileType == 2 && worldX == -20f)
            baseRotation = 90f;
    }

    // 归一化
    baseRotation = (baseRotation % 360f + 360f) % 360f;
    return baseRotation;
}








    float CalculateOutsideCornerRotation(int x, int y)
    {
        bool hasLeft = x > 0 && levelMap[y, x - 1] != 0;
        bool hasRight = x < levelMap.GetLength(1) - 1 && levelMap[y, x + 1] != 0;
        bool hasUp = y > 0 && levelMap[y - 1, x] != 0;
        bool hasDown = y < levelMap.GetLength(0) - 1 && levelMap[y + 1, x] != 0;

        if (hasRight && hasDown) return 0f;
        if (hasLeft && hasDown) return 90f;
        if (hasLeft && hasUp) return 180f;
        if (hasRight && hasUp) return 270f;
        return 0f;
    }

    float CalculateOutsideWallRotation(int x, int y)
    {
        bool hasLeftWall = x > 0 && (levelMap[y, x - 1] == 1 || levelMap[y, x - 1] == 2 || levelMap[y, x - 1] == 7);
        bool hasRightWall = x < levelMap.GetLength(1) - 1 && (levelMap[y, x + 1] == 1 || levelMap[y, x + 1] == 2 || levelMap[y, x + 1] == 7);
        bool hasUpWall = y > 0 && (levelMap[y - 1, x] == 1 || levelMap[y - 1, x] == 2 || levelMap[y - 1, x] == 7);
        bool hasDownWall = y < levelMap.GetLength(0) - 1 && (levelMap[y + 1, x] == 1 || levelMap[y + 1, x] == 2 || levelMap[y + 1, x] == 7);

        if ((hasUpWall || hasDownWall) && !(hasLeftWall || hasRightWall))
            return 90f;
        return 0f;
    }

    float CalculateInsideCornerRotation(int x, int y)
    {
        bool hasLeft = x > 0 && (levelMap[y, x - 1] == 3 || levelMap[y, x - 1] == 4 || levelMap[y, x - 1] == 8);
        bool hasRight = x < levelMap.GetLength(1) - 1 && (levelMap[y, x + 1] == 3 || levelMap[y, x + 1] == 4 || levelMap[y, x + 1] == 8);
        bool hasUp = y > 0 && (levelMap[y - 1, x] == 3 || levelMap[y - 1, x] == 4 || levelMap[y - 1, x] == 8);
        bool hasDown = y < levelMap.GetLength(0) - 1 && (levelMap[y + 1, x] == 3 || levelMap[y + 1, x] == 4 || levelMap[y + 1, x] == 8);

        if (hasRight && hasDown) return 0f;
        if (hasLeft && hasDown) return 90f;
        if (hasLeft && hasUp) return 180f;
        if (hasRight && hasUp) return 270f;
        return 0f;
    }

    float CalculateInsideWallRotation(int x, int y)
    {
        bool hasLeftWall = x > 0 && (levelMap[y, x - 1] == 3 || levelMap[y, x - 1] == 4 || levelMap[y, x - 1] == 8);
        bool hasRightWall = x < levelMap.GetLength(1) - 1 && (levelMap[y, x + 1] == 3 || levelMap[y, x + 1] == 4 || levelMap[y, x + 1] == 8);
        bool hasUpWall = y > 0 && (levelMap[y - 1, x] == 3 || levelMap[y - 1, x] == 4 || levelMap[y - 1, x] == 8);
        bool hasDownWall = y < levelMap.GetLength(0) - 1 && (levelMap[y + 1, x] == 3 || levelMap[y + 1, x] == 4 || levelMap[y + 1, x] == 8);

        if ((hasUpWall || hasDownWall) && !(hasLeftWall || hasRightWall))
            return 90f;
        return 0f;
    }

    float CalculateTJunctionRotation(int x, int y)
    {
        bool hasLeft = x > 0 && levelMap[y, x - 1] != 0;
        bool hasRight = x < levelMap.GetLength(1) - 1 && levelMap[y, x + 1] != 0;
        bool hasUp = y > 0 && levelMap[y - 1, x] != 0;
        bool hasDown = y < levelMap.GetLength(0) - 1 && levelMap[y + 1, x] != 0;

        if (hasLeft && hasUp && hasDown) return 0f;
        if (hasUp && hasLeft && hasRight) return 90f;
        if (hasRight && hasUp && hasDown) return 180f;
        if (hasDown && hasLeft && hasRight) return 270f;
        return 0f;
    }

    float CalculateGhostExitRotation(int x, int y)
    {
        bool hasLeft = x > 0 && levelMap[y, x - 1] != 0;
        bool hasRight = x < levelMap.GetLength(1) - 1 && levelMap[y, x + 1] != 0;
        if (hasLeft || hasRight) return 0f;
        return 90f;
    }

    void AdjustCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        // 地图左上角是 startPosition，右下角:
        Vector3 topLeft = startPosition;
        Vector3 bottomRight = new Vector3(startPosition.x + (cols - 1) * tileSize, startPosition.y - (rows - 1) * tileSize, startPosition.z);

        float levelWidth = Mathf.Abs(bottomRight.x - topLeft.x) + tileSize;
        float levelHeight = Mathf.Abs(topLeft.y - bottomRight.y) + tileSize;

        Vector3 center = (topLeft + bottomRight) / 2f;
        mainCamera.transform.position = new Vector3(center.x, center.y, mainCamera.transform.position.z);

        float screenRatio = (float)Screen.width / Screen.height;
        float levelRatio = levelWidth / levelHeight;

        if (levelRatio > screenRatio)
            mainCamera.orthographicSize = levelWidth / (2f * screenRatio);
        else
            mainCamera.orthographicSize = levelHeight / 2f;
    }
}
