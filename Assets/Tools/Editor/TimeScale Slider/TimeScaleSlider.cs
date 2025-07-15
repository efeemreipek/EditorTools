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

    static TimeScaleSlider()
    {
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
    }

    private static void OnToolbarGUI()
    {
        GUILayout.Space(10);
        GUILayout.BeginHorizontal("Box");

        GUILayout.Label("TimeScale");
        timeScale = GUILayout.HorizontalSlider(timeScale, minTimeScale, maxTimeScale, GUILayout.Width(100f));
        timeScale = EditorGUILayout.FloatField(timeScale, GUILayout.Width(50f));
        timeScale = Mathf.Clamp(timeScale, minTimeScale, maxTimeScale);

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();

        if(Application.isPlaying)
        {
            Time.timeScale = timeScale;
        }
    }
}
