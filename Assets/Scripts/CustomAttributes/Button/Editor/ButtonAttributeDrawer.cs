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

        float buttonHeight = EditorGUIUtility.singleLineHeight * 1.5f;

        // Split the position into two parts: one for the button, one for the property
        Rect buttonRect = new Rect(position.x, position.y, position.width, buttonHeight);
        Rect propertyRect = new Rect(position.x, position.y + buttonHeight + EditorGUIUtility.standardVerticalSpacing,
            position.width, position.height - buttonHeight - EditorGUIUtility.standardVerticalSpacing);
        
        // Draw the button
        if(GUI.Button(buttonRect, buttonAttribute.ButtonText))
        {
            var target = property.serializedObject.targetObject;
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

        // Draw the original property field
        EditorGUI.PropertyField(propertyRect, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Calculate the total height: button height + property height + spacing
        float buttonHeight = EditorGUIUtility.singleLineHeight * 1.5f;
        float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
        return buttonHeight + propertyHeight + EditorGUIUtility.standardVerticalSpacing;
    }
}
