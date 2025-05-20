using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class Scenes : EditorWindow
{
    [Serializable]
    private class SceneFolderGUIDData
    {
        public List<string> guids;
    }

    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Tools/UI Toolkit/Scenes")]
    public static void ShowExample()
    {
        Scenes wnd = GetWindow<Scenes>();
        wnd.titleContent = new GUIContent("Scenes");
        wnd.minSize = new Vector2(300, 400);
    }

    private const string EDITOR_KEY_SCENE_FOLDER_GUIDS = "SCENES_FOLDER_GUIDS";

    private Label headerLabel;
    private ScrollView scrollView;
    private Button addSceneButton;
    private Button clearButton;
    private Button viewButton;

    private List<string> sceneFolderGUIDs = new List<string>();
    private List<string> sceneAssetPaths = new List<string>();
    private bool viewToggle = false; // T:folders F:scenes

    public void CreateGUI()
    {
        VisualElement root = m_VisualTreeAsset.Instantiate();
        root.style.flexGrow = 1;
        rootVisualElement.Add(root);
        rootVisualElement.style.flexGrow = 1;

        headerLabel = root.Q<Label>("header-label");
        scrollView = root.Q<ScrollView>("scroll-view");
        addSceneButton = root.Q<Button>("add-scene-button");
        clearButton = root.Q<Button>("clear-button");
        viewButton = root.Q<Button>("view-button");

        addSceneButton.RegisterCallback<MouseUpEvent>(AddSceneFolders);
        clearButton.RegisterCallback<MouseUpEvent>(ClearFolders);
        viewButton.RegisterCallback<MouseUpEvent>(ViewToggle);

        LoadFolderPrefs();
        UpdateScrollView();
    }
    private void OnDisable()
    {
        addSceneButton.UnregisterCallback<MouseUpEvent>(AddSceneFolders);
        clearButton.UnregisterCallback<MouseUpEvent>(ClearFolders);
        viewButton.UnregisterCallback<MouseUpEvent>(ViewToggle);
    }
    private void AddSceneFolders(MouseUpEvent evt)
    {
        string fullPath = EditorUtility.OpenFolderPanel("Open Scene Folder", "", "");

        if(!string.IsNullOrEmpty(fullPath) && fullPath.StartsWith(Application.dataPath))
        {
            string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);
            string guid = AssetDatabase.AssetPathToGUID(relativePath);

            if(!string.IsNullOrEmpty(guid) && !sceneFolderGUIDs.Contains(guid))
            {
                sceneFolderGUIDs.Add(guid);
                SaveFolderPrefs();
                UpdateScrollView();
            }
        }
    }
    private void ClearFolders(MouseUpEvent evt)
    {
        sceneFolderGUIDs.Clear();
        UpdateScrollView();
    }
    private void ViewToggle(MouseUpEvent evt)
    {
        viewToggle = !viewToggle;
        headerLabel.text = viewToggle ? "FOLDERS" : "SCENES";

        UpdateScrollView();
    }
    private void UpdateScrollView()
    {
        scrollView.Clear();

        if(viewToggle) // Folder view
        {
            foreach(var guid in sceneFolderGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GroupBox folderGroup = new GroupBox();
                folderGroup.AddToClassList("folder-group");
                Label folderLabel = new Label(path);
                folderLabel.AddToClassList("folder-label");
                Button xButton = new Button(() =>
                {
                    sceneFolderGUIDs.Remove(guid);
                    SaveFolderPrefs();
                    UpdateScrollView();
                });
                xButton.text = "X";
                xButton.AddToClassList("x-button");

                folderGroup.Add(folderLabel);
                folderGroup.Add(xButton);

                scrollView.Add(folderGroup);
            }
        }
        else // Scene view
        {
            sceneAssetPaths.Clear();

            foreach(string guid in sceneFolderGUIDs)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(guid);

                if(string.IsNullOrEmpty(folderPath)) continue;

                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new string[] { folderPath });

                foreach(string sceneGuid in sceneGuids)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);

                    if(!string.IsNullOrEmpty(scenePath) && !sceneAssetPaths.Contains(scenePath))
                    {
                        sceneAssetPaths.Add(scenePath);
                    }
                }
            }

            foreach(string scenePath in sceneAssetPaths)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                Button button = new Button(() =>
                {
                    if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scenePath);
                    }
                });

                button.text = sceneName;

                button.AddToClassList("button");
                button.AddToClassList("scene-button");
                scrollView.Add(button);
            }
        }
    }
    private void SaveFolderPrefs()
    {
        var data = new SceneFolderGUIDData { guids = sceneFolderGUIDs };
        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EDITOR_KEY_SCENE_FOLDER_GUIDS, json);
    }
    private void LoadFolderPrefs()
    {
        if(EditorPrefs.HasKey(EDITOR_KEY_SCENE_FOLDER_GUIDS))
        {
            string json = EditorPrefs.GetString(EDITOR_KEY_SCENE_FOLDER_GUIDS);
            var data = JsonUtility.FromJson<SceneFolderGUIDData>(json);
            if(data != null && data.guids != null)
                sceneFolderGUIDs = data.guids;
        }
    }
}
