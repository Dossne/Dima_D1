using UnityEngine;

public class Arrow : TrapBase
{
    public override void MoveStep()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        base.MoveStep();

        if (!GridManager.Instance.IsWithinBounds(gridPosition))
        {
            Destroy(gameObject);
        }
    }

    public override void Activate(PlayerController player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage();
        }
    }
}
