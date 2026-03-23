using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private List<TrapRow> trapRows = new List<TrapRow>();

    private readonly List<TrapBase> spawnedTraps = new List<TrapBase>();

    private static readonly FieldInfo SpeedField = typeof(TrapBase).GetField("speed", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo DirectionField = typeof(TrapBase).GetField("direction", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo GridPositionField = typeof(TrapBase).GetField("gridPosition", BindingFlags.Instance | BindingFlags.NonPublic);

    public IReadOnlyList<TrapRow> TrapRows => trapRows;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SpawnLevel(int level)
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("LevelManager requires a GridManager in the scene.");
            return;
        }

        ClearSpawnedTraps();

        trapRows = BuildLevel(level);

        for (int i = 0; i < trapRows.Count; i++)
        {
            SpawnTrapForRow(trapRows[i]);
        }

        GridManager.Instance.FitCameraToGrid();
    }

    private void SpawnTrapForRow(TrapRow rowConfig)
    {
        if (!TryCreateTrap(rowConfig.trapType, out TrapBase trap))
        {
            Debug.LogWarning($"Unsupported trap type '{rowConfig.trapType}' on row {rowConfig.rowIndex}.");
            return;
        }

        int spawnColumn = rowConfig.direction >= 0 ? 0 : GridManager.Instance.Columns - 1;
        Vector2Int gridPosition = new Vector2Int(spawnColumn, Mathf.Clamp(rowConfig.rowIndex, 0, GridManager.Instance.Rows - 1));

        ConfigureTrap(trap, rowConfig.speed, rowConfig.direction, gridPosition);
        trap.name = $"{trap.GetType().Name}_Row_{gridPosition.y}";
        spawnedTraps.Add(trap);
    }

    private bool TryCreateTrap(string trapType, out TrapBase trap)
    {
        GameObject trapObject = new GameObject();

        switch ((trapType ?? string.Empty).ToLowerInvariant())
        {
            case "boulder":
                trapObject.name = "Boulder";
                trap = trapObject.AddComponent<Boulder>();
                return true;
            case "arrow":
                trapObject.name = "Arrow";
                trap = trapObject.AddComponent<Arrow>();
                return true;
            default:
                Destroy(trapObject);
                trap = null;
                return false;
        }
    }

    private void ConfigureTrap(TrapBase trap, int speed, int direction, Vector2Int gridPosition)
    {
        int normalizedDirection = direction >= 0 ? 1 : -1;

        SpeedField?.SetValue(trap, Mathf.Max(1, speed));
        DirectionField?.SetValue(trap, normalizedDirection);
        GridPositionField?.SetValue(trap, gridPosition);

        trap.transform.position = GridManager.Instance.GridToWorld(gridPosition);
    }

    private void ClearSpawnedTraps()
    {
        for (int i = 0; i < spawnedTraps.Count; i++)
        {
            if (spawnedTraps[i] != null)
            {
                Destroy(spawnedTraps[i].gameObject);
            }
        }

        spawnedTraps.Clear();
    }

    private List<TrapRow> BuildLevel(int level)
    {
        switch (Mathf.Max(1, level))
        {
            case 1:
                return new List<TrapRow>
                {
                    new TrapRow(2, "boulder", 1, 1),
                    new TrapRow(5, "boulder", 1, -1)
                };
            case 2:
                return new List<TrapRow>
                {
                    new TrapRow(3, "boulder", 1, 1),
                    new TrapRow(6, "boulder", 1, -1)
                };
            case 3:
                return new List<TrapRow>
                {
                    new TrapRow(1, "boulder", 1, 1),
                    new TrapRow(3, "boulder", 1, -1),
                    new TrapRow(5, "boulder", 1, 1),
                    new TrapRow(7, "arrow", 1, -1)
                };
            case 4:
                return new List<TrapRow>
                {
                    new TrapRow(1, "boulder", 1, -1),
                    new TrapRow(2, "boulder", 1, 1),
                    new TrapRow(5, "boulder", 1, -1),
                    new TrapRow(6, "arrow", 1, 1)
                };
            case 5:
                return new List<TrapRow>
                {
                    new TrapRow(1, "boulder", 2, 1),
                    new TrapRow(3, "boulder", 1, -1),
                    new TrapRow(5, "arrow", 2, 1),
                    new TrapRow(7, "boulder", 2, 1)
                };
            case 6:
                return new List<TrapRow>
                {
                    new TrapRow(1, "arrow", 2, -1),
                    new TrapRow(2, "boulder", 2, 1),
                    new TrapRow(4, "boulder", 1, -1),
                    new TrapRow(6, "arrow", 1, 1)
                };
            case 7:
                return new List<TrapRow>
                {
                    new TrapRow(1, "boulder", 2, 1),
                    new TrapRow(2, "arrow", 1, -1),
                    new TrapRow(4, "boulder", 2, -1),
                    new TrapRow(5, "arrow", 1, 1),
                    new TrapRow(7, "boulder", 2, 1)
                };
            case 8:
                return new List<TrapRow>
                {
                    new TrapRow(1, "arrow", 1, 1),
                    new TrapRow(2, "boulder", 2, -1),
                    new TrapRow(3, "boulder", 1, 1),
                    new TrapRow(5, "arrow", 2, -1),
                    new TrapRow(6, "boulder", 2, 1)
                };
            case 9:
                return new List<TrapRow>
                {
                    new TrapRow(1, "boulder", 2, 1),
                    new TrapRow(2, "arrow", 2, -1),
                    new TrapRow(3, "boulder", 2, -1),
                    new TrapRow(4, "arrow", 1, 1),
                    new TrapRow(6, "boulder", 2, 1),
                    new TrapRow(7, "arrow", 2, -1)
                };
            default:
                return new List<TrapRow>
                {
                    new TrapRow(1, "arrow", 2, 1),
                    new TrapRow(2, "boulder", 2, -1),
                    new TrapRow(3, "arrow", 2, -1),
                    new TrapRow(4, "boulder", 2, 1),
                    new TrapRow(5, "boulder", 1, -1),
                    new TrapRow(7, "arrow", 2, 1)
                };
        }
    }
}
