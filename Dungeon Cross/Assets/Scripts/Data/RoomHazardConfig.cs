using UnityEngine;

[System.Serializable]
public class RoomHazardConfig
{
    public TrapType trapType;
    public TrapPattern pattern = TrapPattern.Horizontal;
    [Range(0, 8)] public int startColumn = 4;
    [Range(1, 10)] public int startRow = 5;
    [Range(0.2f, 2f)] public float moveInterval = 0.8f;
    public int direction = 1;
    [Range(0.2f, 1.25f)] public float dangerRadius = 0.45f;
    public bool useOrbitingBlade;
    [Range(0.25f, 1.5f)] public float orbitRadius = 0.7f;
    [Range(0.1f, 0.75f)] public float orbitBladeRadius = 0.28f;
    [Range(30f, 360f)] public float orbitAngularSpeed = 180f;
}
