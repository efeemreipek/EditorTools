using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ScenesWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float SPACE = 5f;
    }
    [Serializable]
    private class SceneFolderPathsData
    {
        public List<string> paths = new List<string>();
    }

    [MenuItem("Tools/Scenes")]
    public static void ShowWindow()
    {
        var win = GetWindow<ScenesWindow>("Scenes");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private Vector2 scrollPos;
    private bool isStylesInitDone;
    private List<string> sceneFolderPaths = new List<string>();
    private List<string> sceneAssetPaths = new List<string>();

    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;

    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);

    private const string EDITOR_KEY_SCENE_FOLDER_PATHS = "SCENES_FOLDER_PATHS";

    private void OnEnable()
    {
        if(EditorPrefs.HasKey(EDITOR_KEY_SCENE_FOLDER_PATHS))
        {
            string json = EditorPrefs.GetString(EDITOR_KEY_SCENE_FOLDER_PATHS);
            var data = JsonUtility.FromJson<SceneFolderPathsData>(json);

            if(data != null && data.paths != null && data.paths.Count > 0)
            {
                sceneFolderPaths = data.paths;
            }
        }
    }
    private void OnDisable()
    {
        var data = new SceneFolderPathsData() { paths = sceneFolderPaths };
        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EDITOR_KEY_SCENE_FOLDER_PATHS, json);
    }
    private void OnGUI()
    {
        if(!isStylesInitDone) InitializeStyles();

        DrawHeader();
        DrawScenesList();
        DrawControlButtons();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);

        EditorGUILayout.LabelField("SCENES", headerStyle);

        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();
    }
    private void DrawScenesList()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawScenes();
        EditorGUILayout.EndScrollView();

        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();
    }
    private void DrawScenes()
    {
        sceneAssetPaths.Clear();

        foreach(string folderPath in sceneFolderPaths)
        {
            if(string.IsNullOrEmpty(folderPath)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Scene", new string[] { folderPath });

            foreach(string guid in guids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);

                if(!string.IsNullOrEmpty(scenePath) && !sceneAssetPaths.Contains(scenePath))
                {
                    sceneAssetPaths.Add(scenePath);
                }
            }
        }

        foreach(string scenePath in sceneAssetPaths)
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            GUI.color = buttonColor;

            if(GUILayout.Button(sceneName, buttonStyle) && Event.current.button == 0)
            {
                if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }

            GUI.color = Color.white;
        }
    }
    private void DrawControlButtons()
    {
        EditorGUILayout.BeginVertical("Box");

        GUI.color = buttonColor;

        if(GUILayout.Button("ADD SCENE FOLDER", buttonStyle) && Event.current.button == 0)
        {
            string fullPath = EditorUtility.OpenFolderPanel("Open Scene Folder", "", "");

            if(!string.IsNullOrEmpty(fullPath) && fullPath.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);

                if(!sceneFolderPaths.Contains(relativePath))
                {
                    sceneFolderPaths.Add(relativePath);
                }
            }
        }
        if(GUILayout.Button("CLEAR SCENE FOLDERS", buttonStyle) && Event.current.button == 0)
        {
            sceneFolderPaths.Clear();
        }

        GUI.color = Color.white;

        EditorGUILayout.EndVertical();
    }
    private void InitializeStyles()
    {
        isStylesInitDone = true;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 14;
        buttonStyle.normal.textColor = Color.white;
    }
}
