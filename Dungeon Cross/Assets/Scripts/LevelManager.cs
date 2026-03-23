using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public LevelConfig[] levelConfigs;

    private readonly List<TrapBase> spawnedTraps = new List<TrapBase>();
    private readonly HashSet<Vector2Int> activeWallCells = new HashSet<Vector2Int>();

    public IReadOnlyCollection<Vector2Int> ActiveWallCells => activeWallCells;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        levelConfigs = LoadLevelConfigs();
    }

    public void SpawnLevel(int level)
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("LevelManager requires a GridManager in the scene.");
            return;
        }

        ClearSpawnedTraps();
        CacheWallCells(level);
        List<RoomHazardConfig> hazards = BuildConfiguredLevel(level) ?? GetBuiltInLevel(level);

        for (int i = 0; i < hazards.Count; i++)
        {
            SpawnHazard(hazards[i]);
        }

        DungeonVisual dungeonVisual = FindObjectOfType<DungeonVisual>();
        if (dungeonVisual != null)
        {
            dungeonVisual.Build();
        }

        GridManager.Instance.FitCameraToGrid();
    }

    public bool IsWallCell(Vector2Int cell)
    {
        return activeWallCells.Contains(cell);
    }

    public bool IsPositionBlocked(Vector2 worldPosition, float radius)
    {
        if (GridManager.Instance == null || activeWallCells.Count == 0)
        {
            return false;
        }

        foreach (Vector2Int wallCell in activeWallCells)
        {
            if (GridManager.Instance.CircleIntersectsCell(worldPosition, radius, wallCell))
            {
                return true;
            }
        }

        return false;
    }

    public static LevelConfig[] LoadLevelConfigs()
    {
        LevelConfig[] configs = Resources.LoadAll<LevelConfig>("Levels");
        System.Array.Sort(configs, CompareLevelConfigs);
        return configs;
    }

    public static List<RoomHazardConfig> GetBuiltInLevel(int level)
    {
        switch (Mathf.Max(1, level))
        {
            case 1:
                return new List<RoomHazardConfig>
                {
                    CreateHazard(TrapType.Boulder, TrapPattern.Horizontal, 2, 3, 1.0f, 1, 0.42f, false),
                    CreateHazard(TrapType.Boulder, TrapPattern.Horizontal, 6, 8, 0.9f, -1, 0.42f, false)
                };
            case 2:
                return new List<RoomHazardConfig>
                {
                    CreateHazard(TrapType.Boulder, TrapPattern.Horizontal, 1, 2, 0.85f, 1, 0.45f, false),
                    CreateHazard(TrapType.Boulder, TrapPattern.Vertical, 6, 6, 1.0f, 1, 0.45f, false),
                    CreateHazard(TrapType.Arrow, TrapPattern.Horizontal, 7, 9, 0.75f, -1, 0.4f, false)
                };
            case 3:
                return new List<RoomHazardConfig>
                {
                    CreateHazard(TrapType.Boulder, TrapPattern.Horizontal, 1, 3, 0.7f, 1, 0.45f, true, 0.75f, 0.22f, 160f),
                    CreateHazard(TrapType.Boulder, TrapPattern.Vertical, 2, 7, 0.9f, 1, 0.48f, false),
                    CreateHazard(TrapType.Arrow, TrapPattern.Horizontal, 7, 5, 0.6f, -1, 0.38f, false),
                    CreateHazard(TrapType.Boulder, TrapPattern.Square, 4, 10, 0.55f, 1, 0.42f, true, 0.8f, 0.24f, 220f)
                };
            case 4:
                return new List<RoomHazardConfig>
                {
                    CreateHazard(TrapType.Boulder, TrapPattern.Horizontal, 1, 2, 0.7f, 1, 0.48f, true, 0.75f, 0.24f, 180f),
                    CreateHazard(TrapType.Boulder, TrapPattern.Vertical, 6, 4, 0.75f, 1, 0.45f, false),
                    CreateHazard(TrapType.Arrow, TrapPattern.Horizontal, 7, 7, 0.6f, -1, 0.42f, false),
                    CreateHazard(TrapType.Boulder, TrapPattern.Vertical, 2, 9, 0.7f, -1, 0.48f, true, 0.7f, 0.22f, 210f)
                };
            default:
                return new List<RoomHazardConfig>
                {
                    CreateHazard(TrapType.Boulder, TrapPattern.Horizontal, 1, 2, 0.65f, 1, 0.5f, true, 0.8f, 0.24f, 220f),
                    CreateHazard(TrapType.Arrow, TrapPattern.Horizontal, 7, 4, 0.55f, -1, 0.4f, false),
                    CreateHazard(TrapType.Boulder, TrapPattern.Vertical, 3, 6, 0.7f, 1, 0.46f, true, 0.75f, 0.2f, 200f),
                    CreateHazard(TrapType.Boulder, TrapPattern.Square, 5, 9, 0.5f, 1, 0.45f, true, 0.8f, 0.26f, 240f),
                    CreateHazard(TrapType.Arrow, TrapPattern.Horizontal, 2, 10, 0.45f, 1, 0.38f, false)
                };
        }
    }

    public static int BuiltInLevelCount => 5;

    private void SpawnHazard(RoomHazardConfig config)
    {
        if (!TryCreateTrap(config.trapType, out TrapBase trap))
        {
            return;
        }

        trap.Configure(config);
        trap.name = $"{trap.GetType().Name}_{config.pattern}_{config.startColumn}_{config.startRow}";
        spawnedTraps.Add(trap);
    }

    private bool TryCreateTrap(TrapType trapType, out TrapBase trap)
    {
        GameObject trapObject = new GameObject(trapType.ToString());

        switch (trapType)
        {
            case TrapType.Arrow:
                trap = trapObject.AddComponent<Arrow>();
                return true;
            default:
                trap = trapObject.AddComponent<Boulder>();
                return true;
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

    private void CacheWallCells(int level)
    {
        activeWallCells.Clear();

        if (levelConfigs == null || levelConfigs.Length == 0)
        {
            return;
        }

        int index = Mathf.Clamp(level - 1, 0, levelConfigs.Length - 1);
        LevelConfig config = levelConfigs[index];
        if (config == null || config.wallCells == null)
        {
            return;
        }

        for (int i = 0; i < config.wallCells.Count; i++)
        {
            WallCellData wallCell = config.wallCells[i];
            if (wallCell == null)
            {
                continue;
            }

            Vector2Int clampedCell = GridManager.Instance.ClampToBounds(wallCell.ToVector2Int());
            if (IsReservedCell(clampedCell))
            {
                continue;
            }

            activeWallCells.Add(clampedCell);
        }
    }

    private List<RoomHazardConfig> BuildConfiguredLevel(int level)
    {
        if (levelConfigs == null || levelConfigs.Length == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(level - 1, 0, levelConfigs.Length - 1);
        LevelConfig config = levelConfigs[index];
        if (config == null)
        {
            return null;
        }

        if (config.hazards != null && config.hazards.Count > 0)
        {
            return CloneHazards(config.hazards);
        }

        if (config.traps == null || config.traps.Count == 0)
        {
            return null;
        }

        List<RoomHazardConfig> convertedHazards = new List<RoomHazardConfig>();
        for (int i = 0; i < config.traps.Count; i++)
        {
            TrapRowConfig trap = config.traps[i];
            if (trap == null)
            {
                continue;
            }

            convertedHazards.Add(new RoomHazardConfig
            {
                trapType = trap.trapType,
                pattern = trap.pattern,
                startColumn = GetLegacyColumn(trap.pattern, trap.direction),
                startRow = trap.rowIndex,
                moveInterval = 0.6f / Mathf.Max(1, trap.speed),
                direction = trap.direction,
                dangerRadius = trap.trapType == TrapType.Arrow ? 0.38f : 0.45f,
                useOrbitingBlade = false,
                orbitRadius = 0.7f,
                orbitBladeRadius = 0.28f,
                orbitAngularSpeed = 180f
            });
        }

        return convertedHazards;
    }

    private int GetLegacyColumn(TrapPattern pattern, int direction)
    {
        switch (pattern)
        {
            case TrapPattern.Vertical:
                return direction >= 0 ? 2 : GridManager.Instance.Columns - 3;
            case TrapPattern.Square:
                return direction >= 0 ? 2 : GridManager.Instance.Columns - 4;
            default:
                return direction >= 0 ? 1 : GridManager.Instance.Columns - 2;
        }
    }

    private static List<RoomHazardConfig> CloneHazards(List<RoomHazardConfig> source)
    {
        List<RoomHazardConfig> result = new List<RoomHazardConfig>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            RoomHazardConfig hazard = source[i];
            if (hazard == null)
            {
                continue;
            }

            result.Add(CreateHazard(
                hazard.trapType,
                hazard.pattern,
                hazard.startColumn,
                hazard.startRow,
                hazard.moveInterval,
                hazard.direction,
                hazard.dangerRadius,
                hazard.useOrbitingBlade,
                hazard.orbitRadius,
                hazard.orbitBladeRadius,
                hazard.orbitAngularSpeed));
        }

        return result;
    }

    private bool IsReservedCell(Vector2Int cell)
    {
        if (GridManager.Instance == null)
        {
            return false;
        }

        Vector2Int entryCell = new Vector2Int(GridManager.Instance.Columns / 2, 0);
        Vector2Int exitCell = new Vector2Int(GridManager.Instance.Columns / 2, GridManager.Instance.Rows - 1);
        return cell == entryCell || cell == exitCell;
    }

    private static RoomHazardConfig CreateHazard(TrapType type, TrapPattern pattern, int startColumn, int startRow, float moveInterval, int direction, float dangerRadius, bool useOrbitingBlade, float orbitRadius = 0.7f, float orbitBladeRadius = 0.25f, float orbitAngularSpeed = 180f)
    {
        return new RoomHazardConfig
        {
            trapType = type,
            pattern = pattern,
            startColumn = startColumn,
            startRow = startRow,
            moveInterval = moveInterval,
            direction = direction,
            dangerRadius = dangerRadius,
            useOrbitingBlade = useOrbitingBlade,
            orbitRadius = orbitRadius,
            orbitBladeRadius = orbitBladeRadius,
            orbitAngularSpeed = orbitAngularSpeed
        };
    }

    private static int CompareLevelConfigs(LevelConfig left, LevelConfig right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return string.CompareOrdinal(left.name, right.name);
    }
}
