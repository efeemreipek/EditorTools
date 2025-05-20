using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class AutoSave : EditorWindow
{
    [MenuItem("Tools/UI Toolkit/AutoSave")]
    public static void ShowExample()
    {
        AutoSave wnd = GetWindow<AutoSave>();
        wnd.titleContent = new GUIContent("AutoSave");
        wnd.minSize = new Vector2(300, 75);
        wnd.maxSize = wnd.minSize;
    }

    private const string EDITOR_KEY_AUTOSAVE = "AUTOSAVE_ENABLED";
    private const string EDITOR_KEY_INTERVAL = "AUTOSAVE_INTERVAL";

    private Toggle autoSaveToggle;
    private SliderInt saveIntervalSlider;
    private Label nextSaveLabel;

    private bool autoSaveEnabled;
    private double nextSaveTime = 0;
    private int intervalInMinutes = 1;


    public void CreateGUI()
    {
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Tools/Editor/AutoSave/AutoSave.uxml");
        VisualElement root = visualTree.Instantiate();
        rootVisualElement.Add(root);

        autoSaveToggle = root.Q<Toggle>("AutoSaveToggle");
        saveIntervalSlider = root.Q<SliderInt>("SaveIntervalSlider");
        nextSaveLabel = root.Q<Label>("NextSaveLabel");

        autoSaveEnabled = EditorPrefs.GetBool(EDITOR_KEY_AUTOSAVE);
        intervalInMinutes = EditorPrefs.GetInt(EDITOR_KEY_INTERVAL);

        autoSaveToggle.value = autoSaveEnabled;
        saveIntervalSlider.value = intervalInMinutes;

        autoSaveToggle.RegisterValueChangedCallback(evt => 
        {
            autoSaveEnabled = evt.newValue;
            nextSaveTime = EditorApplication.timeSinceStartup + intervalInMinutes * 60f;
            UpdateNextSaveLabel();
        });

        saveIntervalSlider.RegisterValueChangedCallback(evt =>
        {
            intervalInMinutes = evt.newValue;
            if(autoSaveEnabled)
            {
                nextSaveTime = EditorApplication.timeSinceStartup + intervalInMinutes * 60f;
            }
            UpdateNextSaveLabel();
        });

        EditorApplication.update += OnUpdate;
    }
    private void OnDisable()
    {
        EditorApplication.update -= OnUpdate;

        EditorPrefs.SetBool(EDITOR_KEY_AUTOSAVE, autoSaveEnabled);
        EditorPrefs.SetInt(EDITOR_KEY_INTERVAL, intervalInMinutes);
    }

    private void OnUpdate()
    {
        if(!autoSaveEnabled) return;

        if(EditorApplication.timeSinceStartup >= nextSaveTime)
        {
            Save();
            nextSaveTime = EditorApplication.timeSinceStartup + intervalInMinutes * 60f;
        }

        UpdateNextSaveLabel();
    }
    private void UpdateNextSaveLabel()
    {
        if(autoSaveEnabled)
        {
            double timeRemaining = nextSaveTime - EditorApplication.timeSinceStartup;
            if(timeRemaining < 0) timeRemaining = 0;
            nextSaveLabel.text = $"Next save in {timeRemaining:F0} seconds";
        }
        else
        {
            nextSaveLabel.text = "Next save in -";
        }
    }
    private void Save()
    {
        if(Application.isPlaying) return;

        Scene scene = SceneManager.GetActiveScene();
        if(string.IsNullOrEmpty(scene.path)) return;

        Debug.Log($"[AutoSave] Saving scene at {DateTime.Now:HH:mm:ss}");

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}
