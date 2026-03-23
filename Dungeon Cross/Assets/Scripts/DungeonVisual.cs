using UnityEngine;

public class DungeonVisual : MonoBehaviour
{
    private static Sprite cachedSprite;

    public void Build()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        ClearChildren();
        BuildFloor();
        BuildWalls();
        BuildInteriorWalls();
        BuildDoors();
    }

    private void BuildFloor()
    {
        for (int row = 0; row < GridManager.Instance.Rows; row++)
        {
            for (int column = 0; column < GridManager.Instance.Columns; column++)
            {
                BuildFloorTile(column, row);
            }
        }
    }

    private void BuildFloorTile(int column, int row)
    {
        Vector3 position = GridManager.Instance.GetWorldPosition(column, row);
        Color baseColor = (column + row) % 2 == 0
            ? new Color(52f / 255f, 66f / 255f, 78f / 255f, 1f)
            : new Color(60f / 255f, 76f / 255f, 90f / 255f, 1f);

        GameObject tileRoot = CreateVisualRoot($"Floor_{column}_{row}", position, -10);
        CreateLayer(tileRoot.transform, "Base", Vector3.one, baseColor, 0);
        CreateLayer(tileRoot.transform, "Inset", new Vector3(0.86f, 0.86f, 1f), new Color(0.18f, 0.22f, 0.28f, 0.2f), 1);

        if ((column + row) % 3 == 0)
        {
            CreateLayer(tileRoot.transform, "CrackA", new Vector3(0.1f, 0.72f, 1f), new Color(0.12f, 0.15f, 0.19f, 0.45f), 2, new Vector3(-0.15f, 0f, 0f), 35f);
        }

        if ((column * 2 + row) % 4 == 0)
        {
            CreateLayer(tileRoot.transform, "CrackB", new Vector3(0.08f, 0.45f, 1f), new Color(0.08f, 0.11f, 0.15f, 0.35f), 2, new Vector3(0.18f, -0.08f, 0f), -30f);
        }

        CreateCornerShade(tileRoot.transform, new Vector3(-0.33f, 0.33f, 0f));
        CreateCornerShade(tileRoot.transform, new Vector3(0.33f, -0.33f, 0f));
    }

    private void BuildWalls()
    {
        int columns = GridManager.Instance.Columns;
        int rows = GridManager.Instance.Rows;

        for (int column = 0; column < columns; column++)
        {
            BuildWallTile($"Wall_Top_{column}", GetPerimeterWorldPosition(column, rows), true, column);
            BuildWallTile($"Wall_Bottom_{column}", GetPerimeterWorldPosition(column, -1), true, column + 7);
        }

        for (int row = 0; row < rows; row++)
        {
            BuildWallTile($"Wall_Left_{row}", GetPerimeterWorldPosition(-1, row), false, row);
            BuildWallTile($"Wall_Right_{row}", GetPerimeterWorldPosition(columns, row), false, row + 5);
        }

        BuildWallTile("Wall_Corner_BottomLeft", GetPerimeterWorldPosition(-1, -1), true, 1);
        BuildWallTile("Wall_Corner_BottomRight", GetPerimeterWorldPosition(columns, -1), true, 2);
        BuildWallTile("Wall_Corner_TopLeft", GetPerimeterWorldPosition(-1, rows), true, 3);
        BuildWallTile("Wall_Corner_TopRight", GetPerimeterWorldPosition(columns, rows), true, 4);
    }

    private void BuildWallTile(string objectName, Vector3 position, bool horizontal, int seed)
    {
        GameObject tileRoot = CreateVisualRoot(objectName, position, -12);
        Color baseColor = seed % 2 == 0
            ? new Color(0.18f, 0.18f, 0.2f, 1f)
            : new Color(0.22f, 0.22f, 0.24f, 1f);

        CreateLayer(tileRoot.transform, "Base", Vector3.one, baseColor, 0);
        CreateLayer(tileRoot.transform, "Inset", new Vector3(0.9f, 0.9f, 1f), new Color(0.32f, 0.32f, 0.36f, 0.18f), 1);

        if (horizontal)
        {
            CreateLayer(tileRoot.transform, "StoneLineA", new Vector3(0.92f, 0.08f, 1f), new Color(0.05f, 0.05f, 0.06f, 0.55f), 2, new Vector3(0f, 0.18f, 0f));
            CreateLayer(tileRoot.transform, "StoneLineB", new Vector3(0.72f, 0.08f, 1f), new Color(0.38f, 0.38f, 0.42f, 0.15f), 2, new Vector3(0f, -0.18f, 0f));
        }
        else
        {
            CreateLayer(tileRoot.transform, "StoneLineA", new Vector3(0.08f, 0.92f, 1f), new Color(0.05f, 0.05f, 0.06f, 0.55f), 2, new Vector3(0.18f, 0f, 0f));
            CreateLayer(tileRoot.transform, "StoneLineB", new Vector3(0.08f, 0.72f, 1f), new Color(0.38f, 0.38f, 0.42f, 0.15f), 2, new Vector3(-0.18f, 0f, 0f));
        }
    }

    private void BuildDoors()
    {
        BuildDoor("EntryDoor", GridManager.Instance.GetWorldPosition(4, 0) + new Vector3(0f, 0f, 1f), new Color(76f / 255f, 175f / 255f, 80f / 255f, 1f), new Color(0.82f, 1f, 0.84f, 0.28f));
        BuildDoor("ExitDoor", GridManager.Instance.GetWorldPosition(4, 11) + new Vector3(0f, 0f, 1f), new Color(1f, 215f / 255f, 0f, 1f), new Color(1f, 0.96f, 0.7f, 0.35f));
    }

    private void BuildInteriorWalls()
    {
        if (LevelManager.Instance == null)
        {
            return;
        }

        foreach (Vector2Int wallCell in LevelManager.Instance.ActiveWallCells)
        {
            BuildInteriorWallTile(wallCell);
        }
    }

    private void BuildInteriorWallTile(Vector2Int wallCell)
    {
        Vector3 position = GridManager.Instance.GetWorldPosition(wallCell.x, wallCell.y) + new Vector3(0f, 0f, 0.6f);
        GameObject root = CreateVisualRoot($"InteriorWall_{wallCell.x}_{wallCell.y}", position, -3);
        CreateLayer(root.transform, "Base", new Vector3(0.94f, 0.94f, 1f), new Color(0.2f, 0.2f, 0.22f, 1f), 0);
        CreateLayer(root.transform, "Inset", new Vector3(0.76f, 0.76f, 1f), new Color(0.36f, 0.36f, 0.4f, 0.25f), 1);
        CreateLayer(root.transform, "Cap", new Vector3(0.98f, 0.12f, 1f), new Color(0.48f, 0.48f, 0.52f, 0.22f), 2, new Vector3(0f, 0.26f, 0f));
        CreateLayer(root.transform, "CreaseA", new Vector3(0.1f, 0.82f, 1f), new Color(0.06f, 0.06f, 0.07f, 0.45f), 2, new Vector3(-0.18f, 0f, 0f));
        CreateLayer(root.transform, "CreaseB", new Vector3(0.08f, 0.66f, 1f), new Color(0.5f, 0.5f, 0.56f, 0.18f), 2, new Vector3(0.17f, 0f, 0f));
    }

    private void BuildDoor(string objectName, Vector3 position, Color mainColor, Color glowColor)
    {
        GameObject root = CreateVisualRoot(objectName, position, -4);
        CreateLayer(root.transform, "Frame", new Vector3(1.08f, 1.58f, 1f), new Color(0.14f, 0.1f, 0.07f, 1f), 0);
        CreateLayer(root.transform, "Inner", new Vector3(0.82f, 1.28f, 1f), mainColor, 1, new Vector3(0f, -0.05f, 0f));
        CreateLayer(root.transform, "Glow", new Vector3(0.46f, 0.82f, 1f), glowColor, 2, new Vector3(0f, 0.08f, 0f));
        CreateLayer(root.transform, "Lintel", new Vector3(1.18f, 0.14f, 1f), new Color(0.3f, 0.22f, 0.1f, 1f), 3, new Vector3(0f, 0.67f, 0f));
    }

    private GameObject CreateVisualRoot(string objectName, Vector3 position, int sortingOrder)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(transform, false);
        root.transform.position = position;
        root.transform.localScale = Vector3.one;
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = GetOrCreateSprite();
        renderer.color = new Color(0f, 0f, 0f, 0f);
        renderer.sortingOrder = sortingOrder;
        return root;
    }

    private void CreateLayer(Transform parent, string objectName, Vector3 scale, Color color, int orderOffset, Vector3? localPosition = null, float rotation = 0f)
    {
        GameObject layerObject = new GameObject(objectName);
        layerObject.transform.SetParent(parent, false);
        layerObject.transform.localPosition = localPosition ?? Vector3.zero;
        layerObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        layerObject.transform.localScale = scale;

        SpriteRenderer spriteRenderer = layerObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetOrCreateSprite();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = parent.GetComponent<SpriteRenderer>().sortingOrder + orderOffset;
    }

    private void CreateCornerShade(Transform parent, Vector3 localPosition)
    {
        CreateLayer(parent, "CornerShade", new Vector3(0.22f, 0.22f, 1f), new Color(0.08f, 0.1f, 0.12f, 0.35f), 3, localPosition, 45f);
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private Vector3 GetPerimeterWorldPosition(int column, int row)
    {
        float cellSize = GridManager.Instance.CellSize;
        int clampedColumn = Mathf.Clamp(column, 0, GridManager.Instance.Columns - 1);
        int clampedRow = Mathf.Clamp(row, 0, GridManager.Instance.Rows - 1);
        Vector3 basePosition = GridManager.Instance.GetWorldPosition(clampedColumn, clampedRow);

        float offsetX = 0f;
        float offsetY = 0f;

        if (column < 0)
        {
            offsetX = -cellSize;
        }
        else if (column >= GridManager.Instance.Columns)
        {
            offsetX = cellSize;
        }

        if (row < 0)
        {
            offsetY = -cellSize;
        }
        else if (row >= GridManager.Instance.Rows)
        {
            offsetY = cellSize;
        }

        return new Vector3(basePosition.x + offsetX, basePosition.y + offsetY, 1f);
    }

    private static Sprite GetOrCreateSprite()
    {
        if (cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        cachedSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return cachedSprite;
    }
}
