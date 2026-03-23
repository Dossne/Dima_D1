using UnityEngine;

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
}
