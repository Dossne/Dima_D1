using UnityEngine;

public abstract class TrapBase : MonoBehaviour
{
    [SerializeField] protected int speed = 1;
    [SerializeField] protected int direction = 1;
    [SerializeField] protected Vector2Int gridPosition;

    public Vector2Int GridPosition => gridPosition;

    protected virtual void Start()
    {
        UpdateWorldPosition();
    }

    public abstract void Activate(PlayerController player);

    public virtual void MoveStep()
    {
        int clampedSpeed = Mathf.Max(0, speed);
        int stepDirection = direction >= 0 ? 1 : -1;
        gridPosition += new Vector2Int(clampedSpeed * stepDirection, 0);
        UpdateWorldPosition();
    }

    protected void UpdateWorldPosition()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        transform.position = GridManager.Instance.GridToWorld(gridPosition);
    }
}
