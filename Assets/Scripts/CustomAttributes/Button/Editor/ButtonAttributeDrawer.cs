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

        bool isVisible = IsPropertyVisible(property);

        if(!isVisible) return;

        float buttonHeight = EditorGUIUtility.singleLineHeight * 1.5f;

        Rect buttonRect = new Rect(position.x, position.y, position.width, buttonHeight);
        Rect propertyRect = new Rect(position.x, position.y + buttonHeight + EditorGUIUtility.standardVerticalSpacing, position.width, position.height - buttonHeight - EditorGUIUtility.standardVerticalSpacing);

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

        EditorGUI.PropertyField(propertyRect, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        bool isVisible = IsPropertyVisible(property);

        if(!isVisible) return -EditorGUIUtility.standardVerticalSpacing;

        float buttonHeight = EditorGUIUtility.singleLineHeight * 1.5f;
        float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
        return buttonHeight + propertyHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    private bool IsPropertyVisible(SerializedProperty property)
    {
        var fieldInfo = property.serializedObject.targetObject.GetType()
            .GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if(fieldInfo != null)
        {
            var foldoutAttribute = fieldInfo.GetCustomAttribute<FoldoutGroupAttribute>();
            if(foldoutAttribute != null)
            {
                string key = $"{property.serializedObject.targetObject.GetInstanceID()}_{foldoutAttribute.GroupName}";
                return FoldoutGroupAttributeDrawer.GetFoldoutState(key);
            }
        }

        return true;
    }
}
