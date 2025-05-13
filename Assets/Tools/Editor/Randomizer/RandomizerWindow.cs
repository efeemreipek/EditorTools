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
    private int positionBoundIndex = 0;
    private float boundLength;
    private bool preserveOriginalY;
    private bool showGizmos;
    private bool doScale;
    private float scaleLimitMin = 0.5f;
    private float scaleLimitMax = 2f;
    private int decimalPlacesIndex = 0;
    private bool doRotation;

    private const string EDITOR_KEY_SEED = "RAND_SEED";
    private const string EDITOR_KEY_BOUND_LENGTH = "RAND_BOUND_LENGTH";
    private const string EDITOR_KEY_PRESERVE_ORIGINAL_Y = "RAND_PRESERVE_ORG_Y";
    private const string EDITOR_KEY_SCALE_MIN = "RAND_SCALE_MIN";
    private const string EDITOR_KEY_SCALE_MAX = "RAND_SCALE_MAX";
    private const string EDITOR_KEY_DECIMAL_INDEX = "RAND_DECIMAL_INDEX";

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;

        seed = EditorPrefs.GetInt(EDITOR_KEY_SEED);
        boundLength = EditorPrefs.GetFloat(EDITOR_KEY_BOUND_LENGTH);
        preserveOriginalY = EditorPrefs.GetBool(EDITOR_KEY_PRESERVE_ORIGINAL_Y);
        scaleLimitMin = EditorPrefs.GetFloat(EDITOR_KEY_SCALE_MIN);
        scaleLimitMax = EditorPrefs.GetFloat(EDITOR_KEY_SCALE_MAX);
        decimalPlacesIndex = EditorPrefs.GetInt(EDITOR_KEY_DECIMAL_INDEX);
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;

        EditorPrefs.SetInt(EDITOR_KEY_SEED, seed);
        EditorPrefs.SetFloat(EDITOR_KEY_BOUND_LENGTH, boundLength);
        EditorPrefs.SetBool(EDITOR_KEY_PRESERVE_ORIGINAL_Y, preserveOriginalY);
        EditorPrefs.SetFloat(EDITOR_KEY_SCALE_MIN, scaleLimitMin);
        EditorPrefs.SetFloat(EDITOR_KEY_SCALE_MAX, scaleLimitMax);
        EditorPrefs.SetInt(EDITOR_KEY_DECIMAL_INDEX, decimalPlacesIndex);
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
            switch(positionBoundIndex)
            {
                case 0:
                    DrawWireCircle();
                    break;
                case 1:
                    DrawWireSquare();
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

            int newPositionBoundIndex = EditorGUILayout.Popup("Bound Style", positionBoundIndex, new string[] { "Circle", "Square" });
            if(newPositionBoundIndex != positionBoundIndex)
            {
                positionBoundIndex = newPositionBoundIndex;
            }
            boundLength = EditorGUILayout.FloatField(positionBoundIndex == 0 ? "Bound Radius" : "Bound Size", boundLength);
            preserveOriginalY = EditorGUILayout.Toggle("Preserve Original Y", preserveOriginalY);
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
                if(preserveOriginalY)
                {
                    Vector2 rnd2D = Random.insideUnitCircle * boundLength;
                    offset = new Vector3(rnd2D.x, 0f, rnd2D.y);
                }
                else
                {
                    Vector3 rnd3D = Random.insideUnitSphere * boundLength;
                    offset = rnd3D;
                }
            }
            else // Square
            {
                if(preserveOriginalY)
                {
                    offset = new Vector3(Random.Range(-boundLength, boundLength), 0f, Random.Range(-boundLength, boundLength));
                }
                else
                {
                    offset = new Vector3(Random.Range(-boundLength, boundLength), Random.Range(-boundLength, boundLength), Random.Range(-boundLength, boundLength));
                }
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
            if(preserveOriginalY)
            {
                newPosition.y = obj.transform.position.y;
            }
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
    private void DrawWireCircle()
    {
        Vector3 centerPoint = FindCenterOfGameObjects(Selection.gameObjects);

        if(preserveOriginalY)
        {
            Handles.DrawWireDisc(centerPoint, Vector3.up, boundLength, 3f);
        }
        else
        {
            Handles.DrawWireDisc(centerPoint, Vector3.up, boundLength, 3f);
            Handles.DrawWireDisc(centerPoint, Vector3.right, boundLength, 3f);
            Handles.DrawWireDisc(centerPoint, Vector3.forward, boundLength, 3f);
        }
    }
    private void DrawWireSquare()
    {
        Vector3 centerPoint = FindCenterOfGameObjects(Selection.gameObjects);

        if(preserveOriginalY)
        {
            Vector3 squareVertex0 = centerPoint + new Vector3(-boundLength, 0f, boundLength);
            Vector3 squareVertex1 = centerPoint + new Vector3(boundLength, 0f, boundLength);
            Vector3 squareVertex2 = centerPoint + new Vector3(boundLength, 0f, -boundLength);
            Vector3 squareVertex3 = centerPoint + new Vector3(-boundLength, 0f, -boundLength);

            Handles.DrawLine(squareVertex0, squareVertex1, 3f);
            Handles.DrawLine(squareVertex1, squareVertex2, 3f);
            Handles.DrawLine(squareVertex2, squareVertex3, 3f);
            Handles.DrawLine(squareVertex3, squareVertex0, 3f);
        }
        else
        {
            Vector3 cubeVertex0 = centerPoint + new Vector3(-boundLength, -boundLength, boundLength);
            Vector3 cubeVertex1 = centerPoint + new Vector3(boundLength, -boundLength, boundLength);
            Vector3 cubeVertex2 = centerPoint + new Vector3(boundLength, -boundLength, -boundLength);
            Vector3 cubeVertex3 = centerPoint + new Vector3(-boundLength, -boundLength, -boundLength);
            Vector3 cubeVertex4 = centerPoint + new Vector3(-boundLength, boundLength, boundLength);
            Vector3 cubeVertex5 = centerPoint + new Vector3(boundLength, boundLength, boundLength);
            Vector3 cubeVertex6 = centerPoint + new Vector3(boundLength, boundLength, -boundLength);
            Vector3 cubeVertex7 = centerPoint + new Vector3(-boundLength, boundLength, -boundLength);

            Handles.DrawLine(cubeVertex0, cubeVertex1, 3f);
            Handles.DrawLine(cubeVertex1, cubeVertex2, 3f);
            Handles.DrawLine(cubeVertex2, cubeVertex3, 3f);
            Handles.DrawLine(cubeVertex3, cubeVertex0, 3f);

            Handles.DrawLine(cubeVertex4, cubeVertex5, 3f);
            Handles.DrawLine(cubeVertex5, cubeVertex6, 3f);
            Handles.DrawLine(cubeVertex6, cubeVertex7, 3f);
            Handles.DrawLine(cubeVertex7, cubeVertex4, 3f);

            Handles.DrawLine(cubeVertex0, cubeVertex4, 3f);
            Handles.DrawLine(cubeVertex1, cubeVertex5, 3f);
            Handles.DrawLine(cubeVertex2, cubeVertex6, 3f);
            Handles.DrawLine(cubeVertex3, cubeVertex7, 3f);
        }
    }
}
