using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ColorPaletteManagerWindow : EditorWindow
{
    [System.Serializable]
    private class ColorName
    {
        public string Name;
        public Color Color;

        public ColorName(string name, Color color)
        {
            Name = name;
            Color = color;
        }
    }
    private static class Layout
    {
        public const float MIN_WINDOW_WIDTH = 400f;
        public const float MIN_WINDOW_HEIGHT = 500f;
        public const float SPACE = 5f;
    }

    [MenuItem("Tools/Color Palette Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<ColorPaletteManagerWindow>("Color Palette Manager");
        window.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private Vector2 scrollPos;
    private string colorName = string.Empty;
    private Color color;
    private List<ColorName> colors = new List<ColorName>();

    private bool isStylesInitDone;
    private GUIStyle middleLabelStyle;

    private void OnGUI()
    {
        if(!isStylesInitDone) InitializeStyles();

        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.LabelField("COLOR PALETTE MANAGER", middleLabelStyle);
        GUILayout.Space(Layout.SPACE);
        colorName = EditorGUILayout.TextField("Name", colorName);
        color = EditorGUILayout.ColorField("Color", color);
        GUILayout.Space(Layout.SPACE);
        if(GUILayout.Button("Add Color"))
        {
            AddColor(colorName, color);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("Box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawColors();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    private void InitializeStyles()
    {
        isStylesInitDone = true;

        middleLabelStyle = new GUIStyle(GUI.skin.label);
        middleLabelStyle.alignment = TextAnchor.MiddleCenter;
        middleLabelStyle.fontStyle = FontStyle.Bold;
        middleLabelStyle.fontSize = 24;
        middleLabelStyle.normal.textColor = Color.white;
    }
    private void AddColor(string name, Color color)
    {
        if(name == string.Empty) name = $"Color #{colors.Count + 1}";
        ColorName newColorName = new ColorName(name, color);
        colors.Add(newColorName);

        colorName = string.Empty;
    }
    private void DrawColors()
    {
        for(int i = 0; i < colors.Count; i++)
        {
            ColorName color = colors[i];

            color.Color = EditorGUILayout.ColorField(new GUIContent(color.Name), color.Color, false, true, false);

            GUILayout.Space(Layout.SPACE);
        }
    }
}
