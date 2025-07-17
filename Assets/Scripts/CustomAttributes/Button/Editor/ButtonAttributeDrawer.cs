using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomPropertyDrawer(typeof(ButtonAttribute))]
public class ButtonAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ButtonAttribute buttonAttribute = (ButtonAttribute)attribute;
        string methodName = buttonAttribute.MethodName ?? buttonAttribute.ButtonText;

        if(GUI.Button(position, buttonAttribute.ButtonText))
        {
            var target = property.serializedObject.targetObject;

            // Search for the method with all binding flags to include private methods
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static);

            if(method != null)
            {
                try
                {
                    method.Invoke(target, null);
                }
                catch(System.Exception e)
                {
                    Debug.LogError($"Error calling method '{methodName}': {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Method '{methodName}' not found on {target.GetType().Name}");
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 25f;
    }
}
