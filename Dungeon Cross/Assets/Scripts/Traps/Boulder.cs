using System.Collections.Generic;
using UnityEngine;

public class Boulder : TrapBase
{
    protected override List<Vector2Int> BuildTrajectoryGridPositions()
    {
        if (GridManager.Instance == null)
        {
            return new List<Vector2Int> { gridPosition };
        }

        switch (pattern)
        {
            case TrapPattern.Vertical:
                return BuildVerticalPath();
            case TrapPattern.Square:
                return BuildSquarePath();
            default:
                return BuildHorizontalPath();
        }
    }

    public override void MoveStep()
    {
        if (TrajectoryGridPositions.Count <= 1)
        {
            return;
        }

        if (pattern == TrapPattern.Square)
        {
            pathIndex = (pathIndex + 1) % TrajectoryGridPositions.Count;
        }
        else
        {
            int nextIndex = pathIndex + pathDirection;

            if (nextIndex < 0 || nextIndex >= TrajectoryGridPositions.Count)
            {
                pathDirection *= -1;
                nextIndex = pathIndex + pathDirection;
            }

            pathIndex = nextIndex;
        }

        gridPosition = TrajectoryGridPositions[pathIndex];

        if (GridManager.Instance != null && !GridManager.Instance.IsWithinBounds(gridPosition))
        {
            gridPosition = GridManager.Instance.ClampToBounds(gridPosition);
            pathDirection *= -1;
            direction *= -1;
        }

        UpdateWorldPosition();
    }

    public override void Activate(PlayerController player)
    {
        if (GameManager.Instance != null)
        {
            PlayHitFlash(player);
            GameManager.Instance.TakeDamage();
        }
    }

    private List<Vector2Int> BuildHorizontalPath()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int row = Mathf.Clamp(gridPosition.y, 0, GridManager.Instance.Rows - 1);

        for (int column = 0; column < GridManager.Instance.Columns; column++)
        {
            path.Add(new Vector2Int(column, row));
        }

        return path;
    }

    private List<Vector2Int> BuildVerticalPath()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int column = Mathf.Clamp(gridPosition.x, 0, GridManager.Instance.Columns - 1);
        int startRow = Mathf.Clamp(gridPosition.y, 0, GridManager.Instance.Rows - 1);
        int endRow = direction >= 0
            ? Mathf.Min(startRow + 3, GridManager.Instance.Rows - 1)
            : Mathf.Max(startRow - 3, 0);

        if (direction >= 0)
        {
            for (int row = startRow; row <= endRow; row++)
            {
                path.Add(new Vector2Int(column, row));
            }
        }
        else
        {
            for (int row = endRow; row <= startRow; row++)
            {
                path.Add(new Vector2Int(column, row));
            }
        }

        if (path.Count == 1)
        {
            int alternateRow = startRow > 0 ? startRow - 1 : Mathf.Min(startRow + 1, GridManager.Instance.Rows - 1);
            path.Add(new Vector2Int(column, alternateRow));
        }

        return path;
    }

    private List<Vector2Int> BuildSquarePath()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int anchorX = Mathf.Clamp(gridPosition.x, 0, Mathf.Max(0, GridManager.Instance.Columns - 3));
        int anchorY = Mathf.Clamp(gridPosition.y, 2, GridManager.Instance.Rows - 1);

        path.Add(new Vector2Int(anchorX, anchorY));
        path.Add(new Vector2Int(anchorX + 1, anchorY));
        path.Add(new Vector2Int(anchorX + 2, anchorY));
        path.Add(new Vector2Int(anchorX + 2, anchorY - 1));
        path.Add(new Vector2Int(anchorX + 2, anchorY - 2));
        path.Add(new Vector2Int(anchorX + 1, anchorY - 2));
        path.Add(new Vector2Int(anchorX, anchorY - 2));
        path.Add(new Vector2Int(anchorX, anchorY - 1));

        return path;
    }
}
