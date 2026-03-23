using System;

[Serializable]
public enum TrapPattern
{
    Horizontal,
    Vertical,
    Square
}

[Serializable]
public class TrapRow
{
    public int rowIndex;
    public string trapType;
    public int speed;
    public int direction;
    public TrapPattern pattern;

    public TrapRow(int rowIndex, string trapType, int speed, int direction, TrapPattern pattern)
    {
        this.rowIndex = rowIndex;
        this.trapType = trapType;
        this.speed = speed;
        this.direction = direction;
        this.pattern = pattern;
    }
}
