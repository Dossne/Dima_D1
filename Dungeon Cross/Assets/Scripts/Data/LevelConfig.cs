using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "DungeonCross/Level Config")]
public class LevelConfig : ScriptableObject
{
    public List<RoomHazardConfig> hazards = new List<RoomHazardConfig>();
    public List<WallCellData> wallCells = new List<WallCellData>();
    [HideInInspector] public List<TrapRowConfig> traps = new List<TrapRowConfig>();
}
