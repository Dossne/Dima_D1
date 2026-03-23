using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "DungeonCross/Level Config")]
public class LevelConfig : ScriptableObject
{
    public List<TrapRowConfig> traps;
}
