using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private int columns = 9;
    [SerializeField] private int rows = 12;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 gridCenter = Vector2.zero;

    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;
    public Vector2 Origin => origin;
    public Rect WorldBounds => worldBounds;

    private Vector2 origin;
    private Rect worldBounds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RecalculateOrigin();
        FitCameraToGrid();
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        cellSize = Mathf.Max(0.01f, cellSize);
        RecalculateOrigin();
        FitCameraToGrid();
    }

    public Vector2Int ClampToBounds(Vector2Int gridPosition)
    {
        return new Vector2Int(
            Mathf.Clamp(gridPosition.x, 0, columns - 1),
            Mathf.Clamp(gridPosition.y, 0, rows - 1));
    }

    public bool IsWithinBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < columns
            && gridPosition.y >= 0 && gridPosition.y < rows;
    }

    public bool IsWithinWorldBounds(Vector2 worldPosition, float padding = 0f)
    {
        return worldPosition.x >= worldBounds.xMin + padding
            && worldPosition.x <= worldBounds.xMax - padding
            && worldPosition.y >= worldBounds.yMin + padding
            && worldPosition.y <= worldBounds.yMax - padding;
    }

    public Vector2 ClampWorldPosition(Vector2 worldPosition, float padding = 0f)
    {
        return new Vector2(
            Mathf.Clamp(worldPosition.x, worldBounds.xMin + padding, worldBounds.xMax - padding),
            Mathf.Clamp(worldPosition.y, worldBounds.yMin + padding, worldBounds.yMax - padding));
    }

    public Vector2Int WorldToGrid(Vector2 worldPosition)
    {
        int column = Mathf.RoundToInt((worldPosition.x - origin.x) / cellSize);
        int row = Mathf.RoundToInt((worldPosition.y - origin.y) / cellSize);
        return ClampToBounds(new Vector2Int(column, row));
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        Vector2Int clampedPosition = ClampToBounds(gridPosition);
        return new Vector3(
            origin.x + (clampedPosition.x * cellSize),
            origin.y + (clampedPosition.y * cellSize),
            0f);
    }

    public Vector3 GetWorldPosition(int column, int row)
    {
        return GridToWorld(new Vector2Int(column, row));
    }

    public Rect GetCellWorldRect(Vector2Int gridPosition)
    {
        Vector3 center = GridToWorld(gridPosition);
        float halfSize = cellSize * 0.5f;
        return new Rect(center.x - halfSize, center.y - halfSize, cellSize, cellSize);
    }

    public bool CircleIntersectsCell(Vector2 center, float radius, Vector2Int gridPosition)
    {
        Rect cellRect = GetCellWorldRect(gridPosition);
        float closestX = Mathf.Clamp(center.x, cellRect.xMin, cellRect.xMax);
        float closestY = Mathf.Clamp(center.y, cellRect.yMin, cellRect.yMax);
        float deltaX = center.x - closestX;
        float deltaY = center.y - closestY;
        return (deltaX * deltaX) + (deltaY * deltaY) <= radius * radius;
    }

    public Vector3 GetEntryWorldPosition()
    {
        return GetWorldPosition(columns / 2, 0);
    }

    public Vector3 GetExitWorldPosition()
    {
        return GetWorldPosition(columns / 2, rows - 1);
    }

    public void ConfigureGridCenter(Vector2 newGridCenter)
    {
        gridCenter = newGridCenter;
        RecalculateOrigin();
    }

    public void FitCameraToGrid()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            cam = FindObjectOfType<Camera>();
        }

        if (cam == null)
        {
            return;
        }

        float padding = 1f;
        float gridWorldHeight = rows * cellSize;
        float gridWorldWidth = columns * cellSize;
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : cam.aspect;
        float sizeByHeight = gridWorldHeight / 2f + padding;
        float sizeByWidth = (gridWorldWidth / 2f + padding) / aspect;

        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);

        Vector2 boundsCenter = worldBounds.center;
        cam.transform.position = new Vector3(boundsCenter.x, boundsCenter.y, -10f);
    }

    private void RecalculateOrigin()
    {
        float width = (columns - 1) * cellSize;
        float height = (rows - 1) * cellSize;
        origin = gridCenter - new Vector2(width * 0.5f, height * 0.5f);

        worldBounds = new Rect(
            origin.x - cellSize * 0.5f,
            origin.y - cellSize * 0.5f,
            columns * cellSize,
            rows * cellSize);
    }
}
