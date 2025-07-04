using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ColoredFoldersContextMenu
{
    [MenuItem("Assets/Color Folder", true)]
    private static bool OpenColoredFoldersWindowValidate()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return AssetDatabase.IsValidFolder(path);
    }

    [MenuItem("Assets/Color Folder")]
    private static void OpenColoredFoldersWindow()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string guid = AssetDatabase.AssetPathToGUID(path);
        ColoredFoldersWindow.Open(guid);
    }

    [MenuItem("Assets/Remove Folder Color", true)]
    private static bool RemoveFolderColorValidate()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if(!AssetDatabase.IsValidFolder(path)) return false;

        string guid = AssetDatabase.AssetPathToGUID(path);
        string dataPath = "Assets/ColoredFolders/ColoredFoldersData.asset";
        var foldersData = AssetDatabase.LoadAssetAtPath<ColoredFoldersData>(dataPath);

        return foldersData != null && foldersData.Data.Exists(entry => entry.GUID == guid);
    }

    [MenuItem("Assets/Remove Folder Color")]
    private static void RemoveFolderColor()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string guid = AssetDatabase.AssetPathToGUID(path);

        string dataPath = "Assets/ColoredFolders/ColoredFoldersData.asset";
        var foldersData = AssetDatabase.LoadAssetAtPath<ColoredFoldersData>(dataPath);

        if(foldersData != null)
        {
            foldersData.Data.RemoveAll(entry => entry.GUID == guid);
            EditorUtility.SetDirty(foldersData);
            AssetDatabase.SaveAssets();
            ColoredFoldersDrawer.Refresh();
        }
    }
}

public class ColoredFoldersWindow : EditorWindow, IHasCustomMenu
{
    private static string targetGUID;
    private static ColoredFoldersData foldersData;

    private bool useCustomColor;
    private Color selectedColor = Color.white;
    private List<Color> originalPresetColors = new List<Color>
    {
        new Color(0.7f, 0.15f, 0.25f),  // RED
        new Color(0.0f, 0.65f, 0.5f),  // GREEN
        new Color(0.0f, 0.25f, 0.5f), // BLUE
        new Color(0.8f, 0.8f, 0.3f),  // YELLOW
        new Color(0.4f, 0.8f, 0.95f),  // AQUA
        new Color(0.4f, 0.3f, 0.7f), // PURPLE
        new Color(1.0f, 0.6f, 0.3f),   // ORANGE
        new Color(0.55f, 0.4f, 0.25f), // BROWN
    };
    private List<Color> presetColors = new List<Color>
    {
        new Color(0.7f, 0.15f, 0.25f),  // RED
        new Color(0.0f, 0.65f, 0.5f),  // GREEN
        new Color(0.0f, 0.25f, 0.5f), // BLUE
        new Color(0.8f, 0.8f, 0.3f),  // YELLOW
        new Color(0.4f, 0.8f, 0.95f),  // AQUA
        new Color(0.4f, 0.3f, 0.7f), // PURPLE
        new Color(1.0f, 0.6f, 0.3f),   // ORANGE
        new Color(0.55f, 0.4f, 0.25f), // BROWN
    };
    private Dictionary<Color, Texture2D> colorTextures = new Dictionary<Color, Texture2D>();

    private Vector2 scrollPos;
    private bool isStylesInitDone;
    private GUIStyle buttonStyle;
    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);

    private const string EDITOR_KEY_PRESET_COLORS = "COLORED_FOLDERS_PRESET_COLORS";
    [Serializable]
    private class PresetColorsData
    {
        public List<Color> PresetColors = new List<Color>();
    }

    public static void Open(string guid)
    {
        targetGUID = guid;

        string path = "Assets/ColoredFolders/ColoredFoldersData.asset";
        foldersData = AssetDatabase.LoadAssetAtPath<ColoredFoldersData>(path);
        if(foldersData == null)
        {
            foldersData = ScriptableObject.CreateInstance<ColoredFoldersData>();
            if(!AssetDatabase.IsValidFolder("Assets/ColoredFolders"))
            {
                AssetDatabase.CreateFolder("Assets", "ColoredFolders");
            }
            AssetDatabase.CreateAsset(foldersData, path);
            AssetDatabase.SaveAssets();
        }

        var window = GetWindow<ColoredFoldersWindow>("Colored Folders");
        window.minSize = new Vector2(320f, 320f);

        // set selected color to the current folder color if it exists
        var existingEntry = foldersData.Data.Find(entry => entry.GUID == guid);
        if(existingEntry != null)
        {
            window.useCustomColor = window.presetColors.IndexOf(existingEntry.Color) == -1;
            window.selectedColor = existingEntry.Color;
        }

        window.Show();
    }
    
    private void OnEnable()
    {
        if(EditorPrefs.HasKey(EDITOR_KEY_PRESET_COLORS))
        {
            string json = EditorPrefs.GetString(EDITOR_KEY_PRESET_COLORS);
            var data = JsonUtility.FromJson<PresetColorsData>(json);

            if(data != null && data.PresetColors != null && data.PresetColors.Count > 0)
            {
                presetColors = data.PresetColors;
            }
        }

        CreateColorTextures();
    }
    private void OnDisable()
    {
        var data = new PresetColorsData() { PresetColors = presetColors };
        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EDITOR_KEY_PRESET_COLORS, json);
    }
    private void OnGUI()
    {
        if(!isStylesInitDone) InitializeStyles();

        EditorGUILayout.BeginVertical("Box");
        useCustomColor = EditorGUILayout.ToggleLeft("Use Custom Color?", useCustomColor);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("Box");
        if(useCustomColor)
        {
            selectedColor = EditorGUILayout.ColorField(new GUIContent("Select Custom Color"), selectedColor, true, true, false);
            GUILayout.FlexibleSpace();
        }
        else
        {
            EditorGUILayout.LabelField("Select Preset Color");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical();

            int columns = Mathf.CeilToInt(EditorGUIUtility.currentViewWidth / 80f);
            int rows = Mathf.CeilToInt((float)presetColors.Count / columns);

            for(int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for(int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if(index >= presetColors.Count) break;

                    Color color = presetColors[index];

                    // Space between colors
                    if(col > 0) GUILayout.Space(5);

                    // Get rect with extra space for borders
                    Rect buttonRect = GUILayoutUtility.GetRect(32, 32);
                    EditorGUI.DrawRect(new Rect(buttonRect.x - 1, buttonRect.y - 1, buttonRect.width + 2, buttonRect.height + 2), Color.black);

                    if(GUI.Button(buttonRect, GUIContent.none) && Event.current.button == 0)
                    {
                        selectedColor = color;
                    }
                    EditorGUI.DrawRect(buttonRect, color);

                    // Highlight selected color with extended borders
                    if(selectedColor == color)
                    {
                        EditorGUI.DrawRect(new Rect(buttonRect.x - 2, buttonRect.y - 2, buttonRect.width + 4, buttonRect.height + 4), Color.white);
                        EditorGUI.DrawRect(new Rect(buttonRect.x - 1, buttonRect.y - 1, buttonRect.width + 2, buttonRect.height + 2), Color.black);
                        EditorGUI.DrawRect(buttonRect, color);
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview");

            // preview color border
            Rect previewRect = GUILayoutUtility.GetRect(64, 20);
            EditorGUI.DrawRect(new Rect(previewRect.x - 2, previewRect.y - 2, previewRect.width + 4, previewRect.height + 4), Color.black);
            EditorGUI.DrawRect(previewRect, selectedColor);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        GUI.color = buttonColor;
        if(useCustomColor)
        {
            if(GUILayout.Button("ADD TO PRESETS", buttonStyle, GUILayout.Height(40f)) && Event.current.button == 0)
            {
                presetColors.Add(selectedColor);
            }
        }
        else
        {
            if(GUILayout.Button("REMOVE FROM PRESETS", buttonStyle, GUILayout.Height(40f)) && Event.current.button == 0)
            {
                if(presetColors.Contains(selectedColor))
                {
                    presetColors.Remove(selectedColor);
                }
                selectedColor = Color.white;
            }
        }

        if(GUILayout.Button("APPLY COLOR", buttonStyle, GUILayout.Height(40f)) && Event.current.button == 0)
        {
            ColoredFolderEntry existingEntry = foldersData.Data.Find(entry => entry.GUID == targetGUID);

            if(existingEntry != null)
            {
                existingEntry.Path = AssetDatabase.GUIDToAssetPath(targetGUID);
                existingEntry.Color = selectedColor;
            }
            else
            {
                ColoredFolderEntry newEntry = new ColoredFolderEntry()
                {
                    Path = AssetDatabase.GUIDToAssetPath(targetGUID),
                    GUID = targetGUID,
                    Color = selectedColor
                };
                foldersData.Data.Add(newEntry);
            }

            EditorUtility.SetDirty(foldersData);
            AssetDatabase.SaveAssets();

            ColoredFoldersDrawer.Refresh();

            this.Close();
        }
        GUI.color = Color.white;
    }
    private void CreateColorTextures()
    {
        colorTextures.Clear();
        foreach(Color color in presetColors)
        {
            Texture2D tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for(int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            colorTextures[color] = tex;
        }
    }
    private void InitializeStyles()
    {
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 14;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.wordWrap = true;
    }

    public void AddItemsToMenu(GenericMenu menu)
    {
        menu.AddItem(new GUIContent("Reset Preset Colors"), false, ResetPresetColors);
    }
    private void ResetPresetColors()
    {
        presetColors.Clear();
        presetColors.AddRange(originalPresetColors);
    }
}
