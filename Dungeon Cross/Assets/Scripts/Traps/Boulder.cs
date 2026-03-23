using System.Collections.Generic;
using UnityEngine;

public class Boulder : TrapBase
{
    protected override List<Vector2> BuildTrajectoryPoints()
    {
        if (GridManager.Instance == null)
        {
            return new List<Vector2> { transform.position };
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
        if (TrajectoryPoints.Count <= 1)
        {
            return;
        }

        if (pattern == TrapPattern.Square)
        {
            pathIndex = (pathIndex + 1) % TrajectoryPoints.Count;
        }
        else
        {
            int nextIndex = pathIndex + pathDirection;
            if (nextIndex < 0 || nextIndex >= TrajectoryPoints.Count)
            {
                pathDirection *= -1;
                direction *= -1;
                nextIndex = pathIndex + pathDirection;
            }

            pathIndex = Mathf.Clamp(nextIndex, 0, TrajectoryPoints.Count - 1);
        }

        transform.position = TrajectoryPoints[pathIndex];

        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.ClampWorldPosition(transform.position, 0.1f);
        }
    }

    public override void Activate(PlayerController player)
    {
        if (GameManager.Instance == null || player == null || !player.CanTakeDamage())
        {
            return;
        }

        PlayHitFlash(player);
        player.NotifyDamaged();
        GameManager.Instance.TakeDamage();
    }

    protected override void BuildTokenVisual(Transform parent)
    {
        CreateTokenLayer(parent, "Shadow", new Vector3(0.64f, 0.26f, 1f), new Color(0f, 0f, 0f, 0.22f), 2, new Vector3(0f, -0.28f, 0f));
        CreateTokenLayer(parent, "Body", new Vector3(0.56f, 0.52f, 1f), new Color(0.46f, 0.08f, 0.1f, 1f), 3, Vector3.zero);
        CreateTokenLayer(parent, "Shell", new Vector3(0.42f, 0.36f, 1f), new Color(0.74f, 0.16f, 0.16f, 1f), 4, new Vector3(0f, 0.06f, 0f));
        CreateTokenLayer(parent, "HornLeft", new Vector3(0.12f, 0.2f, 1f), new Color(0.9f, 0.84f, 0.72f, 1f), 5, new Vector3(-0.22f, 0.23f, 0f), 28f);
        CreateTokenLayer(parent, "HornRight", new Vector3(0.12f, 0.2f, 1f), new Color(0.9f, 0.84f, 0.72f, 1f), 5, new Vector3(0.22f, 0.23f, 0f), -28f);
        CreateTokenLayer(parent, "EyeLeft", new Vector3(0.06f, 0.06f, 1f), new Color(1f, 0.78f, 0.3f, 1f), 6, new Vector3(-0.12f, 0.08f, 0f));
        CreateTokenLayer(parent, "EyeRight", new Vector3(0.06f, 0.06f, 1f), new Color(1f, 0.78f, 0.3f, 1f), 6, new Vector3(0.12f, 0.08f, 0f));
    }

    protected override Color GetPrimaryDangerColor()
    {
        return new Color(0.96f, 0.24f, 0.24f);
    }

    private List<Vector2> BuildHorizontalPath()
    {
        List<Vector2> path = new List<Vector2>();
        int row = Mathf.Clamp(startGridPosition.y, 1, GridManager.Instance.Rows - 2);

        for (int column = 1; column < GridManager.Instance.Columns - 1; column++)
        {
            path.Add(GridManager.Instance.GetWorldPosition(column, row));
        }

        return path;
    }

    private List<Vector2> BuildVerticalPath()
    {
        List<Vector2> path = new List<Vector2>();
        int column = Mathf.Clamp(startGridPosition.x, 1, GridManager.Instance.Columns - 2);
        int startRow = Mathf.Clamp(startGridPosition.y, 1, GridManager.Instance.Rows - 2);
        int minRow = Mathf.Max(1, startRow - 2);
        int maxRow = Mathf.Min(GridManager.Instance.Rows - 2, startRow + 2);

        for (int row = minRow; row <= maxRow; row++)
        {
            path.Add(GridManager.Instance.GetWorldPosition(column, row));
        }

        return path;
    }

    private List<Vector2> BuildSquarePath()
    {
        List<Vector2> path = new List<Vector2>();
        int anchorX = Mathf.Clamp(startGridPosition.x, 1, GridManager.Instance.Columns - 3);
        int anchorY = Mathf.Clamp(startGridPosition.y, 3, GridManager.Instance.Rows - 2);

        path.Add(GridManager.Instance.GetWorldPosition(anchorX, anchorY));
        path.Add(GridManager.Instance.GetWorldPosition(anchorX + 1, anchorY));
        path.Add(GridManager.Instance.GetWorldPosition(anchorX + 2, anchorY));
        path.Add(GridManager.Instance.GetWorldPosition(anchorX + 2, anchorY - 1));
        path.Add(GridManager.Instance.GetWorldPosition(anchorX + 2, anchorY - 2));
        path.Add(GridManager.Instance.GetWorldPosition(anchorX + 1, anchorY - 2));
        path.Add(GridManager.Instance.GetWorldPosition(anchorX, anchorY - 2));
        path.Add(GridManager.Instance.GetWorldPosition(anchorX, anchorY - 1));

        return path;
    }
}
