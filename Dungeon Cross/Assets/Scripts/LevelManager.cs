using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private List<TrapRow> trapRows = new List<TrapRow>();
    public LevelConfig[] levelConfigs;

    private readonly List<TrapBase> spawnedTraps = new List<TrapBase>();

    public IReadOnlyList<TrapRow> TrapRows => trapRows;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        levelConfigs = Resources.LoadAll<LevelConfig>("Levels");
    }

    public void SpawnLevel(int level)
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("LevelManager requires a GridManager in the scene.");
            return;
        }

        ClearSpawnedTraps();
        trapRows = BuildConfiguredLevel(level) ?? BuildLevel(level);

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

        Vector2Int gridPosition = new Vector2Int(GetSpawnColumn(rowConfig), Mathf.Clamp(rowConfig.rowIndex, 0, GridManager.Instance.Rows - 1));
        trap.Configure(rowConfig, gridPosition);
        trap.transform.position = GridManager.Instance.GridToWorld(gridPosition);
        trap.name = $"{trap.GetType().Name}_Row_{gridPosition.y}";
        spawnedTraps.Add(trap);
    }

    private int GetSpawnColumn(TrapRow rowConfig)
    {
        switch (rowConfig.pattern)
        {
            case TrapPattern.Vertical:
                return rowConfig.direction >= 0 ? 1 : GridManager.Instance.Columns - 2;
            case TrapPattern.Square:
                return rowConfig.direction >= 0 ? 1 : GridManager.Instance.Columns - 3;
            default:
                return rowConfig.direction >= 0 ? 0 : GridManager.Instance.Columns - 1;
        }
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

    private List<TrapRow> BuildConfiguredLevel(int level)
    {
        if (levelConfigs == null || levelConfigs.Length == 0)
        {
            return null;
        }

        int index = level - 1;
        if (index < 0 || index >= levelConfigs.Length || levelConfigs[index] == null || levelConfigs[index].traps == null)
        {
            return null;
        }

        List<TrapRow> configuredRows = new List<TrapRow>();

        for (int i = 0; i < levelConfigs[index].traps.Count; i++)
        {
            TrapRowConfig config = levelConfigs[index].traps[i];
            if (config == null)
            {
                continue;
            }

            string trapType = config.trapType == TrapType.Arrow ? "arrow" : "boulder";
            int speed = Mathf.Max(1, Mathf.RoundToInt(0.6f / Mathf.Max(0.2f, config.speed)));
            configuredRows.Add(new TrapRow(config.rowIndex, trapType, speed, config.direction, config.pattern));
        }

        return configuredRows;
    }

    private List<TrapRow> BuildLevel(int level)
    {
        switch (Mathf.Max(1, level))
        {
            case 1:
                return new List<TrapRow>
                {
                    new TrapRow(2, "boulder", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(8, "boulder", 1, -1, TrapPattern.Horizontal)
                };
            case 2:
                return new List<TrapRow>
                {
                    new TrapRow(3, "boulder", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(9, "boulder", 1, -1, TrapPattern.Horizontal)
                };
            case 3:
                return new List<TrapRow>
                {
                    new TrapRow(1, "boulder", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(4, "boulder", 1, -1, TrapPattern.Horizontal),
                    new TrapRow(7, "boulder", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(10, "arrow", 1, -1, TrapPattern.Horizontal)
                };
            case 4:
                return new List<TrapRow>
                {
                    new TrapRow(2, "boulder", 1, 1, TrapPattern.Vertical),
                    new TrapRow(4, "boulder", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(7, "boulder", 1, -1, TrapPattern.Vertical),
                    new TrapRow(9, "arrow", 1, 1, TrapPattern.Horizontal)
                };
            case 5:
                return new List<TrapRow>
                {
                    new TrapRow(1, "boulder", 2, 1, TrapPattern.Horizontal),
                    new TrapRow(4, "boulder", 1, 1, TrapPattern.Vertical),
                    new TrapRow(7, "arrow", 2, 1, TrapPattern.Horizontal),
                    new TrapRow(10, "boulder", 2, -1, TrapPattern.Vertical)
                };
            case 6:
                return new List<TrapRow>
                {
                    new TrapRow(2, "arrow", 2, -1, TrapPattern.Horizontal),
                    new TrapRow(5, "boulder", 2, 1, TrapPattern.Vertical),
                    new TrapRow(8, "boulder", 1, -1, TrapPattern.Horizontal),
                    new TrapRow(10, "arrow", 1, 1, TrapPattern.Horizontal)
                };
            case 7:
                return new List<TrapRow>
                {
                    new TrapRow(1, "boulder", 2, 1, TrapPattern.Horizontal),
                    new TrapRow(3, "arrow", 1, -1, TrapPattern.Horizontal),
                    new TrapRow(5, "boulder", 2, 1, TrapPattern.Vertical),
                    new TrapRow(8, "arrow", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(10, "boulder", 1, 1, TrapPattern.Square)
                };
            case 8:
                return new List<TrapRow>
                {
                    new TrapRow(2, "arrow", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(4, "boulder", 2, -1, TrapPattern.Vertical),
                    new TrapRow(6, "boulder", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(8, "arrow", 2, -1, TrapPattern.Horizontal),
                    new TrapRow(10, "boulder", 1, 1, TrapPattern.Square)
                };
            case 9:
                return new List<TrapRow>
                {
                    new TrapRow(2, "boulder", 2, 1, TrapPattern.Square),
                    new TrapRow(3, "arrow", 2, -1, TrapPattern.Horizontal),
                    new TrapRow(5, "boulder", 2, -1, TrapPattern.Vertical),
                    new TrapRow(7, "arrow", 1, 1, TrapPattern.Horizontal),
                    new TrapRow(9, "boulder", 2, 1, TrapPattern.Horizontal),
                    new TrapRow(10, "arrow", 2, -1, TrapPattern.Horizontal)
                };
            default:
                return new List<TrapRow>
                {
                    new TrapRow(1, "arrow", 2, 1, TrapPattern.Horizontal),
                    new TrapRow(3, "boulder", 2, -1, TrapPattern.Square),
                    new TrapRow(5, "arrow", 2, -1, TrapPattern.Horizontal),
                    new TrapRow(7, "boulder", 2, 1, TrapPattern.Vertical),
                    new TrapRow(9, "boulder", 1, -1, TrapPattern.Horizontal),
                    new TrapRow(10, "arrow", 2, 1, TrapPattern.Horizontal)
                };
        }
    }
}
