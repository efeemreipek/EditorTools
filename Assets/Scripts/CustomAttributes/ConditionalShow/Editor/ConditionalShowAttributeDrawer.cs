using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ConditionalShowAttribute))]
public class ConditionalShowAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalShowAttribute attr = (ConditionalShowAttribute)attribute;

        if(ShouldShow(property, attr))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalShowAttribute attr = (ConditionalShowAttribute)attribute;

        if(ShouldShow(property, attr))
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        return -EditorGUIUtility.standardVerticalSpacing;
    }

    private bool ShouldShow(SerializedProperty property, ConditionalShowAttribute attr)
    {
        bool show = false;
        SerializedProperty conditionalProperty = property.serializedObject.FindProperty(attr.ConditionalFieldName);

        if(conditionalProperty != null)
        {
            if(attr.CompareValue == null)
            {
                show = conditionalProperty.boolValue;
            }
            else
            {
                show = ComparePropertyValue(conditionalProperty, attr.CompareValue);
            }
        }

        return attr.Inverse ? !show : show;
    }

    private bool ComparePropertyValue(SerializedProperty property, object compareValue)
    {
        switch(property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return property.boolValue.Equals(compareValue);
            case SerializedPropertyType.Integer:
                return property.intValue.Equals(compareValue);
            case SerializedPropertyType.Float:
                return Mathf.Approximately(property.floatValue, (float)compareValue);
            case SerializedPropertyType.String:
                return property.stringValue.Equals(compareValue);
            case SerializedPropertyType.Enum:
                return property.enumValueIndex.Equals((int)compareValue);
            default:
                Debug.LogWarning($"[ConditionalShow] Unsupported property type {property.propertyType}");
                return false;
        }
    }
}
