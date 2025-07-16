using System;
using UnityEngine;

public class ShowIfAttribute : PropertyAttribute
{
    public string ConditionFieldName { get; }

    public ShowIfAttribute(string conditionFieldName)
    {
        ConditionFieldName = conditionFieldName;
    }
}