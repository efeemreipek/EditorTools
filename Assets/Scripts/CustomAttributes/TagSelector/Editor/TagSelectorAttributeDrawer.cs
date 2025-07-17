using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
public class TagSelectorAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if(property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [TagSelector] with string fields only");
            return;
        }

        string[] tags = UnityEditorInternal.InternalEditorUtility.tags;

        string currentTag = property.stringValue;

        int selectedIndex = System.Array.IndexOf(tags, currentTag);
        if(selectedIndex < 0 && !string.IsNullOrEmpty(currentTag))
        {
            selectedIndex = 0;
        }

        EditorGUI.BeginProperty(position, label, property);
        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, tags);
        property.stringValue = selectedIndex >= 0 ? tags[selectedIndex] : "";
        EditorGUI.EndProperty();

    }
}
