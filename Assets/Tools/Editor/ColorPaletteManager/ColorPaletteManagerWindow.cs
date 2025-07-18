using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

public class ColorPaletteManagerWindow : EditorWindow
{
    [Serializable]
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
    [Serializable]
    private class ColorsData
    {
        public List<ColorName> Colors = new List<ColorName>();
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
    private int renameIndex = -1;
    private string renameBuffer = string.Empty;
    private string textFieldControlName = "RenameTextField";
    private bool shouldSelectAllText = false;

    private bool isStylesInitDone;
    private GUIStyle middleLabelStyle;
    private GUIStyle buttonStyle;
    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);
    private Color xButtonColor = new Color(0.93f, 0.38f, 0.34f);

    private const string EDITOR_KEY_COLORS = "COLOR_PALETTE_COLORS";

    private void OnEnable()
    {
        if(EditorPrefs.HasKey(EDITOR_KEY_COLORS))
        {
            string json = EditorPrefs.GetString(EDITOR_KEY_COLORS);
            var data = JsonUtility.FromJson<ColorsData>(json);

            if(data != null && data.Colors != null && data.Colors.Count > 0)
            {
                colors = data.Colors;
            }
        }

        InitReorderableList();
    }
    private void OnDisable()
    {
        var data = new ColorsData() { Colors = colors };
        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EDITOR_KEY_COLORS, json);
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
        if(GUILayout.Button("ADD COLOR", buttonStyle, GUILayout.Height(30f)) && Event.current.button == 0)
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

            Event e = Event.current;

            if(e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Rename"), false, () =>
                {
                    renameIndex = index;
                    renameBuffer = colors[index].Name;
                    shouldSelectAllText = true;
                    Repaint();
                });

                menu.AddItem(new GUIContent("Duplicate"), false, () =>
                {
                    ColorName originalColor = colors[index];
                    ColorName duplicatedColor = new ColorName(originalColor.Name + " Copy", originalColor.Color);
                    colors.Insert(index + 1, duplicatedColor);
                    reorderableList.list = colors;
                    Repaint();
                });

                menu.AddItem(new GUIContent("Copy HEX"), false, () =>
                {
                    Color selectedColor = colors[index].Color;
                    string hexColor = ColorUtility.ToHtmlStringRGBA(selectedColor);
                    EditorGUIUtility.systemCopyBuffer = "#" + hexColor;
                });

                menu.AddItem(new GUIContent("Delete"), false, () =>
                {
                    colorToRemove = colors[index];
                    Repaint();
                });

                menu.ShowAsContext();
                e.Use();
                return;
            }

            if(renameIndex == index)
            {
                GUI.SetNextControlName(textFieldControlName);

                EditorGUI.BeginChangeCheck();
                string newText = EditorGUI.TextField(labelRect, renameBuffer);
                if(EditorGUI.EndChangeCheck())
                {
                    renameBuffer = newText;
                }

                if(shouldSelectAllText && e.type == EventType.Repaint)
                {
                    shouldSelectAllText = false;
                    EditorGUI.FocusTextInControl(textFieldControlName);

                    // Schedule selection for next frame
                    if(EditorApplication.update != null)
                    {
                        EditorApplication.update -= SelectAllTextDelayed;
                    }
                    EditorApplication.update += SelectAllTextDelayed;
                }

                if((e.type == EventType.KeyUp && e.keyCode == KeyCode.Return) || (e.type == EventType.MouseDown && !labelRect.Contains(e.mousePosition)))
                {
                    colorItem.Name = renameBuffer;
                    renameIndex = -1;
                    shouldSelectAllText = false;
                    GUI.FocusControl(null);
                    Repaint();
                }
            }
            else
            {
                EditorGUI.LabelField(labelRect, colorItem.Name);

                if(e.type == EventType.MouseDown && e.clickCount == 2 && labelRect.Contains(e.mousePosition))
                {
                    renameIndex = index;
                    renameBuffer = colorItem.Name;
                    shouldSelectAllText = true;
                    e.Use();
                }
            }

            colorItem.Color = EditorGUI.ColorField(colorRect, GUIContent.none, colorItem.Color, false, true, false);

            GUI.color = xButtonColor;
            if(GUI.Button(xRect, "X") && Event.current.button == 0)
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
    private void SelectAllTextDelayed()
    {
        EditorApplication.update -= SelectAllTextDelayed;

        if(GUI.GetNameOfFocusedControl() == textFieldControlName)
        {
            TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if(textEditor != null)
            {
                textEditor.SelectAll();
                Repaint();
            }
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
