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
        BuildDoors();
    }

    private void BuildFloor()
    {
        Color floorColor = new Color(58f / 255f, 74f / 255f, 90f / 255f, 1f);

        for (int row = 0; row < GridManager.Instance.Rows; row++)
        {
            for (int column = 0; column < GridManager.Instance.Columns; column++)
            {
                CreateQuad($"Floor_{column}_{row}", GridManager.Instance.GetWorldPosition(column, row), Vector3.one, floorColor);
            }
        }
    }

    private void BuildWalls()
    {
        Color wallColor = new Color(42f / 255f, 42f / 255f, 42f / 255f, 1f);
        int columns = GridManager.Instance.Columns;
        int rows = GridManager.Instance.Rows;

        for (int column = 0; column < columns; column++)
        {
            CreateQuad($"Wall_Top_{column}", GetPerimeterWorldPosition(column, rows), Vector3.one, wallColor);
            CreateQuad($"Wall_Bottom_{column}", GetPerimeterWorldPosition(column, -1), Vector3.one, wallColor);
        }

        for (int row = 0; row < rows; row++)
        {
            CreateQuad($"Wall_Left_{row}", GetPerimeterWorldPosition(-1, row), Vector3.one, wallColor);
            CreateQuad($"Wall_Right_{row}", GetPerimeterWorldPosition(columns, row), Vector3.one, wallColor);
        }

        CreateQuad("Wall_Corner_BottomLeft", GetPerimeterWorldPosition(-1, -1), Vector3.one, wallColor);
        CreateQuad("Wall_Corner_BottomRight", GetPerimeterWorldPosition(columns, -1), Vector3.one, wallColor);
        CreateQuad("Wall_Corner_TopLeft", GetPerimeterWorldPosition(-1, rows), Vector3.one, wallColor);
        CreateQuad("Wall_Corner_TopRight", GetPerimeterWorldPosition(columns, rows), Vector3.one, wallColor);
    }

    private void BuildDoors()
    {
        Vector3 entryPosition = GridManager.Instance.GetWorldPosition(4, 0) + new Vector3(0f, 0f, 1f);
        Vector3 exitPosition = GridManager.Instance.GetWorldPosition(4, 11) + new Vector3(0f, 0f, 1f);

        CreateQuad("EntryDoor", entryPosition, new Vector3(1f, 1.5f, 1f), new Color(76f / 255f, 175f / 255f, 80f / 255f, 1f));
        CreateQuad("ExitDoor", exitPosition, new Vector3(1f, 1.5f, 1f), new Color(1f, 215f / 255f, 0f, 1f));
    }

    private void CreateQuad(string objectName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject quadObject = new GameObject(objectName);
        quadObject.transform.SetParent(transform, false);
        quadObject.transform.position = position;
        quadObject.transform.localScale = scale;

        SpriteRenderer spriteRenderer = quadObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetOrCreateSprite();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = -10;
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
