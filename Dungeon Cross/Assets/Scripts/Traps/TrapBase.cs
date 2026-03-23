using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class TrapBase : MonoBehaviour
{
    [SerializeField] protected int speed = 1;
    [SerializeField] protected int direction = 1;
    [SerializeField] protected Vector2Int gridPosition;

    private static Sprite cachedTrapSprite;

    public Vector2Int GridPosition => gridPosition;

    protected virtual void Awake()
    {
        TrapManager.Instance?.RegisterTrap(this);
    }

    protected virtual void Start()
    {
        CreateVisual();
        UpdateWorldPosition();
        DrawTrajectory();
    }

    protected virtual void OnDestroy()
    {
        TrapManager.Instance?.UnregisterTrap(this);
    }

    public abstract void Activate(PlayerController player);

    public virtual void MoveStep()
    {
        int clampedSpeed = Mathf.Max(0, speed);
        int stepDirection = direction >= 0 ? 1 : -1;
        gridPosition += new Vector2Int(clampedSpeed * stepDirection, 0);
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

    protected virtual void DrawTrajectory()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        List<Vector3> pathPoints = new List<Vector3>();
        int row = Mathf.Clamp(gridPosition.y, 0, GridManager.Instance.Rows - 1);

        if (this is Boulder)
        {
            for (int column = 0; column < GridManager.Instance.Columns; column++)
            {
                Vector3 worldPoint = GridManager.Instance.GridToWorld(new Vector2Int(column, row));
                pathPoints.Add(new Vector3(worldPoint.x, worldPoint.y, 0.1f));
            }
        }
        else
        {
            int startColumn = Mathf.Clamp(gridPosition.x, 0, GridManager.Instance.Columns - 1);
            int endColumn = direction >= 0 ? GridManager.Instance.Columns - 1 : 0;
            int step = direction >= 0 ? 1 : -1;

            for (int column = startColumn; direction >= 0 ? column <= endColumn : column >= endColumn; column += step)
            {
                Vector3 worldPoint = GridManager.Instance.GridToWorld(new Vector2Int(column, row));
                pathPoints.Add(new Vector3(worldPoint.x, worldPoint.y, 0.1f));
            }
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
