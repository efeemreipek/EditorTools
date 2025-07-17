using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneSelectorAttribute))]
public class SceneSelectorAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if(property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [SceneSelector] with string fields only");
            return;
        }

        string[] scenePaths = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        string[] sceneNames = scenePaths
            .Select(path => System.IO.Path.GetFileNameWithoutExtension(path))
            .ToArray();

        string currentScene = property.stringValue;

        int selectedIndex = System.Array.IndexOf(sceneNames, currentScene);
        if(selectedIndex < 0 && !string.IsNullOrEmpty(currentScene))
        {
            selectedIndex = 0;
        }

        EditorGUI.BeginProperty(position, label, property);
        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, sceneNames);
        property.stringValue = selectedIndex >= 0 ? sceneNames[selectedIndex] : "";
        EditorGUI.EndProperty();
    }
}
