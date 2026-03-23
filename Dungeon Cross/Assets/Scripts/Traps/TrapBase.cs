using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TrapBase : MonoBehaviour
{
    [SerializeField] protected float moveInterval = 0.6f;
    [SerializeField] protected int direction = 1;
    [SerializeField] protected Vector2Int startGridPosition;
    [SerializeField] protected TrapPattern pattern = TrapPattern.Horizontal;
    [SerializeField] protected float dangerRadius = 0.45f;
    [SerializeField] protected bool useOrbitingBlade;
    [SerializeField] protected float orbitRadius = 0.7f;
    [SerializeField] protected float orbitBladeRadius = 0.28f;
    [SerializeField] protected float orbitAngularSpeed = 180f;

    private static Sprite cachedTrapSprite;
    private readonly List<Vector2> trajectoryPoints = new List<Vector2>();
    private float moveTimer;
    private float orbitAngle;
    private Transform bladeRoot;
    private TrailRenderer bladeTrail;
    protected int pathIndex;
    protected int pathDirection = 1;

    public Vector2 Position => transform.position;
    public Vector2Int GridPosition => GridManager.Instance != null ? GridManager.Instance.WorldToGrid(transform.position) : startGridPosition;
    public TrapPattern Pattern => pattern;
    public float DangerRadius => dangerRadius;
    public bool HasOrbitingBlade => useOrbitingBlade;
    public float OrbitRadius => orbitRadius;
    public float OrbitBladeRadius => orbitBladeRadius;
    public float OrbitAngularSpeed => orbitAngularSpeed;
    protected IReadOnlyList<Vector2> TrajectoryPoints => trajectoryPoints;

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
        DrawDangerZone();
        CreateBladeVisual();
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
        orbitAngle = Mathf.Repeat(orbitAngle + orbitAngularSpeed * Time.unscaledDeltaTime, 360f);
        UpdateBladeVisual();

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

    public virtual void Configure(RoomHazardConfig config)
    {
        direction = config.direction >= 0 ? 1 : -1;
        pattern = config.pattern;
        startGridPosition = new Vector2Int(config.startColumn, config.startRow);
        moveInterval = Mathf.Max(0.05f, config.moveInterval);
        dangerRadius = Mathf.Max(0.1f, config.dangerRadius);
        useOrbitingBlade = config.useOrbitingBlade;
        orbitRadius = Mathf.Max(0.1f, config.orbitRadius);
        orbitBladeRadius = Mathf.Max(0.05f, config.orbitBladeRadius);
        orbitAngularSpeed = Mathf.Max(1f, config.orbitAngularSpeed);
        orbitAngle = direction >= 0 ? 0f : 180f;
    }

    public abstract void Activate(PlayerController player);
    protected abstract List<Vector2> BuildTrajectoryPoints();
    protected abstract void BuildTokenVisual(Transform parent);
    protected abstract Color GetPrimaryDangerColor();

    public virtual void MoveStep()
    {
        if (trajectoryPoints.Count == 0)
        {
            return;
        }

        transform.position = trajectoryPoints[pathIndex];
    }

    public bool IsPlayerInDanger(PlayerController player)
    {
        if (player == null || !player.CanTakeDamage())
        {
            return false;
        }

        float combinedRadius = dangerRadius + player.PlayerRadius;
        if (Vector2.Distance(transform.position, player.CurrentWorldPosition) <= combinedRadius)
        {
            return true;
        }

        if (!useOrbitingBlade)
        {
            return false;
        }

        return Vector2.Distance(GetOrbitingBladePosition(), player.CurrentWorldPosition) <= orbitBladeRadius + player.PlayerRadius;
    }

    protected Vector2 GetOrbitingBladePosition()
    {
        Vector2 orbitOffset = (Vector2)(Quaternion.Euler(0f, 0f, orbitAngle) * (Vector3.right * orbitRadius));
        return (Vector2)transform.position + orbitOffset;
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

    protected void CreateTokenLayer(Transform parent, string objectName, Vector3 scale, Color color, int sortingOrder, Vector3 localPosition, float rotation = 0f)
    {
        GameObject layer = new GameObject(objectName);
        layer.transform.SetParent(parent, false);
        layer.transform.localPosition = localPosition;
        layer.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        layer.transform.localScale = scale;

        SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = GetOrCreateTrapSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private void UpdateWorldPosition()
    {
        if (trajectoryPoints.Count == 0)
        {
            transform.position = GridManager.Instance != null ? GridManager.Instance.GetWorldPosition(startGridPosition.x, startGridPosition.y) : Vector3.zero;
            return;
        }

        pathIndex = Mathf.Clamp(pathIndex, 0, trajectoryPoints.Count - 1);
        transform.position = trajectoryPoints[pathIndex];
    }

    private void CreateVisual()
    {
        GameObject visualRoot = new GameObject("TrapVisual");
        visualRoot.transform.SetParent(transform, false);
        BuildTokenVisual(visualRoot.transform);
    }

    private void CreateBladeVisual()
    {
        if (!useOrbitingBlade)
        {
            return;
        }

        GameObject bladeObject = new GameObject("BladeRoot");
        bladeObject.transform.SetParent(transform, false);
        bladeRoot = bladeObject.transform;

        CreateTokenLayer(bladeRoot, "Glow", new Vector3(0.18f, 0.82f, 1f), new Color(1f, 0.82f, 0.22f, 0.24f), 7, Vector3.zero);
        CreateTokenLayer(bladeRoot, "Blade", new Vector3(0.1f, 0.7f, 1f), new Color(0.92f, 0.96f, 1f, 1f), 8, new Vector3(0f, 0.08f, 0f));
        CreateTokenLayer(bladeRoot, "Core", new Vector3(0.05f, 0.52f, 1f), new Color(0.7f, 0.82f, 1f, 0.55f), 9, new Vector3(0f, 0.12f, 0f));
        CreateTokenLayer(bladeRoot, "Guard", new Vector3(0.22f, 0.06f, 1f), new Color(0.88f, 0.7f, 0.2f, 1f), 10, new Vector3(0f, -0.1f, 0f));
        CreateTokenLayer(bladeRoot, "Hilt", new Vector3(0.06f, 0.2f, 1f), new Color(0.45f, 0.22f, 0.1f, 1f), 9, new Vector3(0f, -0.22f, 0f));

        bladeTrail = bladeObject.AddComponent<TrailRenderer>();
        bladeTrail.time = 0.12f;
        bladeTrail.minVertexDistance = 0.02f;
        bladeTrail.startWidth = 0.16f;
        bladeTrail.endWidth = 0.02f;
        bladeTrail.sortingOrder = 6;
        bladeTrail.material = new Material(Shader.Find("Sprites/Default"));

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.55f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.5f),
                new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.75f, 0f),
                new GradientAlphaKey(0.2f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        bladeTrail.colorGradient = gradient;
        bladeTrail.emitting = true;
        UpdateBladeVisual();
    }

    private void UpdateBladeVisual()
    {
        if (bladeRoot == null)
        {
            return;
        }

        Vector2 bladePosition = GetOrbitingBladePosition();
        Vector2 radialDirection = (bladePosition - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(radialDirection.y, radialDirection.x) * Mathf.Rad2Deg - 90f;

        bladeRoot.localPosition = (Vector3)(bladePosition - (Vector2)transform.position);
        bladeRoot.localRotation = Quaternion.Euler(0f, 0f, angle + 18f);
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
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(0.5f, 0f, Mathf.Clamp01(elapsed / duration));
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
        trajectoryPoints.Clear();
        trajectoryPoints.AddRange(BuildTrajectoryPoints());

        if (trajectoryPoints.Count == 0 && GridManager.Instance != null)
        {
            trajectoryPoints.Add(GridManager.Instance.GetWorldPosition(startGridPosition.x, startGridPosition.y));
        }

        pathIndex = 0;
        pathDirection = direction >= 0 ? 1 : -1;
    }

    protected virtual void DrawTrajectory()
    {
        if (trajectoryPoints.Count == 0)
        {
            return;
        }

        List<Vector3> pathPoints = new List<Vector3>();
        for (int i = 0; i < trajectoryPoints.Count; i++)
        {
            pathPoints.Add(new Vector3(trajectoryPoints[i].x, trajectoryPoints[i].y, 0.1f));
        }

        if (pattern == TrapPattern.Square && pathPoints.Count > 0)
        {
            pathPoints.Add(pathPoints[0]);
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
        lineRenderer.startColor = new Color(1f, 1f, 1f, 0.22f);
        lineRenderer.endColor = new Color(1f, 1f, 1f, 0.22f);
        lineRenderer.sortingOrder = -1;
    }

    protected virtual void DrawDangerZone()
    {
        Color primary = GetPrimaryDangerColor();
        DrawCircle("DangerRadius", dangerRadius, new Color(primary.r, primary.g, primary.b, 0.16f), 0.03f);

        if (useOrbitingBlade)
        {
            DrawCircle("BladeOrbit", orbitRadius, new Color(1f, 0.82f, 0.18f, 0.14f), 0.02f);
        }
    }

    protected void DrawCircle(string objectName, float radius, Color color, float width)
    {
        const int segments = 40;
        GameObject circleObject = new GameObject(objectName);
        circleObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = circleObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.sortingOrder = -1;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }
}
