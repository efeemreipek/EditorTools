using UnityEngine;

public class ClampAttribute : PropertyAttribute
{
    public float Min;
    public float Max;

    public ClampAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
