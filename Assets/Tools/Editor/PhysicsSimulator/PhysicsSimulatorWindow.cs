using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PhysicsSimulatorWindow : EditorWindow
{
    private class SimulationData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool HasRigidbody;
        public bool WasKinematic;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
    }
    private static class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float BUTTON_HEIGHT = 40f;
        public const float SPACE = 5f;
        public const float TOGGLE_WIDTH = 16f;
    }
    private enum ColliderType
    {
        BoxCollider,
        SphereCollider
    }

    [MenuItem("Tools/Physics Simulator")]
    public static void ShowWindow() => GetWindow<PhysicsSimulatorWindow>("Physics Simulator");

    private float simulationLength;
    private float simulationTimer;
    private bool isSimulating;
    private Dictionary<GameObject, SimulationData> originalState = new Dictionary<GameObject, SimulationData>();
    private float accumulatedTime = 0f;
    private bool isStylesInitDone;
    private bool isPaused;
    private ColliderType colliderType;
    private bool respectPhysicsHierarchy;

    private GUIStyle buttonStyle;
    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);

    private const string EDITOR_KEY_SIM_LENGTH = "PHYS_SIM_LENGTH";
    private const string EDITOR_KEY_COL_TYPE = "PHYS_SIM_COL";
    private const string EDITOR_KEY_RESPECT_HIERARCHY = "PHYS_SIM_RESPECT_HIERARCHY";

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;

        simulationLength = EditorPrefs.GetFloat(EDITOR_KEY_SIM_LENGTH);
        colliderType = (ColliderType)EditorPrefs.GetInt(EDITOR_KEY_COL_TYPE);
        respectPhysicsHierarchy = EditorPrefs.GetBool(EDITOR_KEY_RESPECT_HIERARCHY);
    }
    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;

        if(isSimulating)
        {
            Physics.simulationMode = SimulationMode.FixedUpdate;
            RestoreOriginalState();
        }

        EditorPrefs.SetFloat(EDITOR_KEY_SIM_LENGTH, simulationLength);
        EditorPrefs.SetInt(EDITOR_KEY_COL_TYPE, (int)colliderType);
        EditorPrefs.SetBool(EDITOR_KEY_RESPECT_HIERARCHY, respectPhysicsHierarchy);
    }
    private void OnGUI()
    {
        if(!isStylesInitDone) InitializeStyles();

        minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);

        EditorGUILayout.BeginVertical();
        DrawHeader();
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);
        DrawButton();

        if(isSimulating)
        {
            GUILayout.Space(Layout.SPACE);

            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(),
                simulationTimer / simulationLength,
                $"Simulating: {simulationTimer:F2}s / {simulationLength:F2}s"
            );
        }

        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);
        EditorGUI.BeginDisabledGroup(isSimulating);

        simulationLength = Mathf.Max(0f, EditorGUILayout.FloatField("Simulation Length", simulationLength, GUILayout.ExpandWidth(true)));
        GUILayout.Space(Layout.SPACE);
        colliderType = (ColliderType)EditorGUILayout.EnumPopup("Fallback Collider Type", colliderType);
        GUILayout.Space(Layout.SPACE);

        EditorGUILayout.BeginHorizontal();
        float labelWidth = EditorGUIUtility.currentViewWidth - Layout.TOGGLE_WIDTH - Layout.SPACE * 4f;
        GUIContent toggleLabel = new GUIContent(
            "Respect Physics Hierarchy",
            "When enabled, parent objects won't get physics components if their children already have them");
        EditorGUILayout.LabelField(toggleLabel, GUILayout.Width(labelWidth));
        respectPhysicsHierarchy = EditorGUILayout.Toggle("", respectPhysicsHierarchy, GUILayout.Width(Layout.TOGGLE_WIDTH));
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();
    }
    private void DrawButton()
    {
        GUI.color = buttonColor;

        EditorGUILayout.BeginHorizontal();

        float buttonWidth = EditorGUIUtility.currentViewWidth * 0.5f - Layout.SPACE * 2f;

        if(isSimulating)
        {
            if(GUILayout.Button(isPaused ? "RESUME" : "PAUSE", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(Layout.BUTTON_HEIGHT)) && Event.current.button == 0)
            {
                isPaused = !isPaused;
            }

            if(GUILayout.Button("STOP", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(Layout.BUTTON_HEIGHT)) && Event.current.button == 0)
            {
                StopSimulation();
            }
        }
        else
        {
            if(GUILayout.Button("SIMULATE", buttonStyle, GUILayout.Height(Layout.BUTTON_HEIGHT)) && Event.current.button == 0)
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
        }

        EditorGUILayout.EndHorizontal();
        GUI.color = Color.white;
    }
    private void InitializeStyles()
    {
        isStylesInitDone = true;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 14;
        buttonStyle.normal.textColor = Color.white;
    }
    private void HandleSimulation()
    {
        StoreOriginalState();
        PrepareRigidbodies();

        simulationTimer = 0f;
        accumulatedTime = 0f;
        isSimulating = true;

        Physics.simulationMode = SimulationMode.Script;

        EditorApplication.QueuePlayerLoopUpdate();
    }
    private void OnEditorUpdate()
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
    private void StopSimulation()
    {
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
            Velocity = Vector3.zero,
            AngularVelocity = Vector3.zero
        };

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if(rb != null)
        {
            data.WasKinematic = rb.isKinematic;
            data.Velocity = rb.linearVelocity;
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
                switch(colliderType)
                {
                    case ColliderType.BoxCollider:
                        Undo.AddComponent<BoxCollider>(go);
                        break;
                    case ColliderType.SphereCollider:
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
                    rb.linearVelocity = data.Velocity;
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