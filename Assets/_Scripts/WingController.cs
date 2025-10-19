using UnityEngine;
using System.Collections;

public class WingController : MonoBehaviour
{
    public float spawnDelay = 10f;
    public float moveSpeed = 2f; 
    public float levelBoundsOffset = 1f; 
    public GameObject wingPrefab;
    public LevelGenerator levelGenerator;
    public Transform levelCenter;

    private GameObject currentWing;
    private bool isWingActive = false;
    private Coroutine spawnCoroutine;

    private enum SpawnSide { Top, Right, Bottom, Left }
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 centerPosition;
    private float lerpTime = 0f;

    void Start()
    {
        if (levelGenerator == null)
        {
            levelGenerator = FindObjectOfType<LevelGenerator>();
        }

        if (levelCenter == null && levelGenerator != null)
        {
            Vector3 center = CalculateLevelCenter();
            levelCenter = new GameObject("LevelCenter").transform;
            levelCenter.position = center;
        }
        
        spawnCoroutine = StartCoroutine(SpawnWingAfterDelay(spawnDelay));
    }

    void Update()
    {
        if (isWingActive && currentWing != null)
        {
            MoveWing();
        }
    }

    private IEnumerator SpawnWingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnWing();
    }

    private void SpawnWing()
    {
        if (wingPrefab == null || levelGenerator == null)
        {
            return;
        }

        SpawnSide spawnSide = GetRandomSpawnSide();
        CalculateWingPath(spawnSide);

        currentWing = Instantiate(wingPrefab, startPosition, Quaternion.identity);
        currentWing.name = "SpeedWing";
        currentWing.tag = "Wing";
        
        SpriteRenderer spriteRenderer = currentWing.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 100; 
        }

        Collider2D collider = currentWing.GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = currentWing.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = true;

        Rigidbody2D rb = currentWing.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = currentWing.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = true;
        rb.simulated = true;

        WingCollisionHandler wingCollision = currentWing.GetComponent<WingCollisionHandler>();
        if (wingCollision == null)
        {
            wingCollision = currentWing.AddComponent<WingCollisionHandler>();
        }
        wingCollision.Initialize(this);

        isWingActive = true;
        lerpTime = 0f;
    }

    private SpawnSide GetRandomSpawnSide()
    {
        return (SpawnSide)Random.Range(0, 4);
    }

    private void CalculateWingPath(SpawnSide spawnSide)
    {
        if (levelGenerator == null) return;

        Bounds levelBounds = CalculateLevelBounds();
        centerPosition = levelCenter != null ? levelCenter.position : levelBounds.center;

        switch (spawnSide)
        {
            case SpawnSide.Top:
                startPosition = new Vector3(
                    Random.Range(levelBounds.min.x, levelBounds.max.x),
                    levelBounds.max.y + levelBoundsOffset,
                    0
                );
                endPosition = new Vector3(
                    Random.Range(levelBounds.min.x, levelBounds.max.x),
                    levelBounds.min.y - levelBoundsOffset,
                    0
                );
                break;

            case SpawnSide.Right:
                startPosition = new Vector3(
                    levelBounds.max.x + levelBoundsOffset,
                    Random.Range(levelBounds.min.y, levelBounds.max.y),
                    0
                );
                endPosition = new Vector3(
                    levelBounds.min.x - levelBoundsOffset,
                    Random.Range(levelBounds.min.y, levelBounds.max.y),
                    0
                );
                break;

            case SpawnSide.Bottom:
                startPosition = new Vector3(
                    Random.Range(levelBounds.min.x, levelBounds.max.x),
                    levelBounds.min.y - levelBoundsOffset,
                    0
                );
                endPosition = new Vector3(
                    Random.Range(levelBounds.min.x, levelBounds.max.x),
                    levelBounds.max.y + levelBoundsOffset,
                    0
                );
                break;

            case SpawnSide.Left:
                startPosition = new Vector3(
                    levelBounds.min.x - levelBoundsOffset,
                    Random.Range(levelBounds.min.y, levelBounds.max.y),
                    0
                );
                endPosition = new Vector3(
                    levelBounds.max.x + levelBoundsOffset,
                    Random.Range(levelBounds.min.y, levelBounds.max.y),
                    0
                );
                break;
        }
    }

    private void MoveWing()
    {
        lerpTime += Time.deltaTime * moveSpeed;

        if (lerpTime > 1f)
        {
            DestroyWing();
            return;
        }

        Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, lerpTime);
        currentWing.transform.position = currentPosition;
    }

    private Bounds CalculateLevelBounds()
    {
        if (levelGenerator == null) return new Bounds();

        Vector3 startPos = levelGenerator.startPosition;
        int width = levelGenerator.levelMap.GetLength(1) * 2;
        int height = (levelGenerator.levelMap.GetLength(0) * 2) - 2;
        float tileSize = levelGenerator.tileSize;

        Vector3 center = new Vector3(
            startPos.x + (width * tileSize) / 2,
            startPos.y - (height * tileSize) / 2,
            0
        );

        Vector3 size = new Vector3(width * tileSize, height * tileSize, 0);

        return new Bounds(center, size);
    }

    private Vector3 CalculateLevelCenter()
    {
        if (levelGenerator == null) return Vector3.zero;

        Bounds bounds = CalculateLevelBounds();
        return bounds.center;
    }

    public void DestroyWing()
    {
        if (currentWing != null)
        {
            Destroy(currentWing);
            currentWing = null;
        }

        isWingActive = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnWingAfterDelay(spawnDelay));
    }

    public void OnWingCollected()
    {
        isWingActive = false;
        
        if (currentWing != null)
        {
            Destroy(currentWing);
            currentWing = null;
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnWingAfterDelay(spawnDelay));
    }

    public void ForceDestroyWing()
    {
        DestroyWing();
    }
}