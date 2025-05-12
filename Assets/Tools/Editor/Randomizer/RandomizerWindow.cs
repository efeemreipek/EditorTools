using UnityEditor;
using UnityEngine;

public class RandomizerWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float SPACE = 5f;
    }

    [MenuItem("Tools/Randomizer")]
    public static void ShowWindow()
    {
        var win = GetWindow<RandomizerWindow>("Randomizer");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private int seed = 0;
    private bool doRotation;
    private bool doScale;
    private float scaleLimitMin = 0.5f;
    private float scaleLimitMax = 2f;
    private int decimalPlacesIndex = 0;

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal("Box");
        EditorGUILayout.LabelField("SEED", EditorStyles.boldLabel, GUILayout.Width(40f));
        int newSeed = EditorGUILayout.IntSlider(seed, 0, 100);
        if(newSeed != seed)
        {
            seed = newSeed;
            Random.InitState(seed);
            ApplyRandomChanges();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical("Box");
        DrawScale();
        GUILayout.Space(Layout.SPACE);
        DrawRotation();
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }
    private void DrawScale()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        doScale = EditorGUILayout.ToggleLeft("Scale", doScale);
        if(doScale)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Limits", GUILayout.Width(133f));

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = (position.width - 163f) * 0.5f;
            EditorGUIUtility.labelWidth = 40f;

            scaleLimitMin = EditorGUILayout.FloatField("Min", scaleLimitMin, GUILayout.Width(fieldWidth));
            scaleLimitMax = EditorGUILayout.FloatField("Max", scaleLimitMax, GUILayout.Width(fieldWidth));

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = originalLabelWidth;

            int newDecimalPlacesIndex = EditorGUILayout.Popup("Decimal Places", decimalPlacesIndex, new string[] { "1", "2", "3", "4" });
            if(newDecimalPlacesIndex != decimalPlacesIndex)
            {
                decimalPlacesIndex = newDecimalPlacesIndex;
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawRotation()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        doRotation = EditorGUILayout.ToggleLeft("Rotation", doRotation);
        EditorGUILayout.EndVertical();
    }
    private void ApplyRandomChanges()
    {
        if(doScale)
        {
            ApplyRandomScale();
        }
        if(doRotation)
        {
            ApplyRandomRotation();
        }
    }
    private void ApplyRandomRotation()
    {
        foreach(GameObject obj in Selection.gameObjects)
        {
            if(obj == null) return;

            Undo.RecordObject(obj.transform, "Random Rotation");

            Vector3 randomRotation = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
            obj.transform.rotation = Random.rotationUniform;

            EditorUtility.SetDirty(obj);
        }
    }
    private void ApplyRandomScale()
    {
        foreach(GameObject obj in Selection.gameObjects)
        {
            if(obj == null) return;

            Undo.RecordObject(obj.transform, "Random Scale");

            float rawScale = Random.Range(scaleLimitMin, scaleLimitMax);
            float decimalPlace;
            switch(decimalPlacesIndex)
            {
                case 0:
                    decimalPlace = 10f;
                    break;
                case 1:
                    decimalPlace = 100f;
                    break;
                case 2:
                    decimalPlace = 1000f;
                    break;
                case 3:
                    decimalPlace = 10000f;
                    break;
                default:
                    decimalPlace = 100f;
                    break;
            }
            float roundedScale = Mathf.Round(rawScale * decimalPlace) / decimalPlace;
            obj.transform.localScale = Vector3.one * roundedScale;

            EditorUtility.SetDirty(obj);
        }
    }
}
