using UnityEngine;


public class RequiredAttribute : PropertyAttribute
{
    public string Message;

    public RequiredAttribute(string message)
    {
        Message = message;
    }
}
