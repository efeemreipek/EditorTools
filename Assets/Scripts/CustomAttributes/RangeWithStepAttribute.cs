using UnityEngine;

public class RangeWithStepAttribute : PropertyAttribute
{
    public float Min;
    public float Max;
    public float Step;

    public RangeWithStepAttribute(float min, float max, float step)
    {
        Min = min;
        Max = max;
        Step = step;
    }
}
