using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class ConditionalShowAttribute : PropertyAttribute
{
    public string ConditionalFieldName;
    public object CompareValue;
    public bool Inverse;

    public ConditionalShowAttribute(string conditionalFieldName, object compareValue = null, bool inverse = false)
    {
        ConditionalFieldName = conditionalFieldName;
        CompareValue = compareValue;
        Inverse = inverse;
    }
}
