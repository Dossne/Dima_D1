using UnityEngine;

[System.Serializable]
public class WallCellData
{
    [Range(0, 8)] public int column;
    [Range(0, 11)] public int row;

    public WallCellData()
    {
    }

    public WallCellData(int column, int row)
    {
        this.column = column;
        this.row = row;
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(column, row);
    }
}
