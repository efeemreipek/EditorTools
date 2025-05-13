using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSaveWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 75f;
        public const float SPACE = 5f;
    }

    [MenuItem("Tools/Auto Save")]
    public static void ShowWindow()
    {
        var win = GetWindow<AutoSaveWindow>("Auto Save");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private bool autoSaveEnabled;
    private bool previousAutoSaveEnabled;
    private double nextSaveTime = 0;
    private float intervalInMinutes = 5f;

    private const string EDITOR_KEY_AUTOSAVE = "AUTOSAVE_ENABLED";
    private const string EDITOR_KEY_INTERVAL = "AUTOSAVE_INTERVAL";

    private void OnEnable()
    {
        EditorApplication.update += OnUpdate;

        autoSaveEnabled = EditorPrefs.GetBool(EDITOR_KEY_AUTOSAVE);
        intervalInMinutes = EditorPrefs.GetFloat(EDITOR_KEY_INTERVAL);
    }
    private void OnDisable()
    {
        EditorApplication.update -= OnUpdate;

        EditorPrefs.SetBool(EDITOR_KEY_AUTOSAVE, autoSaveEnabled);
        EditorPrefs.SetFloat(EDITOR_KEY_INTERVAL, intervalInMinutes);
    }

    private void OnUpdate()
    {
        if(!autoSaveEnabled) return;

        Repaint();

        if(EditorApplication.timeSinceStartup >= nextSaveTime)
        {
            Save();
            nextSaveTime = EditorApplication.timeSinceStartup + intervalInMinutes * 60f;
        }
    }
    private void OnGUI()
    {
        EditorGUILayout.BeginVertical("Box");

        bool newAutoSaveEnabled = EditorGUILayout.ToggleLeft("Enable Auto Save", autoSaveEnabled);
        if(newAutoSaveEnabled && !previousAutoSaveEnabled)
        {
            nextSaveTime = EditorApplication.timeSinceStartup + intervalInMinutes * 60f;
        }
        autoSaveEnabled = newAutoSaveEnabled;
        previousAutoSaveEnabled = newAutoSaveEnabled;

        GUILayout.Space(Layout.SPACE);

        intervalInMinutes = EditorGUILayout.Slider("Save interval (in minutes)", intervalInMinutes, 1f, 30f);

        if(autoSaveEnabled)
        {
            double timeRemaining = nextSaveTime - EditorApplication.timeSinceStartup;
            if(timeRemaining < 0) timeRemaining = 0;
            EditorGUILayout.LabelField($"Next save in {timeRemaining:F0} seconds");
        }
        else
        {
            EditorGUILayout.LabelField("Next save in -");
        }

        EditorGUILayout.EndVertical();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }
    private void Save()
    {
        if(Application.isPlaying) return;

        Scene currentScene = SceneManager.GetActiveScene();
        if(string.IsNullOrEmpty(currentScene.path)) return;

        Debug.Log($"[AutoSave] Saving scene at {DateTime.Now:HH:mm:ss}");

        EditorSceneManager.SaveScene(currentScene);
        AssetDatabase.SaveAssets();
    }
}
