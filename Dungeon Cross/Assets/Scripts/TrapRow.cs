using System;

[Serializable]
public class TrapRow
{
    public int rowIndex;
    public string trapType;
    public int speed;
    public int direction;

    public TrapRow(int rowIndex, string trapType, int speed, int direction)
    {
        this.rowIndex = rowIndex;
        this.trapType = trapType;
        this.speed = speed;
        this.direction = direction;
    }
}
