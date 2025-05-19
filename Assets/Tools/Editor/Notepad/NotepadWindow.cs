using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

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
    [System.Serializable]
    private class NoteData
    {
        public List<Note> Notes;
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
    private bool linkedElementsFolded;
    private Dictionary<Note, ReorderableList> viewLists = new Dictionary<Note, ReorderableList>();
    private bool isTagButtonClicked;
    private string tagSearchText = string.Empty;

    private string editingTitle = string.Empty;
    private string editingContent = string.Empty;
    private List<Object> editingLinkedElements = new List<Object>();
    private string editingTagName = string.Empty;
    private List<NoteTag> editingNoteTags = new List<NoteTag>();

    private GUIStyle buttonStyle;
    private GUIStyle textAreaFieldStyle;
    private GUIStyle textAreaLabelStyle;
    private GUIStyle titleTextFieldStyle;
    private GUIStyle titleLabelStyle;
    private GUIStyle foldoutStyle;
    private GUIStyle noteLabelStyle;
    private GUIStyle tagLabelStyle;

    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);
    private Color xButtonColor = new Color(0.93f, 0.38f, 0.34f);

    private const string EDITOR_KEY_NOTEPAD = "NOTEPAD_NOTES";

    private void OnEnable()
    {
        if(EditorPrefs.HasKey(EDITOR_KEY_NOTEPAD))
        {
            string json = EditorPrefs.GetString(EDITOR_KEY_NOTEPAD);
            var data = JsonUtility.FromJson<NoteData>(json);

            if(data != null && data.Notes != null && data.Notes.Count > 0)
            {
                notes = data.Notes;
            }
        }
    }
    private void OnDisable()
    {
        var data = new NoteData() { Notes = notes };
        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EDITOR_KEY_NOTEPAD, json);
    }
    private void OnGUI()
    {
        if(!isStylesInitDone) InitializeStyles();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.25f));
        GUILayout.Space(Layout.SPACE);

        DrawSearchBar();
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
    private void DrawSearchBar()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Search by Tags");
        tagSearchText = EditorGUILayout.TextField(tagSearchText, EditorStyles.toolbarSearchField);
        EditorGUILayout.EndVertical();
    }
    private List<Note> GetTagSearchedNotes()
    {
        if(string.IsNullOrEmpty(tagSearchText)) return notes;

        string lowerSearch = tagSearchText.ToLower();

        return notes
            .Where(note => note.NoteTags
            .Any(tag => tag.Name.ToLower().Contains(lowerSearch)))
            .ToList();
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
        List<Note> filteredNotes = GetTagSearchedNotes();

        for(int i = 0; i < filteredNotes.Count; i++)
        {
            Note note = filteredNotes[i];
            Rect rect = GUILayoutUtility.GetRect(200, 40, GUILayout.ExpandWidth(true));
            noteElementRects.Add(rect);
            DrawNoteListElement(i, rect, note);
        }
    }
    private void DrawNoteListElement(int index, Rect rect, Note note)
    {
        Event e = Event.current;

        if(index == selectedNoteIndex)
        {
            EditorGUI.DrawRect(rect, new Color(1f, 0.65f, 0.1f, 0.3f));
        }

        float titleHeight = 20f;
        float titleY = note.NoteTags.Count > 0 ? rect.y - 5f : rect.y + 5f;
        float tagsHeight = note.NoteTags.Count > 0 ? 20f : 0f;
        float totalHeight = titleHeight + tagsHeight + 10f;

        Rect titleRect = new Rect(rect.x + 5f, titleY, rect.width - 35f, rect.height - 10f);
        Rect tagsRect = new Rect(rect.x + 5f, rect.y + titleHeight, rect.width - 40f, tagsHeight);
        Rect xButtonRect = new Rect(rect.xMax - 35f, rect.y + 5f, 30f, 30f);

        EditorGUI.LabelField(titleRect, note.Title, noteLabelStyle);

        if(note.NoteTags.Count > 0)
        {
            float tagX = tagsRect.x;
            float tagY = tagsRect.y + 2f;
            float tagHeight = 18f;

            foreach(var tag in note.NoteTags)
            {
                GUIContent tagContent = new GUIContent(tag.Name);
                Vector2 tagSize = tagLabelStyle.CalcSize(tagContent);
                Rect tagRect = new Rect(tagX, tagY, tagSize.x + 10f, tagHeight);

                GUI.Label(tagRect, tag.Name, tagLabelStyle);

                tagX += tagRect.width + 5f;
                if(tagX > tagsRect.xMax - 20f) break;
            }
        }

        GUI.color = xButtonColor;
        if(GUI.Button(xButtonRect, "X", buttonStyle) && e.button == 0)
        {
            notes.Remove(note);
            if(selectedNoteIndex == index)
            {
                selectedNoteIndex = -1;
            }
            else if(selectedNoteIndex > index)
            {
                selectedNoteIndex--;
            }

            panelMode = NotePanelMode.None;
            GUI.changed = true;
            return;
        }
        GUI.color = Color.white;

        if(rect.Contains(e.mousePosition) && !xButtonRect.Contains(e.mousePosition))
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
                    PrepareNoteForEditing(notes[index]);
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
            editingLinkedElements = new List<Object>();
            editingNoteTags = new List<NoteTag>();
            isTagButtonClicked = false;

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
        // TITLE
        EditorGUILayout.LabelField(note.Title, titleLabelStyle, GUILayout.ExpandHeight(true), GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
        // TAGS LABEL
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
        // TAGS
        EditorGUILayout.BeginHorizontal();
        DrawTags(note.NoteTags);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        // CONTENT
        EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField(note.Content, textAreaLabelStyle);
        EditorGUILayout.EndVertical();

        // LINKED ELEMENTS
        EditorGUILayout.BeginVertical("Box", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.2f));
        linkedElementsFolded = EditorGUILayout.Foldout(linkedElementsFolded, "Linked Elements", false, foldoutStyle);
        if(linkedElementsFolded) GetOrCreateViewList(note).DoLayoutList();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
    private void DrawNoteEdit(Note note)
    {
        EditorGUILayout.BeginVertical();

        // HEADER
        EditorGUILayout.BeginHorizontal("Box", GUILayout.Height(40f));
        // TITLE
        editingTitle = EditorGUILayout.TextField(editingTitle, titleTextFieldStyle, GUILayout.ExpandHeight(true), GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        // TAGS LABEL
        EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel, GUILayout.Width(50f));
        // TAG BUTTON
        if(editingNoteTags.Count < 3)
        {
            GUI.color = buttonColor;
            if(GUILayout.Button("+", buttonStyle, GUILayout.Width(50f)))
            {
                if(isTagButtonClicked)
                {
                    if(!string.IsNullOrEmpty(editingTagName))
                    {
                        CreateAndAddTag(editingTagName, note);
                        editingTagName = string.Empty;
                    }
                    isTagButtonClicked = false;
                }
                else
                {
                    isTagButtonClicked = true;
                    editingTagName = string.Empty;
                }
                GUI.FocusControl(null);
                Repaint();
            }
            GUI.color = Color.white;
            // TAG NAME FIELD
            if(isTagButtonClicked)
            {
                editingTagName = EditorGUILayout.TextField(editingTagName);
            }
        }
        EditorGUILayout.EndHorizontal();
        // TAGS
        EditorGUILayout.BeginHorizontal();
        DrawEditingTags();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        // CONTENT
        EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true));
        editingContent = EditorGUILayout.TextArea(editingContent, textAreaFieldStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndVertical();

        // LINKED ELEMENTS
        EditorGUILayout.BeginVertical("Box", GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.2f));
        linkedElementsFolded = EditorGUILayout.Foldout(linkedElementsFolded, "Linked Elements", false, foldoutStyle);
        if(linkedElementsFolded) GetTempEditList().DoLayoutList();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // CONTROL BUTTONS
        EditorGUILayout.BeginHorizontal("Box");
        float buttonWidth = EditorGUIUtility.currentViewWidth * 0.375f - Layout.SPACE * 2f; // half of currentViewWidth * 0.75f
        GUI.color = buttonColor;
        if(GUILayout.Button("SAVE", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(Layout.BUTTON_HEIGHT)))
        {
            SaveNoteChanges(note);

            panelMode = NotePanelMode.View;
            Repaint();
        }
        if(GUILayout.Button("CANCEL", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(Layout.BUTTON_HEIGHT)))
        {
            CancelNoteEditing();

            panelMode = NotePanelMode.View;
            Repaint();
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
    private void PrepareNoteForEditing(Note note)
    {
        editingTitle = note.Title;
        editingContent = note.Content;
        editingLinkedElements = new List<Object>(note.LinkedElements);

        editingNoteTags = new List<NoteTag>();
        foreach(var tag in note.NoteTags)
        {
            editingNoteTags.Add(new NoteTag() { Name = tag.Name });
        }

        panelMode = NotePanelMode.Edit;
    }
    private void SaveNoteChanges(Note note)
    {
        note.Title = editingTitle;
        note.Content = editingContent;

        note.LinkedElements.Clear();
        note.LinkedElements.AddRange(editingLinkedElements);       

        note.NoteTags.Clear();
        note.NoteTags.AddRange(editingNoteTags);

        ClearEditingData();
    }
    private void CancelNoteEditing()
    {
        ClearEditingData();
    }
    private void ClearEditingData()
    {
        editingTitle = string.Empty;
        editingContent = string.Empty;
        editingLinkedElements.Clear();

        isTagButtonClicked = false;
        editingTagName = string.Empty;
    }
    private ReorderableList GetTempEditList()
    {
        ReorderableList list = new ReorderableList(editingLinkedElements, typeof(Object), true, false, true, true);
        list.drawElementCallback = (rect, index, _, __) =>
        {
            editingLinkedElements[index] = EditorGUI.ObjectField(
                new Rect(rect.x, rect.y + 5f, rect.width, EditorGUIUtility.singleLineHeight),
                editingLinkedElements[index],
                typeof(Object),
                true
            );
        };

        list.elementHeight = EditorGUIUtility.singleLineHeight + 8f;

        return list;
    }
    private ReorderableList GetOrCreateViewList(Note note)
    {
        if(!viewLists.TryGetValue(note, out var list))
        {
            list = new ReorderableList(note.LinkedElements, typeof(Object), false, false, false, false);
            list.drawElementCallback = (rect, index, _, __) =>
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y + 5f, rect.width, EditorGUIUtility.singleLineHeight),
                    note.LinkedElements[index],
                    typeof(Object),
                    true
                    );
                EditorGUI.EndDisabledGroup();
            };

            list.elementHeight = EditorGUIUtility.singleLineHeight + 8f;

            viewLists[note] = list;
        }
        return list;
    }
    private void DrawTags(List<NoteTag> tags)
    {
        foreach(var tag in tags)
        {
            GUIContent content = new GUIContent(tag.Name);
            Vector2 size = tagLabelStyle.CalcSize(content);
            EditorGUILayout.LabelField(content, tagLabelStyle, GUILayout.Width(size.x + 10f));
            GUILayout.Space(5f);
        }
    }
    private void DrawEditingTags()
    {
        for(int i = 0; i < editingNoteTags.Count; i++)
        {
            GUIContent content = new GUIContent(editingNoteTags[i].Name);
            Vector2 labelSize = tagLabelStyle.CalcSize(content);
            float labelWidth = labelSize.x + 10f;

            EditorGUILayout.BeginHorizontal(GUILayout.Width(labelWidth + 20f));

            EditorGUILayout.LabelField(content, tagLabelStyle, GUILayout.Width(labelWidth));

            GUI.color = xButtonColor;
            if(GUILayout.Button("X", GUILayout.Width(20f), GUILayout.Height(18f)))
            {
                editingNoteTags.RemoveAt(i);
                i--;
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5f);
        }
    }
    private void CreateAndAddTag(string editingTagName, Note note)
    {
        NoteTag tag = new NoteTag() { Name = editingTagName };
        editingNoteTags.Add(tag);
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

        foldoutStyle = new GUIStyle(EditorStyles.foldout);
        foldoutStyle.fontSize = 14;
        foldoutStyle.fontStyle = FontStyle.Bold;

        noteLabelStyle = new GUIStyle(GUI.skin.label);
        noteLabelStyle.fontSize = 14;
        noteLabelStyle.fontStyle = FontStyle.Bold;

        tagLabelStyle = new GUIStyle("AssetLabel");
        tagLabelStyle.alignment = TextAnchor.MiddleCenter;
        tagLabelStyle.fontSize = 12;
    }
}