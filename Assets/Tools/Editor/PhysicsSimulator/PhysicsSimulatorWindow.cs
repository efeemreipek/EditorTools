using System;
using UnityEditor;
using UnityEngine;

public class PhysicsSimulatorWindow : EditorWindow
{
    private static class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float BUTTON_HEIGHT = 40f;
        public const float SEC_LABEL_WIDTH = 30f;
        public const float PADDING = 5f;
        public const float MARGIN = 5f;
    }

    [MenuItem("Tools/Physics Simulator")]
    public static void ShowWindow() => GetWindow<PhysicsSimulatorWindow>("Physics Simulator");

    private float simulationLength;

    private Rect boxRect;
    private Rect fieldRect;
    private Rect secLabelRect;
    private Rect buttonRect;

    private void OnGUI()
    {
        minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);

        CalculateRects();
        DrawHeader();
        DrawButton();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }
    private void CalculateRects()
    {
        buttonRect = new Rect(
            Layout.PADDING,
            boxRect.height + Layout.PADDING,
            boxRect.width,
            Layout.BUTTON_HEIGHT);

        boxRect = new Rect(
            Layout.PADDING,
            Layout.PADDING,
            position.width - Layout.PADDING * 2f,
            EditorGUIUtility.singleLineHeight + Layout.PADDING * 2f
            );

        fieldRect = new Rect(
            Layout.PADDING,
            Layout.PADDING * 2f,
            position.width - Layout.SEC_LABEL_WIDTH,
            EditorGUIUtility.singleLineHeight);

        secLabelRect = new Rect(
            position.width - Layout.SEC_LABEL_WIDTH,
            Layout.PADDING * 2f,
            Layout.SEC_LABEL_WIDTH,
            EditorGUIUtility.singleLineHeight);
    }
    private void DrawHeader()
    {
        GUI.Box(boxRect, GUIContent.none);
        simulationLength = Mathf.Max(0f, EditorGUI.FloatField(fieldRect, "Simulation Length", simulationLength));
        GUI.Label(secLabelRect, "sec", EditorStyles.centeredGreyMiniLabel);
    }
    private void DrawButton()
    {
        if(GUI.Button(buttonRect, "Simulate"))
        {
            if(simulationLength == 0)
            {
                EditorUtility.DisplayDialog("Invalid Value!", "Simulation length must be positive.", "OK");
                return;
            }

            if(Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selected GameObjects!", "There must be at least one gameobject for simulation", "OK");
                return;
            }
        }
    }
}
