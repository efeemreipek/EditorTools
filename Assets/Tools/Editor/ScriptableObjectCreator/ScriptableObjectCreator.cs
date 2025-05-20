using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ScriptableObjectCreator : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Tools/UI Toolkit/ScriptableObjectCreator")]
    public static void ShowExample()
    {
        ScriptableObjectCreator wnd = GetWindow<ScriptableObjectCreator>();
        wnd.titleContent = new GUIContent("ScriptableObjectCreator");
        wnd.minSize = new Vector2(300, 300);
        wnd.maxSize = wnd.minSize;
    }

    private const string EDITOR_KEY_MANUAL_SAVE = "SO_CREATOR_MANUAL_SAVE";

    private Toggle manualSaveToggle;
    private ScrollView scrollView;

    private bool manualSaveEnabled;
    private List<Type> scriptableObjectTypes;

    public void CreateGUI()
    {
        VisualElement root = m_VisualTreeAsset.Instantiate();
        rootVisualElement.Add(root);

        manualSaveToggle = root.Q<Toggle>("manual-save-toggle");
        scrollView = root.Q<ScrollView>("scroll-view");

        manualSaveEnabled = EditorPrefs.GetBool(EDITOR_KEY_MANUAL_SAVE);
        manualSaveToggle.value = manualSaveEnabled;

        manualSaveToggle.RegisterValueChangedCallback(ManualSaveToggleChanged);

        scriptableObjectTypes = FindAllScriptableObjectTypesInAssets();
        GenerateButtons();
    }
    private void ManualSaveToggleChanged(ChangeEvent<bool> evt)
    {
        manualSaveEnabled = evt.newValue;
        EditorPrefs.SetBool(EDITOR_KEY_MANUAL_SAVE, manualSaveEnabled);
    }
    private void GenerateButtons()
    {
        scrollView.Clear();

        foreach(var type in scriptableObjectTypes)
        {
            var button = new Button(() => CreateAndSaveScriptableObject(type))
            {
                text = $"Create {type.Name}"
            };
            button.AddToClassList("so-create-button");
            scrollView.Add(button);
        }
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
            if(scriptClass == null || typeof(EditorWindow).IsAssignableFrom(scriptClass)) continue;

            if(scriptClass.IsSubclassOf(typeof(ScriptableObject)) && !scriptClass.IsAbstract)
            {
                soTypes.Add(scriptClass);
            }
        }

        return soTypes.OrderBy(t => t.Name).ToList();
    }
    private void CreateAndSaveScriptableObject(Type type)
    {
        ScriptableObject asset = CreateInstance(type);

        if(manualSaveEnabled)
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
}
