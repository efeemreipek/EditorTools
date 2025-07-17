using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RequiredAttribute))]
public class RequiredAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        RequiredAttribute required = (RequiredAttribute)attribute;

        EditorGUI.PropertyField(position, property, label, true);

        if(IsFieldMissing(property))
        {
            string message = string.IsNullOrEmpty(required.Message) ? $"{property.displayName} is required!" : required.Message;

            Rect helpBoxRect = new Rect(position.x, position.y + EditorGUI.GetPropertyHeight(property, label), position.width, GetHelpBoxHeight());

            EditorGUI.HelpBox(helpBoxRect, message, MessageType.Error);
        }
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        RequiredAttribute required = (RequiredAttribute)attribute;
        float baseHeight = EditorGUI.GetPropertyHeight(property, label);

        if(IsFieldMissing(property))
        {
            return baseHeight + GetHelpBoxHeight() + EditorGUIUtility.standardVerticalSpacing;
        }

        return baseHeight;
    }

    private bool IsFieldMissing(SerializedProperty property)
    {
        switch(property.propertyType)
        {
            case SerializedPropertyType.ObjectReference: return property.objectReferenceValue == null;
            case SerializedPropertyType.String: return string.IsNullOrEmpty(property.stringValue);
            case SerializedPropertyType.Integer: return property.intValue == 0;
            case SerializedPropertyType.Float: return Mathf.Approximately(property.floatValue, 0f);
            case SerializedPropertyType.Boolean: return !property.boolValue;
            case SerializedPropertyType.Vector2: return property.vector2Value == Vector2.zero;
            case SerializedPropertyType.Vector3: return property.vector3Value == Vector3.zero;
            case SerializedPropertyType.Vector4: return property.vector4Value == Vector4.zero;
            case SerializedPropertyType.Color: return property.colorValue == Color.clear;
            case SerializedPropertyType.LayerMask: return property.intValue == 0;
            case SerializedPropertyType.Enum: return property.enumValueIndex == 0;
            case SerializedPropertyType.AnimationCurve: return property.animationCurveValue == null || property.animationCurveValue.keys.Length == 0;
            case SerializedPropertyType.Gradient: return property.gradientValue == null;
            default: return false;
        }
    }
    private float GetHelpBoxHeight()
    {
        return EditorGUIUtility.singleLineHeight * 2f;
    }
}
