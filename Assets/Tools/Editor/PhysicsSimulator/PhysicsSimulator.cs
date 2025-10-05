using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Tilemaps.Tile;

public class PhysicsSimulator : EditorWindow
{
    [Serializable]
    private class SimulationData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool HasRigidbody;
        public bool WasKinematic;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }

    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Tools/UI Toolkit/PhysicsSimulator")]
    public static void ShowExample()
    {
        PhysicsSimulator wnd = GetWindow<PhysicsSimulator>();
        wnd.titleContent = new GUIContent("PhysicsSimulator");
        wnd.minSize = new Vector2(300, 300);
    }

    private const string EDITOR_KEY_SIM_LENGTH = "PHYS_SIM_LENGTH";
    private const string EDITOR_KEY_COL_TYPE = "PHYS_SIM_COL";
    private const string EDITOR_KEY_RESPECT_HIERARCHY = "PHYS_SIM_RESPECT_HIERARCHY";

    private GroupBox controlGroup;
    private Slider simulationLengthSlider;
    private DropdownField fallbackColliderDropdown;
    private Toggle respectHierarchyToggle;
    private Button simulateButton;
    private Button pauseButton;
    private Button stopButton;
    private GroupBox simulationProgressGroup;
    private ProgressBar simulationProgressBar;

    private float simulationLength;
    private float simulationTimer;
    private float accumulatedTime;
    private bool isSimulating;
    private bool isPaused;
    private bool respectPhysicsHierarchy;
    private Dictionary<GameObject, SimulationData> originalState = new Dictionary<GameObject, SimulationData>();
    private string fallbackColliderType;

    public void CreateGUI()
    {
        VisualElement root = m_VisualTreeAsset.Instantiate();
        rootVisualElement.Add(root);

        controlGroup = root.Q<GroupBox>("control-group");
        simulationLengthSlider = root.Q<Slider>("simulation-length-slider");
        fallbackColliderDropdown = root.Q<DropdownField>("fallback-collider-dropdown");
        respectHierarchyToggle = root.Q<Toggle>("respect-hierarchy-toggle");
        simulateButton = root.Q<Button>("simulate-button");
        pauseButton = root.Q<Button>("pause-button");
        stopButton = root.Q<Button>("stop-button");
        simulationProgressGroup = root.Q<GroupBox>("simulation-progress-group");
        simulationProgressBar = root.Q<ProgressBar>("simulation-progress");

        simulationLengthSlider.RegisterValueChangedCallback(SimulationLengthChanged);
        fallbackColliderDropdown.RegisterValueChangedCallback(FallbackColliderChanged);
        respectHierarchyToggle.RegisterValueChangedCallback(RespectHierarchyChanged);

        simulateButton.clicked += SimulateButton_Clicked;
        pauseButton.clicked += PauseButton_Clicked;
        stopButton.clicked += StopButton_Clicked;

        simulationLengthSlider.value = simulationLength;
        fallbackColliderDropdown.value = fallbackColliderType;
        respectHierarchyToggle.value = respectPhysicsHierarchy;
    }
    private void OnEnable()
    {
        simulationLength = EditorPrefs.HasKey(EDITOR_KEY_SIM_LENGTH) ? EditorPrefs.GetFloat(EDITOR_KEY_SIM_LENGTH) : simulationLengthSlider.value;
        fallbackColliderType = EditorPrefs.GetString(EDITOR_KEY_COL_TYPE);
        respectPhysicsHierarchy = EditorPrefs.GetBool(EDITOR_KEY_RESPECT_HIERARCHY);
    }
    private void OnDisable()
    {
        EditorPrefs.SetFloat(EDITOR_KEY_SIM_LENGTH, simulationLength);
        EditorPrefs.SetString(EDITOR_KEY_COL_TYPE, fallbackColliderType);
        EditorPrefs.SetBool(EDITOR_KEY_RESPECT_HIERARCHY, respectPhysicsHierarchy);
    }
    private void Update()
    {
        if(!isSimulating) return;
        if(isPaused) return;

        if(simulationTimer < simulationLength)
        {
            float deltaTime = Time.deltaTime > 0 ? Time.deltaTime : 0.01f;
            simulationTimer += deltaTime;
            accumulatedTime += deltaTime;

            int maxStepsPerFrame = 60;
            int steps = 0;

            simulationProgressBar.value = (simulationTimer / simulationLength) * 100f;
            simulationProgressBar.title = $"Simulating: {simulationTimer:F2}s / {simulationLength:F2}s";

            while(accumulatedTime >= Time.fixedDeltaTime && steps < maxStepsPerFrame)
            {
                Physics.Simulate(Time.fixedDeltaTime);
                accumulatedTime -= Time.fixedDeltaTime;
                steps++;
            }

            Repaint();

            EditorApplication.QueuePlayerLoopUpdate();
        }
        else
        {
            StopSimulation();
        }
    }

    private void SimulationLengthChanged(ChangeEvent<float> evt) => simulationLength = evt.newValue;
    private void FallbackColliderChanged(ChangeEvent<string> evt) => fallbackColliderType = evt.newValue;
    private void RespectHierarchyChanged(ChangeEvent<bool> evt) => respectPhysicsHierarchy = evt.newValue;
    private void SimulateButton_Clicked()
    {
        if(simulationLength == 0)
        {
            EditorUtility.DisplayDialog("Invalid Value!", "Simulation length must be positive.", "OK");
            return;
        }

        if(Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selected GameObjects!", "There must be at least one gameobject for simulation.", "OK");
            return;
        }

        HandleSimulation();
    }
    private void PauseButton_Clicked()
    {
        isPaused = !isPaused;

        pauseButton.text = isPaused ? "RESUME" : "PAUSE";
    }
    private void StopButton_Clicked()
    {
        StopSimulation();
    }

    private void HandleSimulation()
    {
        simulateButton.style.display = DisplayStyle.None;
        pauseButton.style.display = DisplayStyle.Flex;
        stopButton.style.display = DisplayStyle.Flex;
        simulationProgressGroup.style.display = DisplayStyle.Flex;
        controlGroup.SetEnabled(false);

        StoreOriginalState();
        PrepareRigidbodies();

        simulationTimer = 0f;
        accumulatedTime = 0f;
        isSimulating = true;

        Physics.simulationMode = SimulationMode.Script;

        EditorApplication.QueuePlayerLoopUpdate();
    }
    private void StopSimulation()
    {
        simulateButton.style.display = DisplayStyle.Flex;
        pauseButton.style.display = DisplayStyle.None;
        stopButton.style.display = DisplayStyle.None;
        simulationProgressGroup.style.display = DisplayStyle.None;
        controlGroup.SetEnabled(true);

        isSimulating = false;
        isPaused = false;

        Physics.simulationMode = SimulationMode.FixedUpdate;

        bool keepSimulation = EditorUtility.DisplayDialog("Simulation Complete", "Do you want to keep the simulated state?", "Keep", "Revert");

        if(!keepSimulation)
        {
            RestoreOriginalState();
        }
        else
        {
            CleanupTemporaryComponents();
        }
    }
    private void StoreOriginalState()
    {
        originalState.Clear();

        foreach(GameObject go in Selection.gameObjects)
        {
            StoreObjectState(go);

            // children
            foreach(Transform child in go.GetComponentsInChildren<Transform>())
            {
                if(child.gameObject == go) continue;

                StoreObjectState(child.gameObject);
            }
        }
    }
    private void StoreObjectState(GameObject go)
    {
        if(originalState.ContainsKey(go)) return;

        SimulationData data = new SimulationData
        {
            Position = go.transform.position,
            Rotation = go.transform.rotation,
            HasRigidbody = go.GetComponent<Rigidbody>() != null,
            WasKinematic = false,
            LinearVelocity = Vector3.zero,
            AngularVelocity = Vector3.zero
        };

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if(rb != null)
        {
            data.WasKinematic = rb.isKinematic;
            data.LinearVelocity = rb.linearVelocity;
            data.AngularVelocity = rb.angularVelocity;
        }

        originalState.Add(go, data);
    }
    private void PrepareRigidbodies()
    {
        Dictionary<GameObject, bool> hasPhysicsChildren = new Dictionary<GameObject, bool>();

        if(respectPhysicsHierarchy)
        {
            foreach(GameObject go in Selection.gameObjects)
            {
                CollectPhysicsHierarchyInfo(go, hasPhysicsChildren);
            }
        }

        foreach(GameObject go in Selection.gameObjects)
        {
            PrepareRigidbody(go, hasPhysicsChildren);

            foreach(Transform child in go.GetComponentsInChildren<Transform>())
            {
                if(child.gameObject == go) continue;
                PrepareRigidbody(child.gameObject, hasPhysicsChildren);
            }
        }
    }
    private void CollectPhysicsHierarchyInfo(GameObject go, Dictionary<GameObject, bool> hasPhysicsChildren)
    {
        bool hasPhysicsComponents = go.GetComponent<Rigidbody>() != null || go.GetComponent<Collider>() != null;
        bool childrenHavePhysics = false;

        foreach(Transform child in go.transform)
        {
            CollectPhysicsHierarchyInfo(child.gameObject, hasPhysicsChildren);

            bool thisChildHasPhysics = child.GetComponent<Rigidbody>() != null ||
                                       child.GetComponent<Collider>() != null ||
                                       hasPhysicsChildren.ContainsKey(child.gameObject) && hasPhysicsChildren[child.gameObject];

            if(thisChildHasPhysics)
            {
                childrenHavePhysics = true;
            }
        }

        hasPhysicsChildren[go] = childrenHavePhysics;
    }
    private void PrepareRigidbody(GameObject go, Dictionary<GameObject, bool> hasPhysicsChildren)
    {
        Rigidbody rb = go.GetComponent<Rigidbody>();
        bool hasCollider = go.GetComponent<Collider>() != null;

        bool skipDueToHierarchy = respectPhysicsHierarchy &&
                                  hasPhysicsChildren.ContainsKey(go) &&
                                  hasPhysicsChildren[go] &&
                                  rb == null;

        if(skipDueToHierarchy) return;

        if(rb == null)
        {
            rb = Undo.AddComponent<Rigidbody>(go);

            if(!hasCollider)
            {
                switch(fallbackColliderType)
                {
                    case "Box Collider":
                        Undo.AddComponent<BoxCollider>(go);
                        break;
                    case "Sphere Collider":
                        Undo.AddComponent<SphereCollider>(go);
                        break;
                }
            }
        }
        else
        {
            if(rb.isKinematic)
            {
                Undo.RecordObject(rb, "Set Rigidbody Non-Kinematic");
                rb.isKinematic = false;
            }
        }
    }
    private void RestoreOriginalState()
    {
        foreach(var kvp in originalState)
        {
            GameObject go = kvp.Key;
            SimulationData data = kvp.Value;

            if(go == null) continue;

            Undo.RecordObject(go.transform, "Restore Transform");
            go.transform.position = data.Position;
            go.transform.rotation = data.Rotation;

            Rigidbody rb = go.GetComponent<Rigidbody>();
            if(rb != null)
            {
                if(!data.HasRigidbody)
                {
                    Undo.DestroyObjectImmediate(rb);

                    if(go.GetComponent<Collider>() != null && !originalState.ContainsKey(go))
                    {
                        Undo.DestroyObjectImmediate(go.GetComponent<Collider>());
                    }
                }
                else
                {
                    Undo.RecordObject(rb, "Restore Rigidbody");
                    rb.isKinematic = data.WasKinematic;
                    rb.linearVelocity = data.LinearVelocity;
                    rb.angularVelocity = data.AngularVelocity;
                }
            }
        }

        originalState.Clear();
    }
    private void CleanupTemporaryComponents()
    {
        foreach(var kvp in originalState)
        {
            GameObject go = kvp.Key;
            SimulationData data = kvp.Value;

            if(go == null) continue;

            if(!data.HasRigidbody)
            {
                Rigidbody rb = go.GetComponent<Rigidbody>();
                if(rb != null)
                {
                    Undo.DestroyObjectImmediate(rb);

                    Collider col = go.GetComponent<Collider>();
                    if(col != null && !originalState.ContainsKey(go))
                    {
                        Undo.DestroyObjectImmediate(col);
                    }
                }
            }
        }
        originalState.Clear();
    }
}
