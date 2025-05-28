using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ScriptableObjectCreatorWindow : EditorWindow
{
    private static class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float TOGGLE_WIDTH = 100f;
        public const float BUTTON_HEIGHT = 25f;
        public const float ROW_SPACE = 5f;
    }

    [MenuItem("Tools/Scriptable Object Creator")]
    public static void ShowWindow()
    {
        GetWindow<ScriptableObjectCreatorWindow>("SO Creator");
    }

    private List<Type> scriptableObjectTypes;
    private bool manualSave;
    private Vector2 scrollPos;
    private readonly string[] foldersToExclude = new[] { "/Plugins/", "/Tools/" }; 

    private const string EDITOR_KEY_MANUAL_SAVE = "SO_CREATOR_MANUAL_SAVE";

    private void OnEnable()
    {
        scriptableObjectTypes = FindAllScriptableObjectTypesInAssets();

        manualSave = EditorPrefs.GetBool(EDITOR_KEY_MANUAL_SAVE);
    }
    private void OnDisable()
    {
        EditorPrefs.SetBool(EDITOR_KEY_MANUAL_SAVE, manualSave);
    }

    private void OnGUI()
    {
        minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);

        EditorGUILayout.BeginHorizontal("Box");
        DrawHeader();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical("Box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawButtons();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    private void CreateAndSaveScriptableObject(Type type)
    {
        ScriptableObject asset = CreateInstance(type);

        if(manualSave)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save ScriptableObject", type.Name, "asset", "Save your ScriptableObject");

            if(!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }
        else
        {
            string baseFolder = $"Assets/ScriptableObjects";
            string folderPath = $"{baseFolder}/{type.Name}";

            if(!AssetDatabase.IsValidFolder(baseFolder))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if(!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(baseFolder, type.Name);
            }

            string baseAssetPath = $"{folderPath}/{type.Name}.asset";
            string finalAssetPath = baseAssetPath;

            int counter = 1;

            while(AssetDatabase.LoadAssetAtPath<ScriptableObject>(finalAssetPath) != null)
            {
                finalAssetPath = $"{folderPath}/{type.Name}_{counter}.asset";
                counter++;
            }

            AssetDatabase.CreateAsset(asset, finalAssetPath);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
    private List<Type> FindAllScriptableObjectTypesInAssets()
    {
        var guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
        var soTypes = new List<Type>();

        foreach(string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if(foldersToExclude.Any(folder => path.Contains(folder))) continue;

            MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if(monoScript == null) continue;

            Type scriptClass = monoScript.GetClass();
            if(scriptClass == null) continue;

            if(typeof(EditorWindow).IsAssignableFrom(scriptClass)) continue;

            if(scriptClass.IsSubclassOf(typeof(ScriptableObject)) && !scriptClass.IsAbstract)
            {
                soTypes.Add(scriptClass);
            }
        }

        return soTypes.OrderBy(t => t.Name).ToList();
    }
    private void DrawHeader()
    {
        float labelWidth = position.width - Layout.TOGGLE_WIDTH - 20f;

        GUILayout.Label("Create Scriptable Objects", EditorStyles.boldLabel, GUILayout.Width(labelWidth > 0 ? labelWidth : 0));
        GUILayout.FlexibleSpace();
        GUIContent toggleContent = new GUIContent(
            "Manual Save",
            "If checked, you'll manually choose where to save the ScriptableObject. If unchecked, it will auto-save under ScriptableObjects/[TypeName]/.");

        manualSave = EditorGUILayout.ToggleLeft(toggleContent, manualSave, EditorStyles.boldLabel, GUILayout.Width(Layout.TOGGLE_WIDTH));
    }
    private void DrawButtons()
    {
        foreach(var type in scriptableObjectTypes)
        {
            if(GUILayout.Button($"Create {type.Name}", GUILayout.Height(Layout.BUTTON_HEIGHT)))
            {
                CreateAndSaveScriptableObject(type);
            }
        }
    }
}
