using UnityEditor;
using UnityEngine;

public class NotepadWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 1000f;
        public const float MIN_WINDOW_HEIGHT = 500f;
        public const float SPACE = 5f;
        public const float BUTTON_HEIGHT = 50f;
    }

    [MenuItem("Tools/Notepad")]
    public static void ShowWindow()
    {
        var win = GetWindow<NotepadWindow>("Notepad");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private Vector2 scrollPos;
    private bool isStylesInitDone;

    private GUIStyle buttonStyle;

    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);

    private void OnGUI()
    {
        if(!isStylesInitDone) InitializeStyles();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.3f));
        DrawNoteList();
        DrawCreateButton();
        EditorGUILayout.EndVertical();

        GUILayout.Space(Layout.SPACE);

        EditorGUILayout.BeginVertical();
        DrawNote();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    private void DrawNoteList()
    {
        EditorGUILayout.BeginVertical("Box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    private void DrawCreateButton()
    {
        EditorGUILayout.BeginHorizontal("Box");
        GUI.color = buttonColor;
        if(GUILayout.Button("CREATE NOTE", buttonStyle, GUILayout.Height(Layout.BUTTON_HEIGHT)) && Event.current.button == 0)
        {

        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
    }
    private void DrawNote()
    {
        EditorGUILayout.BeginVertical("Box");

        // HEADER
        EditorGUILayout.BeginHorizontal("Box");
        EditorGUILayout.LabelField("NOTE TITLE");
        EditorGUILayout.LabelField("NOTE TAGS");
        EditorGUILayout.EndHorizontal();

        // CONTENT
        EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("NOTE CONTENT");
        EditorGUILayout.EndVertical();

        // LINKED ELEMENTS
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("LINKED ELEMENTS");
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();
    }

    private void InitializeStyles()
    {
        isStylesInitDone = true;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 14;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.wordWrap = true;
    }
}
