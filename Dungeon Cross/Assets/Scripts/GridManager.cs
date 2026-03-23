using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 9;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 gridCenter = Vector2.zero;

    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;

    private Vector2 origin;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RecalculateOrigin();
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        cellSize = Mathf.Max(0.01f, cellSize);
        RecalculateOrigin();
    }

    public Vector2Int ClampToBounds(Vector2Int gridPosition)
    {
        int x = Mathf.Clamp(gridPosition.x, 0, columns - 1);
        int y = Mathf.Clamp(gridPosition.y, 0, rows - 1);
        return new Vector2Int(x, y);
    }

    public bool IsWithinBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < columns
            && gridPosition.y >= 0 && gridPosition.y < rows;
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        Vector2 clampedPosition = ClampToBounds(gridPosition);
        float x = origin.x + (clampedPosition.x * cellSize);
        float y = origin.y + (clampedPosition.y * cellSize);
        return new Vector3(x, y, 0f);
    }

    private void RecalculateOrigin()
    {
        float width = (columns - 1) * cellSize;
        float height = (rows - 1) * cellSize;
        origin = gridCenter - new Vector2(width * 0.5f, height * 0.5f);
    }
}
