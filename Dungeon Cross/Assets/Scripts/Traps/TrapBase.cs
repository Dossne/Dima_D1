using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TrapBase : MonoBehaviour
{
    [SerializeField] protected float moveInterval = 0.6f;
    [SerializeField] protected int direction = 1;
    [SerializeField] protected Vector2Int gridPosition;
    [SerializeField] protected TrapPattern pattern = TrapPattern.Horizontal;

    private static Sprite cachedTrapSprite;
    private readonly List<Vector2Int> trajectoryGridPositions = new List<Vector2Int>();
    private float moveTimer;
    protected int pathIndex;
    protected int pathDirection = 1;

    public Vector2Int GridPosition => gridPosition;
    public TrapPattern Pattern => pattern;
    protected IReadOnlyList<Vector2Int> TrajectoryGridPositions => trajectoryGridPositions;

    protected virtual void Awake()
    {
        TrapManager.Instance?.RegisterTrap(this);
    }

    protected virtual void Start()
    {
        CreateVisual();
        CacheTrajectory();
        UpdateWorldPosition();
        DrawTrajectory();
    }

    protected virtual void Update()
    {
        if (!PlayerController.GameStarted)
        {
            return;
        }

        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsLevelComplete || GameManager.Instance.IsPaused))
        {
            return;
        }

        moveTimer += Time.unscaledDeltaTime;

        while (moveTimer >= moveInterval)
        {
            moveTimer -= moveInterval;
            MoveStep();
            TrapManager.Instance?.CheckCollision(this);
        }
    }

    protected virtual void OnDestroy()
    {
        TrapManager.Instance?.UnregisterTrap(this);
    }

    public virtual void Configure(TrapRow rowConfig, Vector2Int startGridPosition)
    {
        direction = rowConfig.direction >= 0 ? 1 : -1;
        pattern = rowConfig.pattern;
        gridPosition = startGridPosition;
        moveInterval = 0.6f / Mathf.Max(1, rowConfig.speed);
    }

    public abstract void Activate(PlayerController player);
    protected abstract List<Vector2Int> BuildTrajectoryGridPositions();

    public virtual void MoveStep()
    {
        if (trajectoryGridPositions.Count == 0)
        {
            return;
        }

        gridPosition = trajectoryGridPositions[pathIndex];
        UpdateWorldPosition();
    }

    protected void UpdateWorldPosition()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        transform.position = GridManager.Instance.GridToWorld(gridPosition);
    }

    protected void PlayHitFlash(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        GameObject flashObject = new GameObject("HitFlash");
        flashObject.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -0.2f);
        flashObject.transform.localScale = new Vector3(2f, 2f, 1f);

        SpriteRenderer spriteRenderer = flashObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetOrCreateTrapSprite();
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        spriteRenderer.sortingOrder = 10;

        StartCoroutine(FadeHitFlash(flashObject, spriteRenderer));
    }

    private void CreateVisual()
    {
        GameObject visualObject = new GameObject("TrapVisual");
        visualObject.transform.SetParent(transform, false);

        SpriteRenderer spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetOrCreateTrapSprite();
        spriteRenderer.color = Color.red;
    }

    private static Sprite GetOrCreateTrapSprite()
    {
        if (cachedTrapSprite != null)
        {
            return cachedTrapSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        cachedTrapSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return cachedTrapSprite;
    }

    private IEnumerator FadeHitFlash(GameObject flashObject, SpriteRenderer spriteRenderer)
    {
        const float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (spriteRenderer != null)
            {
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(0.5f, 0f, normalizedTime);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        if (flashObject != null)
        {
            Destroy(flashObject);
        }
    }

    private void CacheTrajectory()
    {
        trajectoryGridPositions.Clear();
        trajectoryGridPositions.AddRange(BuildTrajectoryGridPositions());

        if (trajectoryGridPositions.Count == 0)
        {
            trajectoryGridPositions.Add(gridPosition);
        }

        pathIndex = FindPathIndex(gridPosition);
        pathDirection = direction >= 0 ? 1 : -1;
    }

    private int FindPathIndex(Vector2Int targetPosition)
    {
        for (int i = 0; i < trajectoryGridPositions.Count; i++)
        {
            if (trajectoryGridPositions[i] == targetPosition)
            {
                return i;
            }
        }

        return 0;
    }

    protected virtual void DrawTrajectory()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        List<Vector3> pathPoints = new List<Vector3>();

        for (int i = 0; i < trajectoryGridPositions.Count; i++)
        {
            Vector3 worldPoint = GridManager.Instance.GridToWorld(trajectoryGridPositions[i]);
            pathPoints.Add(new Vector3(worldPoint.x, worldPoint.y, 0.1f));
        }

        if (pattern == TrapPattern.Square && trajectoryGridPositions.Count > 0)
        {
            Vector3 firstPoint = GridManager.Instance.GridToWorld(trajectoryGridPositions[0]);
            pathPoints.Add(new Vector3(firstPoint.x, firstPoint.y, 0.1f));
        }

        if (pathPoints.Count == 0)
        {
            return;
        }

        GameObject trajectoryObject = new GameObject("TrapTrajectory");
        trajectoryObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = trajectoryObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = pathPoints.Count;
        lineRenderer.SetPositions(pathPoints.ToArray());
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 1f, 1f, 0.3f);
        lineRenderer.endColor = new Color(1f, 1f, 1f, 0.3f);
        lineRenderer.sortingOrder = -1;
    }
}
