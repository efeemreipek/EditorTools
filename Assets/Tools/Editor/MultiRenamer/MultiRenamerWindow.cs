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
    private enum NumberingStyle
    {
        [InspectorName("{NAME}###")]
        Adjacent,
        [InspectorName("{NAME}_###")]
        Underscore,
        [InspectorName("{NAME} (###)")]
        Parenthesis
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
    private bool addNumbering;
    private string numbering = string.Empty;
    private NumberingStyle numberingStyle;
    private int startNumber = 1;
    private int padding = 2;

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
        DrawNumbering();
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

        if(Selection.objects.Length > 0)
        {
            string previewName = GetNewName(Selection.objects[0], 0);
            EditorGUILayout.TextField(previewName, GUILayout.ExpandWidth(true), GUILayout.Height(30f));
        }
        else
        {
            EditorGUILayout.HelpBox("Select objects to preview renaming", MessageType.Info);
        }

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
        else
        {
            prefix = string.Empty;
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
        else
        {
            suffix = string.Empty;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawNumbering()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        addNumbering = EditorGUILayout.ToggleLeft("Add Numbering", addNumbering);
        if(addNumbering)
        {
            numberingStyle = (NumberingStyle)EditorGUILayout.EnumPopup("Numbering Style", numberingStyle);
            startNumber = EditorGUILayout.IntField("Start Number", startNumber);
            padding = EditorGUILayout.IntSlider("Number Padding", padding, 1, 5);
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawApplyButton()
    {
        GUI.enabled = Selection.objects.Length > 0;
        EditorGUILayout.BeginVertical("Box");
        if(GUILayout.Button("APPLY", GUILayout.Height(Layout.BUTTON_HEIGHT)) && Event.current.button == 0)
        {
            if(Selection.objects.Length > 0)
            {
                RenameObjects(Selection.objects);
            }
        }
        EditorGUILayout.EndVertical();
        GUI.enabled = true;
    }
    private void RenameObjects(Object[] objects)
    {
        Undo.RecordObjects(objects, "Multi Rename");

        for(int i = 0;  i < objects.Length; i++)
        {
            Object obj = objects[i];
            string newObjectName = GetNewName(obj, i);

            if(obj is GameObject)
            {
                obj.name = newObjectName;
            }
            else if(obj is Object)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                AssetDatabase.RenameAsset(path, newName);
            }
        }

        AssetDatabase.Refresh();
    }
    private string GetNewName(Object obj, int index)
    {
        string originalName = obj.name;
        string resultName = originalName;

        if(changeOriginalName)
        {
            resultName = baseName;
        }
        if(addPrefix)
        {
            resultName = prefix + resultName;
        }
        if(addSuffix)
        {
            resultName = resultName + suffix;
        }
        if(addNumbering)
        {
            string paddedNumber = (startNumber + index).ToString().PadLeft(padding, '0');

            switch(numberingStyle)
            {
                case NumberingStyle.Adjacent:
                    resultName = resultName + paddedNumber;
                    break;
                case NumberingStyle.Underscore:
                    resultName = resultName + "_" + paddedNumber;
                    break;
                case NumberingStyle.Parenthesis:
                    resultName = resultName + " (" + paddedNumber + ")";
                    break;
            }
        }

        return resultName;
    }
}
