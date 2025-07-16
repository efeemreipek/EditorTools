using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
public class MinMaxSliderAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxSliderAttribute minMax = (MinMaxSliderAttribute)attribute;

        if(property.propertyType == SerializedPropertyType.Vector2 || property.propertyType == SerializedPropertyType.Vector2Int)
        {
            float minValue, maxValue;

            if(property.propertyType == SerializedPropertyType.Vector2)
            {
                Vector2 value = property.vector2Value;
                minValue = value.x;
                maxValue = value.y;
            }
            else
            {
                Vector2Int value = property.vector2IntValue;
                minValue = value.x;
                maxValue = value.y;
            }

            float labelWidth = EditorGUIUtility.labelWidth;
            float sliderWidth = position.width - labelWidth - 100f;
            float inputWidth = 45f;

            Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect sliderRect = new Rect(position.x + labelWidth, position.y, sliderWidth, EditorGUIUtility.singleLineHeight);
            Rect minRect = new Rect(position.x + labelWidth + sliderWidth + 5f, position.y, inputWidth, EditorGUIUtility.singleLineHeight);
            Rect maxRect = new Rect(position.x + labelWidth + sliderWidth + inputWidth + 10f, position.y, inputWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, label);

            EditorGUI.MinMaxSlider(sliderRect, GUIContent.none, ref minValue, ref maxValue, minMax.Min, minMax.Max);

            if(property.propertyType == SerializedPropertyType.Vector2)
            {
                minValue = EditorGUI.FloatField(minRect, minValue);
                maxValue = EditorGUI.FloatField(maxRect, maxValue);
            }
            else
            {
                minValue = EditorGUI.IntField(minRect, Mathf.RoundToInt(minValue));
                maxValue = EditorGUI.IntField(maxRect, Mathf.RoundToInt(maxValue));
            }

            minValue = Mathf.Clamp(minValue, minMax.Min, maxValue);
            maxValue = Mathf.Clamp(maxValue, minValue, minMax.Max);

            if(property.propertyType == SerializedPropertyType.Vector2)
            {
                property.vector2Value = new Vector2(minValue, maxValue);
            }
            else
            {
                property.vector2IntValue = new Vector2Int(Mathf.RoundToInt(minValue), Mathf.RoundToInt(maxValue));
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use MinMaxSlider with Vector2 or Vector2Int.");
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
