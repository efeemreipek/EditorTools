using System;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

[InitializeOnLoad]
public static class TimeScaleSlider
{
    private static float timeScale = 1f;
    private static readonly float minTimeScale = 0f;
    private static readonly float maxTimeScale = 2f;

    private const string EDITOR_KEY_TIMESCALE = "TIMESCALE_SLIDER";

    static TimeScaleSlider()
    {
        if(EditorPrefs.HasKey(EDITOR_KEY_TIMESCALE)) timeScale = EditorPrefs.GetFloat(EDITOR_KEY_TIMESCALE);
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if(state == PlayModeStateChange.EnteredPlayMode)
        {
            Time.timeScale = timeScale;
        }
    }

    private static void OnToolbarGUI()
    {
        GUILayout.Space(10);
        GUILayout.BeginHorizontal("Box");

        GUILayout.Label("TimeScale");
        timeScale = GUILayout.HorizontalSlider(timeScale, minTimeScale, maxTimeScale, GUILayout.Width(100f));
        timeScale = EditorGUILayout.FloatField(timeScale, GUILayout.Width(50f));
        timeScale = Mathf.Clamp(timeScale, minTimeScale, maxTimeScale);
        EditorPrefs.SetFloat(EDITOR_KEY_TIMESCALE, timeScale);

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();

        if(Application.isPlaying)
        {
            Time.timeScale = timeScale;
        }
    }
}
