using System;
using UnityEngine;

public class ShowIfAttribute : PropertyAttribute
{
    public string ConditionFieldName;

    public ShowIfAttribute(string conditionFieldName)
    {
        ConditionFieldName = conditionFieldName;
    }
}