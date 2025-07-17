using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class FoldoutGroupAttribute : PropertyAttribute
{
    public string GroupName;

    public FoldoutGroupAttribute(string groupName)
    {
        GroupName = groupName;
    }
}
