using UnityEngine;

public enum TrapType
{
    Boulder,
    Arrow
}

[System.Serializable]
public class TrapRowConfig
{
    [Range(1, 10)] public int rowIndex;
    public TrapType trapType;
    [Range(0.2f, 2f)] public float speed = 0.6f;
    public int direction = 1;
    public TrapPattern pattern = TrapPattern.Horizontal;
}
