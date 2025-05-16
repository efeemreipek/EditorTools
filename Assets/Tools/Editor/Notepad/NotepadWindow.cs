using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NotepadWindow : EditorWindow
{
    [System.Serializable]
    private class Note
    {
        public string Title;
        public List<NoteTag> NoteTags = new List<NoteTag>();
        public string Content;
        public List<Object> LinkedElements = new List<Object>();

        public Note()
        {
            Title = "New Note";
        }
    }
    [System.Serializable]
    private class NoteTag
    {
        public string Name;
    }
    private enum NotePanelMode
    {
        None,
        View,
        Edit
    }

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
    private List<Note> notes = new List<Note>();
    private int selectedNoteIndex = -1;
    private List<Rect> noteElementRects = new List<Rect>();
    private Rect noteListRect;
    private NotePanelMode panelMode = NotePanelMode.None;

    private string editingTitle = string.Empty;
    private string editingContent = string.Empty;

    private GUIStyle buttonStyle;
    private GUIStyle textAreaFieldStyle;
    private GUIStyle textAreaLabelStyle;
    private GUIStyle titleTextFieldStyle;
    private GUIStyle titleLabelStyle;

    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);

    private void OnGUI()
    {
        if(!isStylesInitDone) InitializeStyles();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.3f));
        GUILayout.Space(Layout.SPACE);

        DrawNoteList();

        if(Event.current.type == EventType.Repaint)
        {
            noteListRect = GUILayoutUtility.GetLastRect();
        }

        DrawCreateButton();
        GUILayout.Space(Layout.SPACE * 0.8f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        DrawNote();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        HandleDeselection(noteListRect, noteElementRects);
    }
    private void HandleDeselection(Rect noteListRect, List<Rect> noteElementRects)
    {
        Event e = Event.current;

        if(e.type == EventType.MouseDown && e.button == 0)
        {
            bool insideNoteList = noteListRect.Contains(e.mousePosition);
            bool clickedOnAnyNote = noteElementRects.Any(r => r.Contains(e.mousePosition));

            if(insideNoteList && !clickedOnAnyNote)
            {
                selectedNoteIndex = -1;
                GUI.FocusControl(null);
                e.Use();
                Repaint();
            }
            else if(!insideNoteList)
            {
                GUI.FocusControl(null);
                e.Use();
                Repaint();
            }
        }
    }

    private void DrawNoteList()
    {
        EditorGUILayout.BeginVertical("Box");

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        noteElementRects.Clear();

        DrawNoteListElements();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    private void DrawNoteListElements()
    {
        for(int i = 0; i < notes.Count; i++)
        {
            Rect rect = GUILayoutUtility.GetRect(200, 40, GUILayout.ExpandWidth(true));
            noteElementRects.Add(rect);
            DrawNoteListElement(i, rect, notes[i]);
        }
    }
    private void DrawNoteListElement(int index, Rect rect, Note note)
    {
        Event e = Event.current;

        if(index == selectedNoteIndex)
        {
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.85f, 0.3f));
        }

        EditorGUI.LabelField(rect, note.Title, EditorStyles.boldLabel);

        if(rect.Contains(e.mousePosition))
        {
            if(e.type == EventType.MouseDown &&  e.button == 0)
            {
                if(e.clickCount == 1)
                {
                    selectedNoteIndex = index;
                    panelMode = NotePanelMode.View;
                    GUI.changed = true;
                }
                else if(e.clickCount == 2)
                {
                    selectedNoteIndex = index;
                    editingTitle = notes[index].Title;
                    editingContent = notes[index].Content;  
                    panelMode = NotePanelMode.Edit;
                    e.Use();
                }
            }
        }
    }
    private void DrawCreateButton()
    {
        EditorGUILayout.BeginHorizontal("Box");
        GUI.color = buttonColor;
        if(GUILayout.Button("CREATE NOTE", buttonStyle, GUILayout.Height(Layout.BUTTON_HEIGHT)) && Event.current.button == 0)
        {
            Note newNote = new Note();
            notes.Add(newNote);
            selectedNoteIndex = notes.IndexOf(newNote);
            editingTitle = newNote.Title;
            editingContent = newNote.Content;

            panelMode = NotePanelMode.Edit;
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
    }
    private void DrawNote()
    {
        if(selectedNoteIndex < 0 || selectedNoteIndex >= notes.Count)
        {
            return;
        }

        Note note = notes[selectedNoteIndex];

        if(panelMode == NotePanelMode.View)
        {
            DrawNoteView(note);
        }
        else if(panelMode == NotePanelMode.Edit)
        {
            DrawNoteEdit(note);
        }
    }
    private void DrawNoteView(Note note)
    {
        EditorGUILayout.BeginVertical();

        // HEADER
        EditorGUILayout.BeginHorizontal("Box", GUILayout.Height(40f));
        EditorGUILayout.LabelField(note.Title, titleLabelStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("NOTE TAGS");
        EditorGUILayout.EndHorizontal();

        // CONTENT
        EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField(note.Content, textAreaLabelStyle);
        EditorGUILayout.EndVertical();

        // LINKED ELEMENTS
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("LINKED ELEMENTS");
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();
    }
    private void DrawNoteEdit(Note note)
    {
        EditorGUILayout.BeginVertical();

        // HEADER
        EditorGUILayout.BeginHorizontal("Box", GUILayout.Height(40f));
        editingTitle = EditorGUILayout.TextField(editingTitle, titleTextFieldStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("NOTE TAGS");
        EditorGUILayout.EndHorizontal();

        // CONTENT
        EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true));
        editingContent = EditorGUILayout.TextArea(editingContent, textAreaFieldStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndVertical();

        // LINKED ELEMENTS
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("LINKED ELEMENTS");
        EditorGUILayout.EndVertical();

        // CONTROL BUTTONS
        EditorGUILayout.BeginHorizontal("Box");
        float buttonWidth = EditorGUIUtility.currentViewWidth * 0.35f - Layout.SPACE * 2.5f; // half of currentViewWidth * 0.7f
        GUI.color = buttonColor;
        if(GUILayout.Button("SAVE", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(Layout.BUTTON_HEIGHT)))
        {
            note.Title = editingTitle;
            note.Content = editingContent;

            panelMode = NotePanelMode.View;
            Repaint();
        }
        if(GUILayout.Button("CANCEL", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(Layout.BUTTON_HEIGHT)))
        {
            editingTitle = string.Empty;
            editingContent = string.Empty;

            panelMode = NotePanelMode.View;
            Repaint();
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

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

        textAreaFieldStyle = new GUIStyle(GUI.skin.textArea);
        textAreaFieldStyle.wordWrap = true;
        textAreaFieldStyle.fontSize = 16;

        textAreaLabelStyle = new GUIStyle(GUI.skin.label);
        textAreaLabelStyle.wordWrap = true;
        textAreaLabelStyle.fontSize = 16;

        titleTextFieldStyle = new GUIStyle(GUI.skin.textField);
        titleTextFieldStyle.alignment = TextAnchor.MiddleLeft;
        titleTextFieldStyle.fontSize = 20;
        titleTextFieldStyle.fontStyle = FontStyle.Bold;

        titleLabelStyle = new GUIStyle(GUI.skin.label);
        titleLabelStyle.alignment = TextAnchor.MiddleLeft;
        titleLabelStyle.fontSize = 20;
        titleLabelStyle.fontStyle = FontStyle.Bold;
    }
}
