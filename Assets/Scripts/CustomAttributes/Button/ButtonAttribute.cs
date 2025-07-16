using System;

public class ButtonAttribute : Attribute
{
    public string MethodName;

    public ButtonAttribute()
    {
        MethodName = null;
    }
    public ButtonAttribute(string methodName)
    {
        MethodName = methodName;
    }
}
