using UnityEditor;
using UnityEngine;

public class PlayerPrefsViewerWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 400f;
        public const float MIN_WINDOW_HEIGHT = 500f;
        public const float SPACE = 5f;
    }

    [MenuItem("Tools/PlayerPrefs Viewer")]
    public static void ShowWindow()
    {
        var win = GetWindow<PlayerPrefsViewerWindow>("PlayerPrefs Viewer");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private enum PlayerPrefType { String, Float, Int, Bool };

    private Vector2 mainScrollPos;
    private Vector2 listScrollPos;
    private string location;
    private string searchFilter = string.Empty;
    private bool showOnlyFiltered = false;

    private string newKey = string.Empty;
    private object newValue = null;
    private PlayerPrefType newType = PlayerPrefType.String;

    private void OnEnable()
    {
        location = GetPlayerPrefsLocation();
    }
    private void OnGUI()
    {
        mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);
        DrawHeader();
        DrawAddNewEntry();
        DrawSearchFilter();
        DrawEntries();
        DrawFooter();
        EditorGUILayout.EndScrollView();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal("Box");

        GUILayout.Label("PlayerPrefs Viewer", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if(GUILayout.Button("Refresh", GUILayout.Width(80f)))
        {

        }
        if(GUILayout.Button("Delete All", GUILayout.Width(80f)))
        {

        }
        if(GUILayout.Button("Open Location", GUILayout.Width(100f)))
        {

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(Layout.SPACE);
        EditorGUILayout.HelpBox($"PlayerPrefs Location:\n{location}", MessageType.Info);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
    private void DrawAddNewEntry()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField("Add New Entry", EditorStyles.boldLabel);
        EditorGUILayout.Space(Layout.SPACE);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key:", GUILayout.Width(60f));
        newKey = EditorGUILayout.TextField(newKey, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2f);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type:", GUILayout.Width(60f));
        GUILayout.FlexibleSpace();
        newType = (PlayerPrefType)EditorGUILayout.EnumPopup(newType, GUILayout.Width(120f));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2f);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Value:", GUILayout.Width(60f));

        switch(newType)
        {
            case PlayerPrefType.String:
                if(newValue == null ||!(newValue is string)) newValue = string.Empty;
                newValue = EditorGUILayout.TextField((string)newValue, GUILayout.ExpandWidth(true));
                break;
            case PlayerPrefType.Float:
                if(newValue == null || !(newValue is float)) newValue = 0f;
                newValue = EditorGUILayout.FloatField((float)newValue, GUILayout.ExpandWidth(true));
                break;
            case PlayerPrefType.Int:
                if(newValue == null || !(newValue is int)) newValue = 0;
                newValue = EditorGUILayout.IntField((int)newValue, GUILayout.ExpandWidth(true));
                break;
            case PlayerPrefType.Bool:
                if(newValue == null || !(newValue is bool)) newValue = false;
                GUILayout.FlexibleSpace();
                newValue = EditorGUILayout.Toggle((bool)newValue, GUILayout.Width(15f));
                break;
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2f);

        if(GUILayout.Button("Add"))
        {

        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
    private void DrawSearchFilter()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.LabelField("Search", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.ExpandWidth(true));
        GUILayout.Space(Layout.SPACE);
        showOnlyFiltered = EditorGUILayout.ToggleLeft("Show only filtered", showOnlyFiltered, GUILayout.Width(120f));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(2f);

        if(GUILayout.Button("Search"))
        {

        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
    private void DrawEntries()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);
        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
    private void DrawFooter()
    {
        EditorGUILayout.BeginVertical("Box");

        if(showOnlyFiltered)
        {
            EditorGUILayout.LabelField($"Showing 0 of 0 entries", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"Total entries: 0", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private string GetPlayerPrefsLocation()
    {
#if UNITY_EDITOR_WIN
        return $@"Registry: HKEY_CURRENT_USER\Software\{PlayerSettings.companyName}\{PlayerSettings.productName}";
#elif UNITY_EDITOR_OSX
        return $"~/Library/Preferences/unity.{PlayerSettings.companyName}.{PlayerSettings.productName}.plist";
#elif UNITY_EDITOR_LINUX
        return $"~/.config/unity3d/{PlayerSettings.companyName}/{PlayerSettings.productName}/prefs";
#else
        return "Unknown platform";
#endif
    }
}
