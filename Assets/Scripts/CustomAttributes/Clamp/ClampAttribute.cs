using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
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
