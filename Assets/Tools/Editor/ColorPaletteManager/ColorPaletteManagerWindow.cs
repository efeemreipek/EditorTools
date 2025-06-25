using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

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
    private Color color = new Color(1f, 1f, 1f, 1f);
    private List<ColorName> colors = new List<ColorName>();
    private ReorderableList reorderableList;
    private ColorName colorToRemove = null;

    private bool isStylesInitDone;
    private GUIStyle middleLabelStyle;
    private GUIStyle buttonStyle;
    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);
    private Color xButtonColor = new Color(0.93f, 0.38f, 0.34f);

    private void OnEnable()
    {
        InitReorderableList();
    }
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
        GUI.color = buttonColor;
        if(GUILayout.Button("ADD COLOR", buttonStyle, GUILayout.Height(30f)))
        {
            AddColor(colorName, color);
        }
        GUI.color = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("Box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        if(reorderableList != null)
        {
            reorderableList.DoLayoutList();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // delete if pending removal
        if(colorToRemove != null)
        {
            RemoveColor(colorToRemove);
            colorToRemove = null;
        }

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            reorderableList.ClearSelection();
            Repaint();
        }
    }

    private void InitReorderableList()
    {
        reorderableList = new ReorderableList(colors, typeof(ColorName), true, false, false, false);

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            ColorName colorItem = colors[index];
            float labelWidth = rect.width * 0.3f;
            float buttonWidth = 25f;
            float colorWidth = rect.width - labelWidth - buttonWidth - 10f;

            Rect labelRect = new Rect(rect.x, rect.y + 4, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect colorRect = new Rect(rect.x + labelWidth + 5, rect.y + 4, colorWidth, EditorGUIUtility.singleLineHeight);
            Rect xRect = new Rect(rect.x + rect.width - buttonWidth, rect.y + 4, buttonWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, colorItem.Name);
            colorItem.Color = EditorGUI.ColorField(colorRect, GUIContent.none, colorItem.Color, false, true, false);

            GUI.color = xButtonColor;
            if(GUI.Button(xRect, "X"))
            {
                colorToRemove = colorItem;
            }
            GUI.color = Color.white;
        };

        reorderableList.elementHeightCallback = (int index) =>
        {
            return EditorGUIUtility.singleLineHeight + 8;
        };
    }
    private void InitializeStyles()
    {
        isStylesInitDone = true;

        middleLabelStyle = new GUIStyle(GUI.skin.label);
        middleLabelStyle.alignment = TextAnchor.MiddleCenter;
        middleLabelStyle.fontStyle = FontStyle.Bold;
        middleLabelStyle.fontSize = 24;
        middleLabelStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 15;
        buttonStyle.normal.textColor = Color.white;
    }
    private void AddColor(string name, Color color)
    {
        if(string.IsNullOrEmpty(name)) name = $"Color #{colors.Count + 1}";
        ColorName newColorName = new ColorName(name, color);
        colors.Add(newColorName);

        colorName = string.Empty;

        reorderableList.list = colors;
    }
    private void RemoveColor(ColorName color)
    {
        colors.Remove(color);
        reorderableList.list = colors;
    }
}
