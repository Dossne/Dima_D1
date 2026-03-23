using UnityEngine;

public class Boulder : TrapBase
{
    public override void MoveStep()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        int columnCount = GridManager.Instance.Columns;
        if (columnCount <= 0)
        {
            return;
        }

        int clampedSpeed = Mathf.Max(0, speed);
        int stepDirection = direction >= 0 ? 1 : -1;
        int nextX = gridPosition.x + (clampedSpeed * stepDirection);

        while (nextX < 0)
        {
            nextX += columnCount;
        }

        while (nextX >= columnCount)
        {
            nextX -= columnCount;
        }

        gridPosition = new Vector2Int(nextX, gridPosition.y);
        UpdateWorldPosition();
    }

    public override void Activate(PlayerController player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage();
        }
    }
}
