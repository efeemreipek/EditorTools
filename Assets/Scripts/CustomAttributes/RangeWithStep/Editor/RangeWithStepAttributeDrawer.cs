using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RangeWithStepAttribute))]
public class RangeWithStepAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        RangeWithStepAttribute range = (RangeWithStepAttribute)attribute;

        if(property.propertyType == SerializedPropertyType.Float)
        {
            EditorGUI.Slider(position, property, range.Min, range.Max, label);

            float value = property.floatValue;
            value = Mathf.Round(value / range.Step) * range.Step;
            property.floatValue = Mathf.Clamp(value, range.Min, range.Max);
        }
        else if(property.propertyType == SerializedPropertyType.Integer)
        {
            EditorGUI.IntSlider(position, property, (int)range.Min, (int)range.Max, label);

            int value = property.intValue;
            value = Mathf.RoundToInt(value / range.Step) * (int)range.Step;
            property.intValue = Mathf.Clamp(value, (int)range.Min, (int)range.Max);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use RangeWithStep with float or int.");
        }
    }
}
