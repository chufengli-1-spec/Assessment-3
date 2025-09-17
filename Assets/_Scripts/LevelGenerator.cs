using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
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

    public GameObject outsideCornerPrefab;
    public GameObject outsideWallPrefab;
    public GameObject insideCornerPrefab;
    public GameObject insideWallPrefab;
    public GameObject pelletPrefab;
    public GameObject powerPelletPrefab;
    public GameObject tJunctionPrefab;
    public GameObject ghostExitPrefab;

    public float tileSize = 1f;
    
    public Vector3 startPosition = new Vector3(-20f, 10f, 0f);

    void Start()
    {
        DeleteManualLevel();
        
        GameObject levelParent = new GameObject("Generated Level");
        
        GenerateLevel(levelParent);
        
        AdjustCamera();
    }

    void DeleteManualLevel()
    {
        DeleteObjectByName("left_corner");
        DeleteObjectByName("right_corner");
        DeleteObjectByName("left bottom_corner");
        DeleteObjectByName("right bottom_corner");
        
    DeleteAllPrefabInstances();
        
    }

    void DeleteObjectByName(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            DestroyImmediate(obj);
        }
    }

    void DeleteAllPrefabInstances()
    {
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
    }

    void GenerateLevel(GameObject parent)
    {
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        GameObject leftTopParent = new GameObject("LeftTop");
        leftTopParent.transform.parent = parent.transform;
        GenerateQuadrant(leftTopParent, "LeftTop_");

        CreateRightTopFromLeftTop(leftTopParent, parent);

    CreateLeftBottomFromLeftTop(leftTopParent, parent);

        GameObject rightTopParent = GameObject.Find("RightTop");
        if (rightTopParent != null)
        {
            CreateRightBottomFromRightTop(rightTopParent, parent);
        }

    RemoveBottomFirstRowAndAdjust(parent);
}

    void CreateRightTopFromLeftTop(GameObject leftTopParent, GameObject levelParent)
    {
        GameObject rightTopParent = new GameObject("RightTop");
        rightTopParent.transform.parent = levelParent.transform;

        int cols = levelMap.GetLength(1);
        float leftTopRightEdge = startPosition.x + (cols - 1) * tileSize;
        float rightTopStartX = leftTopRightEdge + tileSize;

        foreach (Transform child in leftTopParent.transform)
        {
            GameObject newObj = Instantiate(child.gameObject, rightTopParent.transform);
            newObj.name = child.name.Replace("LeftTop_", "RightTop_");

            Vector3 originalPos = child.position;
            float mirroredX = rightTopStartX + (leftTopRightEdge - originalPos.x);
            newObj.transform.position = new Vector3(mirroredX, originalPos.y, originalPos.z);

            float originalZRotation = child.eulerAngles.z;
            newObj.transform.rotation = Quaternion.Euler(0f, 180f, originalZRotation);
        }
    }

    void CreateLeftBottomFromLeftTop(GameObject leftTopParent, GameObject levelParent)
{
    GameObject leftBottomParent = new GameObject("LeftBottom");
    leftBottomParent.transform.parent = levelParent.transform;
    
    int rows = levelMap.GetLength(0);
    float topBottomEdge = startPosition.y - (rows - 1) * tileSize;
    float bottomTopStartY = topBottomEdge - tileSize; 
    
    foreach (Transform child in leftTopParent.transform)
    {
        GameObject newObj = Instantiate(child.gameObject, leftBottomParent.transform);
        newObj.name = child.name.Replace("LeftTop_", "LeftBottom_");

        Vector3 originalPos = child.position;
        float mirroredY = bottomTopStartY + (topBottomEdge - originalPos.y);
        newObj.transform.position = new Vector3(originalPos.x, mirroredY, originalPos.z);
        
        float originalZRotation = child.eulerAngles.z;
        newObj.transform.rotation = Quaternion.Euler(180f, 0f, originalZRotation);
    }
}

    void CreateRightBottomFromRightTop(GameObject rightTopParent, GameObject levelParent)
    {
        GameObject rightBottomParent = new GameObject("RightBottom");
        rightBottomParent.transform.parent = levelParent.transform;

        int rows = levelMap.GetLength(0);
        float topBottomEdge = startPosition.y - (rows - 1) * tileSize; 
        float bottomTopStartY = topBottomEdge - tileSize; 

        foreach (Transform child in rightTopParent.transform)
        {
            GameObject newObj = Instantiate(child.gameObject, rightBottomParent.transform);
            newObj.name = child.name.Replace("RightTop_", "RightBottom_");

            Vector3 originalPos = child.position;
            float mirroredY = bottomTopStartY + (topBottomEdge - originalPos.y);
            newObj.transform.position = new Vector3(originalPos.x, mirroredY, originalPos.z);

            float originalZRotation = child.eulerAngles.z;
            newObj.transform.rotation = Quaternion.Euler(180f, 180f, originalZRotation);
        }
    }

    void RemoveBottomFirstRowAndAdjust(GameObject levelParent)
{
    int rows = levelMap.GetLength(0);
    float bottomFirstRowY = startPosition.y - (rows - 1) * tileSize; 
    
    GameObject leftBottomParent = GameObject.Find("LeftBottom");
    GameObject rightBottomParent = GameObject.Find("RightBottom");
    
    if (leftBottomParent != null)
    {
        RemoveRowFromQuadrant(leftBottomParent, bottomFirstRowY);
        
        AdjustBottomQuadrantPosition(leftBottomParent, bottomFirstRowY);
    }
    
    if (rightBottomParent != null)
    {
        RemoveRowFromQuadrant(rightBottomParent, bottomFirstRowY);
        
        AdjustBottomQuadrantPosition(rightBottomParent, bottomFirstRowY);
    }
}

void RemoveRowFromQuadrant(GameObject quadrantParent, float rowY)
{
    List<GameObject> objectsToRemove = new List<GameObject>();
    
    foreach (Transform child in quadrantParent.transform)
    {
        if (Mathf.Abs(child.position.y - rowY) < 0.1f)
        {
            objectsToRemove.Add(child.gameObject);
        }
    }
    
    foreach (GameObject obj in objectsToRemove)
    {
        DestroyImmediate(obj);
    }
}

void AdjustBottomQuadrantPosition(GameObject quadrantParent, float originalFirstRowY)
{
    float moveUpDistance = tileSize;
    
    foreach (Transform child in quadrantParent.transform)
    {
        child.position = new Vector3(
            child.position.x,
            child.position.y + moveUpDistance,
            child.position.z
        );
    }
}

    float CalculateMirroredRotation(float originalRotation)
    {
        float normalized = (originalRotation % 360f + 360f) % 360f;
        
        if (normalized == 0f) return 180f;
        if (normalized == 90f) return 270f;
        if (normalized == 180f) return 0f;
        if (normalized == 270f) return 90f;
        
        return (180f - normalized + 360f) % 360f;
    }

    void GenerateQuadrant(GameObject parent, string prefix)
    {
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int tileType = levelMap[y, x];
                if (tileType == 0) continue;

                float posX = startPosition.x + x * tileSize;
                float posY = startPosition.y - y * tileSize;

                GameObject tilePrefab = GetPrefabForTileType(tileType);
                if (tilePrefab != null)
                {
                    GameObject tile = Instantiate(tilePrefab, new Vector3(posX, posY, 0f), Quaternion.identity, parent.transform);
                    tile.name = $"{prefix}{GetTileTypeName(tileType)}_{x}_{y}";

                    float rotation = CalculateRotation(tileType, x, y, 1, 1);
                    tile.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
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

        if (mirrorX == -1) baseRotation = -baseRotation;
        if (mirrorY == -1) baseRotation = 180f - baseRotation;

        baseRotation = -baseRotation;

        if (tileType == 2 && x == 0 && y == 13)
            baseRotation = -180f;
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
    
    mainCamera.transform.position = new Vector3(-10f, -5f, -10f);
    
    mainCamera.orthographic = true;
    
    mainCamera.orthographicSize = 16f;
 }
}