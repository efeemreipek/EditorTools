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
    private bool doPosition;
    private bool showGizmos;
    private int positionBoundIndex = 0;
    private float boundLength;
    private bool doScale;
    private float scaleLimitMin = 0.5f;
    private float scaleLimitMax = 2f;
    private int decimalPlacesIndex = 0;
    private bool doRotation;

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;
    }
    private void OnSelectionChanged()
    {
        Repaint();
    }
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

        if(Selection.gameObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("There are no selected gameobjects", MessageType.Warning);
        }
        GUI.enabled = Selection.gameObjects.Length > 0;

        EditorGUILayout.BeginVertical("Box");
        DrawPosition();
        GUILayout.Space(Layout.SPACE);
        DrawScale();
        GUILayout.Space(Layout.SPACE);
        DrawRotation();
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();

        GUI.enabled = true;

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }
    private void OnSceneGUI(SceneView sceneView)
    {
        if(showGizmos && Selection.gameObjects.Length > 0)
        {
            Vector3 centerPoint = FindCenterOfGameObjects(Selection.gameObjects);

            switch(positionBoundIndex)
            {
                case 0:
                    Handles.DrawWireDisc(centerPoint + Vector3.zero * 0.05f, Vector3.up, boundLength, 3f);
                    break;
                case 1:
                    Vector3 squareVertex0 = centerPoint + new Vector3(-boundLength, 0f, boundLength);
                    Vector3 squareVertex1 = centerPoint + new Vector3(boundLength, 0f, boundLength);
                    Vector3 squareVertex2 = centerPoint + new Vector3(boundLength, 0f, -boundLength);
                    Vector3 squareVertex3 = centerPoint + new Vector3(-boundLength, 0f, -boundLength);

                    //Handles.DrawPolyLine(squareVertex0, squareVertex1, squareVertex2, squareVertex3, squareVertex0);
                    Handles.DrawLine(squareVertex0, squareVertex1, 3f);
                    Handles.DrawLine(squareVertex1, squareVertex2, 3f);
                    Handles.DrawLine(squareVertex2, squareVertex3, 3f);
                    Handles.DrawLine(squareVertex3, squareVertex0, 3f);
                    break;
                default:
                    break;
            }

        }
    }
    private void DrawPosition()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        doPosition = EditorGUILayout.ToggleLeft("Position", doPosition);
        if(doPosition)
        {
            EditorGUI.indentLevel++;

            int newPositionBoundIndex = EditorGUILayout.Popup("Position Bound Style", positionBoundIndex, new string[] { "Circle", "Square" });
            if(newPositionBoundIndex != positionBoundIndex)
            {
                positionBoundIndex = newPositionBoundIndex;
            }
            boundLength = EditorGUILayout.FloatField(positionBoundIndex == 0 ? "Bound Radius" : "Bound Size", boundLength);
            showGizmos = EditorGUILayout.Toggle("Show Gizmos", showGizmos);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
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
    private Vector3 FindCenterOfGameObjects(GameObject[] gameObjects)
    {
        float totalX = 0f;
        float totalY = 0f;
        float totalZ = 0f;

        foreach(GameObject gameObject in gameObjects)
        {
            totalX += gameObject.transform.position.x;
            totalY += gameObject.transform.position.y;
            totalZ += gameObject.transform.position.z;
        }

        float centerX = totalX / gameObjects.Length;
        float centerY = totalY / gameObjects.Length;
        float centerZ = totalZ / gameObjects.Length;

        return gameObjects.Length > 0 ? new Vector3(centerX, centerY, centerZ) : Vector3.zero;
    }
    private void ApplyRandomChanges()
    {
        if(doPosition)
        {
            ApplyRandomPosition();
        }
        if(doScale)
        {
            ApplyRandomScale();
        }
        if(doRotation)
        {
            ApplyRandomRotation();
        }
    }
    private void ApplyRandomPosition()
    {
        GameObject[] objects = Selection.gameObjects;
        if(objects.Length == 0) return;

        Vector3 originalCenter = FindCenterOfGameObjects(objects);

        Vector3[] generatedOffsets = new Vector3[objects.Length];
        Vector3 generatedCenter = Vector3.zero;

        for(int i = 0; i < objects.Length; i++)
        {
            Vector3 offset;
            if(positionBoundIndex == 0) // Circle
            {
                Vector2 rnd2D = Random.insideUnitCircle * boundLength;
                offset = new Vector3(rnd2D.x, 0f, rnd2D.y);
            }
            else // Square
            {
                offset = new Vector3(Random.Range(-boundLength, boundLength), 0f, Random.Range(-boundLength, boundLength));
            }

            generatedOffsets[i] = offset;
            generatedCenter += offset;
        }

        generatedCenter = generatedCenter / objects.Length;
        for(int i = 0; i < generatedOffsets.Length; i++)
        {
            generatedOffsets[i] -= generatedCenter; // recenter around (0,0,0)
        }

        for(int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if(obj == null) continue;

            Undo.RecordObject(obj.transform, "Random Position");

            Vector3 newPosition = originalCenter + generatedOffsets[i];
            newPosition.y = obj.transform.position.y;
            obj.transform.position = newPosition;

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
}
