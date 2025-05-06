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
        public const float SEC_LABEL_WIDTH = 30f;
        public const float PADDING = 5f;
        public const float MARGIN = 5f;
    }

    [MenuItem("Tools/Physics Simulator")]
    public static void ShowWindow() => GetWindow<PhysicsSimulatorWindow>("Physics Simulator");

    private float simulationLength;
    private float simulationTimer;
    private bool isSimulating;
    private Dictionary<GameObject, SimulationData> originalState = new Dictionary<GameObject, SimulationData>();
    private float accumulatedTime = 0f;

    private Rect boxRect;
    private Rect fieldRect;
    private Rect secLabelRect;
    private Rect buttonRect;

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }
    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;

        if(isSimulating)
        {
            Physics.simulationMode = SimulationMode.FixedUpdate;
            RestoreOriginalState();
        }
    }
    private void OnGUI()
    {
        minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);

        CalculateRects();
        DrawHeader();
        DrawButton();

        if(isSimulating)
        {
            EditorGUI.ProgressBar(
                new Rect(Layout.PADDING, buttonRect.yMax + Layout.PADDING, position.width - Layout.PADDING * 2f, 20f),
                simulationTimer / simulationLength,
                $"Simulating: {simulationTimer:F2}s / {simulationLength:F2}s"
            );
        }

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

        EditorGUI.BeginDisabledGroup(isSimulating);
        simulationLength = Mathf.Max(0f, EditorGUI.FloatField(fieldRect, "Simulation Length", simulationLength));
        EditorGUI.EndDisabledGroup();

        GUI.Label(secLabelRect, "sec", EditorStyles.centeredGreyMiniLabel);
    }
    private void DrawButton()
    {
        if(isSimulating)
        {
            if(GUI.Button(buttonRect, "Stop Simulation"))
            {
                StopSimulation();
            }
        }
        else
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
                    EditorUtility.DisplayDialog("No Selected GameObjects!", "There must be at least one gameobject for simulation.", "OK");
                    return;
                }

                HandleSimulation();
            }
        }
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

        if(simulationTimer < simulationLength)
        {
            float deltaTime = Time.deltaTime > 0 ? Time.deltaTime : 0.01f;
            simulationTimer += deltaTime;

            accumulatedTime += deltaTime;

            while(accumulatedTime >= Time.fixedDeltaTime)
            {
                Physics.Simulate(Time.fixedDeltaTime);
                accumulatedTime -= Time.fixedDeltaTime;
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

            // children
            foreach(Transform child in go.GetComponentsInChildren<Transform>())
            {
                if(child.gameObject == go) continue;

                rb = child.GetComponent<Rigidbody>();
                if(rb != null)
                {
                    SimulationData childData = new SimulationData
                    {
                        Position = child.position,
                        Rotation = child.rotation,
                        HasRigidbody = true,
                        WasKinematic = rb.isKinematic,
                        Velocity = rb.linearVelocity,
                        AngularVelocity = rb.angularVelocity
                    };

                    originalState.Add(child.gameObject, childData);
                }
            }
        }
    }
    private void PrepareRigidbodies()
    {
        foreach(GameObject go in Selection.gameObjects)
        {
            PrepareRigidbody(go);

            foreach(Transform child in go.GetComponentsInChildren<Transform>())
            {
                if(child.gameObject == go) continue;
                PrepareRigidbody(child.gameObject);
            }   
        }
    }
    private void PrepareRigidbody(GameObject go)
    {
        Rigidbody rb = go.GetComponent<Rigidbody>();

        if(rb == null)
        {
            rb = Undo.AddComponent<Rigidbody>(go);

            if(go.GetComponent<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(go);
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
