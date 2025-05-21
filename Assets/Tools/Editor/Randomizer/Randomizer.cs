using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Randomizer : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Tools/UI Toolkit/Randomizer")]
    public static void ShowExample()
    {
        Randomizer wnd = GetWindow<Randomizer>();
        wnd.titleContent = new GUIContent("Randomizer");
        wnd.minSize = new Vector2(300, 400);
    }

    private const string EDITOR_KEY_SEED = "RAND_SEED";
    private const string EDITOR_KEY_BOUND_LENGTH = "RAND_BOUND_LENGTH";
    private const string EDITOR_KEY_PRESERVE_ORIGINAL_Y = "RAND_PRESERVE_ORG_Y";
    private const string EDITOR_KEY_SHOW_GIZMOS = "RAND_SHOW_GIZMOS";
    private const string EDITOR_KEY_SCALE_MIN = "RAND_SCALE_MIN";
    private const string EDITOR_KEY_SCALE_MAX = "RAND_SCALE_MAX";
    private const string EDITOR_KEY_DECIMAL_PLACE = "RAND_DECIMAL_PLACE";

    private SliderInt seedSlider;
    private Toggle positionToggle;
    private Toggle scaleToggle;
    private Toggle rotationToggle;
    private VisualElement helpBoxContainer;
    private HelpBox helpBox;
    private GroupBox containers;
    private GroupBox positionGroup;
    private GroupBox scaleGroup;
    private DropdownField boundStyleDropdown;
    private FloatField boundLengthFloat;
    private Toggle preserveOriginalYToggle;
    private Toggle showGizmosToggle;
    private FloatField minLimitFloat;
    private FloatField maxLimitFloat;
    private DropdownField decimalPlacesDropdown;

    private int seed;
    private bool positionToggleEnabled;
    private bool scaleToggleEnabled;
    private bool rotationToggleEnabled;
    private string boundStyle;
    private float boundLength;
    private bool preserveOriginalY;
    private bool showGizmos;
    private float minLimit;
    private float maxLimit;
    private string decimalPlaces;

    public void CreateGUI()
    {
        VisualElement root = m_VisualTreeAsset.Instantiate();
        rootVisualElement.Add(root);

        seedSlider = root.Q<SliderInt>("seed-slider");
        positionToggle = root.Q<Toggle>("position-toggle");
        scaleToggle = root.Q<Toggle>("scale-toggle");
        rotationToggle = root.Q<Toggle>("rotation-toggle");
        helpBoxContainer = root.Q<VisualElement>("help-box-container");
        containers = root.Q<GroupBox>("containers");
        positionGroup = root.Q<GroupBox>("position-group");
        scaleGroup = root.Q<GroupBox>("scale-group");
        boundStyleDropdown = root.Q<DropdownField>("bound-style-dropdown");
        boundLengthFloat = root.Q<FloatField>("bound-length-float");
        preserveOriginalYToggle = root.Q<Toggle>("preserve-original-y-toggle");
        showGizmosToggle = root.Q<Toggle>("show-gizmos-toggle");
        minLimitFloat = root.Q<FloatField>("min-limit");
        maxLimitFloat = root.Q<FloatField>("max-limit");
        decimalPlacesDropdown = root.Q<DropdownField>("decimal-places-dropdown");

        positionGroup.style.display = positionToggleEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        scaleGroup.style.display = scaleToggleEnabled ? DisplayStyle.Flex : DisplayStyle.None;

        seedSlider.RegisterValueChangedCallback(SeedSliderChanged);
        positionToggle.RegisterValueChangedCallback(PositionToggleChanged);
        scaleToggle.RegisterValueChangedCallback(ScaleToggleChanged);
        rotationToggle.RegisterValueChangedCallback(RotationToggleChanged);
        boundStyleDropdown.RegisterValueChangedCallback(BoundStyleChanged);
        boundLengthFloat.RegisterValueChangedCallback(BoundLengthChanged);
        preserveOriginalYToggle.RegisterValueChangedCallback(PreserveOriginalYChanged);
        showGizmosToggle.RegisterValueChangedCallback(ShowGizmosChanged);
        minLimitFloat.RegisterValueChangedCallback(MinLimitChanged);
        maxLimitFloat.RegisterValueChangedCallback(MaxLimitChanged);
        decimalPlacesDropdown.RegisterValueChangedCallback(DecimalPlacesChanged);

        CreateHelpBox();

        seedSlider.value = seed;
        boundLengthFloat.value = boundLength;
        preserveOriginalYToggle.value = preserveOriginalY;
        showGizmosToggle.value = showGizmos;
        minLimitFloat.value = minLimit;
        maxLimitFloat.value = maxLimit;
        decimalPlacesDropdown.value = decimalPlaces;

        boundStyle = boundStyleDropdown.value;

        CheckSelection();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;

        seed = EditorPrefs.GetInt(EDITOR_KEY_SEED);
        boundLength = EditorPrefs.GetFloat(EDITOR_KEY_BOUND_LENGTH);
        preserveOriginalY = EditorPrefs.GetBool(EDITOR_KEY_PRESERVE_ORIGINAL_Y);
        showGizmos = EditorPrefs.GetBool(EDITOR_KEY_SHOW_GIZMOS);
        minLimit = EditorPrefs.GetFloat(EDITOR_KEY_SCALE_MIN);
        maxLimit = EditorPrefs.GetFloat(EDITOR_KEY_SCALE_MAX);
        decimalPlaces = EditorPrefs.GetString(EDITOR_KEY_DECIMAL_PLACE);
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;

        EditorPrefs.SetInt(EDITOR_KEY_SEED, seed);
        EditorPrefs.SetFloat(EDITOR_KEY_BOUND_LENGTH, boundLength);
        EditorPrefs.SetBool(EDITOR_KEY_PRESERVE_ORIGINAL_Y, preserveOriginalY);
        EditorPrefs.SetBool(EDITOR_KEY_SHOW_GIZMOS, showGizmos);
        EditorPrefs.SetFloat(EDITOR_KEY_SCALE_MIN, minLimit);
        EditorPrefs.SetFloat(EDITOR_KEY_SCALE_MAX, maxLimit);
        EditorPrefs.SetString(EDITOR_KEY_DECIMAL_PLACE, decimalPlaces);
    }
    private void OnSceneGUI(SceneView sceneView)
    {
        if(!showGizmos || Selection.gameObjects.Length <= 0 || !positionToggleEnabled)
            return;

        Handles.color = Color.green;
        if(boundStyle == "Circle")
        {
            DrawWireCircle();
        }
        else if(boundStyle == "Square")
        {
            DrawWireSquare();
        }
    }
    private void OnSelectionChanged()
    {
        CheckSelection();
    }
    private void CheckSelection()
    {
        bool isGameObjectSelected = Selection.gameObjects.Length != 0;

        helpBox.style.display = isGameObjectSelected ? DisplayStyle.None : DisplayStyle.Flex;
        containers.SetEnabled(isGameObjectSelected);
    }
    private void CreateHelpBox()
    {
        helpBox = new HelpBox("There are no selected gameobjects", HelpBoxMessageType.Warning);
        helpBox.style.display = DisplayStyle.None;
        helpBoxContainer.Add(helpBox);
    }
    private void SeedSliderChanged(ChangeEvent<int> evt)
    {
        int newSeed = evt.newValue;
        if(newSeed != seed)
        {
            seed = newSeed;
            Random.InitState(seed);
            ApplyRandomChanges();
        }
    }
    private void PositionToggleChanged(ChangeEvent<bool> evt)
    {
        positionToggleEnabled = evt.newValue;

        positionGroup.style.display = positionToggleEnabled ? DisplayStyle.Flex : DisplayStyle.None;
    }
    private void ScaleToggleChanged(ChangeEvent<bool> evt)
    {
        scaleToggleEnabled = evt.newValue;

        scaleGroup.style.display = scaleToggleEnabled ? DisplayStyle.Flex : DisplayStyle.None;
    }
    private void RotationToggleChanged(ChangeEvent<bool> evt)
    {
        rotationToggleEnabled = evt.newValue;
    }
    private void BoundStyleChanged(ChangeEvent<string> evt)
    {
        boundStyle = evt.newValue;
        if(boundStyle == "Circle")
        {
            boundLengthFloat.label = "Bound Radius";
        }
        else if(boundStyle == "Square")
        {
            boundLengthFloat.label = "Bound Length";
        }
    }
    private void BoundLengthChanged(ChangeEvent<float> evt)
    {
        boundLength = evt.newValue;
    }
    private void PreserveOriginalYChanged(ChangeEvent<bool> evt)
    {
        preserveOriginalY = evt.newValue;
    }
    private void ShowGizmosChanged(ChangeEvent<bool> evt)
    {
        showGizmos = evt.newValue;
    }
    private void MinLimitChanged(ChangeEvent<float> evt)
    {
        minLimit = evt.newValue;
    }
    private void MaxLimitChanged(ChangeEvent<float> evt)
    {
        maxLimit = evt.newValue;
    }
    private void DecimalPlacesChanged(ChangeEvent<string> evt)
    {
        decimalPlaces = evt.newValue;
    }

    private void ApplyRandomChanges()
    {
        if(positionToggleEnabled)
        {
            ApplyRandomPosition();
        }
        if(scaleToggleEnabled)
        {
            ApplyRandomScale();
        }
        if(rotationToggleEnabled)
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
            if(boundStyle == "Circle")
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

            float rawScale = Random.Range(minLimit, maxLimit);
            float decimalPlace;
            switch(decimalPlaces)
            {
                case "1":
                    decimalPlace = 10f;
                    break;
                case "2":
                    decimalPlace = 100f;
                    break;
                case "3":
                    decimalPlace = 1000f;
                    break;
                case "4":
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
    private Vector3 FindCenterOfGameObjects(GameObject[] gameObjects)
    {
        if(gameObjects.Length == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach(GameObject gameObject in gameObjects)
        {
            sum += gameObject.transform.position;
        }

        return sum / gameObjects.Length;
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
