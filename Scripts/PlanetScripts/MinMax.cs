using UnityEngine;

public class MinMax 
{
    public float Min { get; set; }
    public float Max { get; set; }

    public MinMax()
    {
        Min = float.MaxValue;
        Max = float.MinValue;
    }

    public void AddValue(float v)
    {
        if (v > Max)
        {
            Max = v;
        }
        if (v < Min)
        {
            Min = v;
        }
    }
}