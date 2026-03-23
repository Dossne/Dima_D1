using System.Collections.Generic;
using UnityEngine;

public class Arrow : TrapBase
{
    protected override List<Vector2Int> BuildTrajectoryGridPositions()
    {
        List<Vector2Int> path = new List<Vector2Int>();

        if (GridManager.Instance == null)
        {
            path.Add(gridPosition);
            return path;
        }

        int row = Mathf.Clamp(gridPosition.y, 0, GridManager.Instance.Rows - 1);

        for (int column = 0; column < GridManager.Instance.Columns; column++)
        {
            path.Add(new Vector2Int(column, row));
        }

        return path;
    }

    public override void MoveStep()
    {
        if (TrajectoryGridPositions.Count <= 1)
        {
            return;
        }

        int nextIndex = pathIndex + pathDirection;

        if (nextIndex < 0 || nextIndex >= TrajectoryGridPositions.Count)
        {
            pathDirection *= -1;
            direction *= -1;
            nextIndex = pathIndex + pathDirection;
        }

        pathIndex = Mathf.Clamp(nextIndex, 0, TrajectoryGridPositions.Count - 1);
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
}
