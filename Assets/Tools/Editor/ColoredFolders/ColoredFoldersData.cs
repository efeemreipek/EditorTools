using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class ColoredFolderEntry
{
    public string Path;
    public string GUID;
    public Color Color;
}

public class ColoredFoldersData : ScriptableObject
{
    public List<ColoredFolderEntry> Data = new List<ColoredFolderEntry>();
}

[CustomEditor(typeof(ColoredFoldersData))]
public class ColoredFoldersDataInspector : Editor
{
    public override void OnInspectorGUI()
    {
        ColoredFoldersData data = (ColoredFoldersData)target;

        GUI.enabled = false;
        base.OnInspectorGUI();
        
        GUI.enabled = data.Data.Count > 0;
        if(GUILayout.Button("CLEAR") && Event.current.button == 0)
        {
            if(EditorUtility.DisplayDialog(
                "Clear Folder Colors",
                "Are you sure you want to clear all folder color data? This action cannot be undone.",
                "Clear",
                "Cancel"))
            {
                data.Data.Clear();
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
                ColoredFoldersDrawer.Refresh();
            }
        }

        GUI.enabled = true;
    }
}