using UnityEditor;
using UnityEngine;

public class MultiRenamerWindow : EditorWindow
{
    private static class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float SPACE = 5f;
        public const float BUTTON_HEIGHT = 40f;
    }

    [MenuItem("Tools/Multi Renamer")]
    public static void ShowWindow()
    {
        var win = GetWindow<MultiRenamerWindow>("Multi Renamer");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private string newName;
    private bool changeOriginalName;
    private string baseName = string.Empty;
    private bool addPrefix;
    private string prefix = string.Empty;
    private bool addSuffix;
    private string suffix = string.Empty;

    private void OnGUI()
    {
        DrawPreview();
        EditorGUILayout.BeginVertical("Box");
        DrawBaseName();
        GUILayout.Space(Layout.SPACE);
        DrawPrefix();
        GUILayout.Space(Layout.SPACE);
        DrawSuffix();
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();
        DrawApplyButton();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }
    private void DrawPreview()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Label("PREVIEW", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.TextField(CombineName(Selection.objects), GUILayout.ExpandWidth(true), GUILayout.Height(30f));
        EditorGUILayout.EndVertical();
    }
    private void DrawBaseName()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        changeOriginalName = EditorGUILayout.ToggleLeft("Change Original Name", changeOriginalName);
        if(changeOriginalName)
        {
            baseName = EditorGUILayout.TextField(new GUIContent("New Base Name"), baseName);
        }
        else
        {
            baseName = string.Empty;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawPrefix()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        addPrefix = EditorGUILayout.ToggleLeft("Add Prefix", addPrefix);
        if(addPrefix)
        {
            prefix = EditorGUILayout.TextField(new GUIContent("Prefix"), prefix);
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawSuffix()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        addSuffix = EditorGUILayout.ToggleLeft("Add Suffix", addSuffix);
        if(addSuffix)
        {
            suffix = EditorGUILayout.TextField(new GUIContent("Suffix"), suffix);
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawApplyButton()
    {
        EditorGUILayout.BeginVertical("Box");
        if(GUILayout.Button("APPLY", GUILayout.Height(Layout.BUTTON_HEIGHT)) && Event.current.button == 0)
        {
            if(Selection.objects.Length > 0)
            {
                RenameObjects(Selection.objects);
            }
        }
        EditorGUILayout.EndVertical();
    }
    private void RenameObjects(Object[] objects)
    {
        foreach(var obj in Selection.objects)
        {
            RenameObject(obj);
        }
    }
    private void RenameObject(Object obj)
    {
        if(obj is GameObject)
        {
            obj.name = newName;
        }
        else if(obj is Object)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            AssetDatabase.RenameAsset(path, newName);
        }
    }
    private string CombineName(Object[] objects)
    {
        newName = string.Empty;
        newName += addPrefix ? prefix : string.Empty;
        newName += objects.Length > 0 ? (changeOriginalName ? baseName : objects[0].name) : string.Empty;
        newName += addSuffix ? suffix : string.Empty;

        return newName;
    }
}
