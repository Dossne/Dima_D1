using System.Collections.Generic;
using UnityEngine;

public class Arrow : TrapBase
{
    protected override List<Vector2> BuildTrajectoryPoints()
    {
        List<Vector2> path = new List<Vector2>();

        if (GridManager.Instance == null)
        {
            path.Add(transform.position);
            return path;
        }

        int row = Mathf.Clamp(startGridPosition.y, 1, GridManager.Instance.Rows - 2);

        for (int column = 1; column < GridManager.Instance.Columns - 1; column++)
        {
            path.Add(GridManager.Instance.GetWorldPosition(column, row));
        }

        return path;
    }

    public override void MoveStep()
    {
        if (TrajectoryPoints.Count <= 1)
        {
            return;
        }

        int nextIndex = pathIndex + pathDirection;
        if (nextIndex < 0 || nextIndex >= TrajectoryPoints.Count)
        {
            pathDirection *= -1;
            direction *= -1;
            nextIndex = pathIndex + pathDirection;
        }

        pathIndex = Mathf.Clamp(nextIndex, 0, TrajectoryPoints.Count - 1);
        transform.position = TrajectoryPoints[pathIndex];
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
        CreateTokenLayer(parent, "Shadow", new Vector3(0.62f, 0.22f, 1f), new Color(0f, 0f, 0f, 0.2f), 2, new Vector3(0f, -0.28f, 0f));
        CreateTokenLayer(parent, "Body", new Vector3(0.34f, 0.56f, 1f), new Color(0.95f, 0.48f, 0.12f, 1f), 3, Vector3.zero);
        CreateTokenLayer(parent, "Mask", new Vector3(0.24f, 0.28f, 1f), new Color(0.48f, 0.12f, 0.06f, 1f), 4, new Vector3(0f, 0.1f, 0f));
        CreateTokenLayer(parent, "Eye", new Vector3(0.08f, 0.08f, 1f), new Color(1f, 0.92f, 0.65f, 1f), 5, new Vector3(0f, 0.12f, 0f));
        CreateTokenLayer(parent, "Tail", new Vector3(0.12f, 0.34f, 1f), new Color(1f, 0.74f, 0.18f, 1f), 4, new Vector3(0f, -0.34f, 0f));
        CreateTokenLayer(parent, "WingLeft", new Vector3(0.1f, 0.28f, 1f), new Color(1f, 0.7f, 0.28f, 0.95f), 3, new Vector3(-0.18f, 0f, 0f), 24f);
        CreateTokenLayer(parent, "WingRight", new Vector3(0.1f, 0.28f, 1f), new Color(1f, 0.7f, 0.28f, 0.95f), 3, new Vector3(0.18f, 0f, 0f), -24f);
    }

    protected override Color GetPrimaryDangerColor()
    {
        return new Color(1f, 0.62f, 0.18f);
    }
}
