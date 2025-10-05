using System;
using UnityEditor;
using UnityEngine;

public class PivotChangerWindow : EditorWindow
{
    private Vector3 handlePosition = Vector3.zero;
    private GameObject targetObject;
    private GameObject lastTargetObject;
    private bool resetCollider;

    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;

    [MenuItem("Tools/Pivot Changer")]
    private static void OpenWindow()
    {
        var window = GetWindow<PivotChangerWindow>("Pivot Changer");
        window.minSize = new Vector2(250, 95);
    }

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

    private void OnGUI()
    {
        EnsureStyles();

        EditorGUILayout.Space();

        GUILayout.Label("Select a GameObject with a MeshFilter.", labelStyle);

        targetObject = Selection.activeGameObject;

        if(targetObject == null)
        {
            EditorGUILayout.HelpBox("No GameObject selected.", MessageType.Info);
            return;
        }

        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        if(meshFilter == null || meshFilter.sharedMesh == null)
        {
            EditorGUILayout.HelpBox("Selected GameObject must have a MeshFilter with a mesh assigned.", MessageType.Warning);
            return;
        }

        if(targetObject != lastTargetObject)
        {
            lastTargetObject = targetObject;
            handlePosition = targetObject.transform.position + Vector3.down;
        }

        EditorGUILayout.Space();

        resetCollider = EditorGUILayout.ToggleLeft("Reset Collider", resetCollider);

        if(GUILayout.Button("Apply Pivot Change", buttonStyle, GUILayout.Height(40f)) && Event.current.button == 0)
        {
            ApplyPivotChange();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if(targetObject == null)
        {
            return;
        }

        handlePosition = Handles.PositionHandle(handlePosition, Quaternion.identity);

        Handles.Label(handlePosition, "New Pivot");
    }

    private void ApplyPivotChange()
    {
        if(targetObject == null) return;

        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        if(meshFilter == null || meshFilter.sharedMesh == null) return;

        Undo.RecordObject(meshFilter, "Change Mesh Pivot");
        Undo.RecordObject(targetObject.transform, "Adjust Transform Position");

        Collider originalCollider = targetObject.GetComponent<Collider>();
        Mesh originalMesh = meshFilter.sharedMesh;
        Mesh newMesh = Instantiate(originalMesh);
        newMesh.name = originalMesh.name + "_Modified";

        Vector3[] vertices = newMesh.vertices;
        Vector3[] normals = newMesh.normals;
        Vector4[] tangents = newMesh.tangents;

        Vector3 newPivotLocal = targetObject.transform.InverseTransformPoint(handlePosition);

        for(int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= newPivotLocal;
        }

        newMesh.vertices = vertices;

        if(normals != null && normals.Length == vertices.Length)
        {
            newMesh.normals = normals;
        }
        else
        {
            newMesh.RecalculateNormals();
        }

        if(tangents != null && tangents.Length == vertices.Length)
        {
            newMesh.tangents = tangents;
        }
        else
        {
            newMesh.RecalculateTangents();
        }

        newMesh.RecalculateBounds();

        string folderPath = "Assets/ModifiedMeshes";
        if(!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "ModifiedMeshes");
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + newMesh.name + ".mesh");

        AssetDatabase.CreateAsset(newMesh, assetPath);
        AssetDatabase.SaveAssets();

        meshFilter.sharedMesh = newMesh;

        targetObject.transform.position = handlePosition;

        handlePosition = targetObject.transform.position;

        if(resetCollider)
        {
            if(originalCollider != null)
            {
                Type colliderType = originalCollider.GetType();
                bool isTrigger = originalCollider.isTrigger;
                PhysicsMaterial physicsMaterial = originalCollider.sharedMaterial;
                Undo.DestroyObjectImmediate(originalCollider);
                Collider newCollider = Undo.AddComponent(targetObject, colliderType) as Collider;
                newCollider.isTrigger = isTrigger;
                newCollider.sharedMaterial = physicsMaterial;
            }
        }
    }

    private void OnSelectionChanged()
    {
        Repaint();
    }

    private void EnsureStyles()
    {
        labelStyle ??= GetStyle(null, TextAnchor.MiddleCenter, 12, FontStyle.Bold, Color.white);
        buttonStyle ??= GetStyle(GUI.skin.button, TextAnchor.MiddleCenter, 16, FontStyle.Bold, Color.white);
    }

    private GUIStyle GetStyle(GUIStyle guiStyle, TextAnchor alignment, int fontSize, FontStyle fontStyle, Color color)
    {
        GUIStyle style = guiStyle != null ? new GUIStyle(guiStyle) : new GUIStyle();
        style.alignment = alignment;
        if(fontSize != -1) style.fontSize = fontSize;
        style.fontStyle = fontStyle;
        style.normal.textColor = color;
        return style;
    }
}
