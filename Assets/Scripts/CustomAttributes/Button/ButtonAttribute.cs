using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class ButtonAttribute : PropertyAttribute
{
    public string ButtonText;
    public string MethodName;

    public ButtonAttribute(string buttonText, string methodName)
    {
        ButtonText = buttonText;
        MethodName = methodName;
    }

    public ButtonAttribute(string methodName)
    {
        ButtonText = methodName;
        MethodName = methodName;
    }
}
