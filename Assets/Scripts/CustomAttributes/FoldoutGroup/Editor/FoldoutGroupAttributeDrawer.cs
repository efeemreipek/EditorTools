using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FoldoutGroupAttribute))]
public class FoldoutGroupAttributeDrawer : PropertyDrawer
{
    private static Dictionary<string, bool> foldoutStates = new();
    private static Dictionary<string, List<string>> groupFields = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        FoldoutGroupAttribute attr = (FoldoutGroupAttribute)attribute;
        string key = GetFoldoutKey(property, attr.GroupName);

        if(!foldoutStates.ContainsKey(key))
        {
            foldoutStates[key] = true;
        }

        CacheGroupFields(property.serializedObject.targetObject, attr.GroupName);

        if(IsFirstFieldInGroup(property, attr.GroupName))
        {
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            foldoutStates[key] = EditorGUI.Foldout(foldoutRect, foldoutStates[key], attr.GroupName, true);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        if(foldoutStates[key])
        {
            EditorGUI.indentLevel++;
            Rect propertyRect = new Rect(position.x, position.y, position.width, EditorGUI.GetPropertyHeight(property, label, true));
            EditorGUI.PropertyField(propertyRect, property, label, true);
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        FoldoutGroupAttribute attr = (FoldoutGroupAttribute)attribute;
        string key = GetFoldoutKey(property, attr.GroupName);
        bool isVisible = foldoutStates.ContainsKey(key) ? foldoutStates[key] : true;

        CacheGroupFields(property.serializedObject.targetObject, attr.GroupName);

        float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);

        if(IsFirstFieldInGroup(property, attr.GroupName))
        {
            float foldoutHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return isVisible ? foldoutHeight + propertyHeight : foldoutHeight;
        }
        if(IsLastFieldInGroup(property, attr.GroupName) && isVisible)
        {
            return propertyHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        return isVisible ? propertyHeight : 0f;
    }

    private string GetFoldoutKey(SerializedProperty property, string groupName)
    {
        Object target = property.serializedObject.targetObject;
        return $"{target.GetInstanceID()}_{groupName}";
    }

    private void CacheGroupFields(Object target, string groupName)
    {
        string cacheKey = $"{target.GetInstanceID()}_{groupName}";

        if(!groupFields.ContainsKey(cacheKey))
        {
            var fields = target.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(field => field.GetCustomAttribute<FoldoutGroupAttribute>()?.GroupName == groupName)
                .OrderBy(field => field.MetadataToken)
                .Select(field => field.Name)
                .ToList();

            groupFields[cacheKey] = fields;
        }
    }

    private bool IsFirstFieldInGroup(SerializedProperty property, string groupName)
    {
        string cacheKey = $"{property.serializedObject.targetObject.GetInstanceID()}_{groupName}";

        if(groupFields.ContainsKey(cacheKey) && groupFields[cacheKey].Count > 0)
        {
            return property.name == groupFields[cacheKey][0];
        }

        return false;
    }
    private bool IsLastFieldInGroup(SerializedProperty property, string groupName)
    {
        string cacheKey = $"{property.serializedObject.targetObject.GetInstanceID()}_{groupName}";

        if(groupFields.ContainsKey(cacheKey) && groupFields[cacheKey].Count > 0)
        {
            return property.name == groupFields[cacheKey][groupFields[cacheKey].Count - 1];
        }

        return false;
    }
}
