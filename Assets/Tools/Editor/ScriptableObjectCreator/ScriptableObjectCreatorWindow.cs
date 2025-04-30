using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ScriptableObjectCreatorWindow : EditorWindow
{
    [MenuItem("Tools/Scriptable Object Creator")]
    public static void ShowWindow()
    {
        GetWindow<ScriptableObjectCreatorWindow>("SO Creator");
    }

    private List<Type> scriptableObjectTypes;
    private bool manualSave;

    private void OnEnable()
    {
        scriptableObjectTypes = FindAllScriptableObjectTypesInAssets();
    }

    private void OnGUI()
    {
        minSize = new Vector2(300, 300);

        float toggleWidth = 100f;
        float labelWidth = position.width - toggleWidth - 20f;

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Create Scriptable Objects", EditorStyles.boldLabel, GUILayout.Width(labelWidth > 0 ? labelWidth : 0));
        GUILayout.FlexibleSpace();
        GUIContent toggleContent = new GUIContent(
            "Manual Save",
            "If checked, you'll manually choose where to save the ScriptableObject. If unchecked, it will auto-save under ScriptableObjects/[TypeName]/.");
        manualSave = EditorGUILayout.ToggleLeft(toggleContent, manualSave, EditorStyles.boldLabel, GUILayout.Width(toggleWidth));

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        foreach(var type in scriptableObjectTypes)
        {
            if(GUILayout.Button($"Create {type.Name}"))
            {
                CreateAndSaveScriptableObject(type);
            }
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
}
