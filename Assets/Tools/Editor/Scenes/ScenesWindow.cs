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
    private class SceneFolderGUIDData
    {
        public List<string> guids = new List<string>();
    }

    [MenuItem("Tools/Scenes")]
    public static void ShowWindow()
    {
        var win = GetWindow<ScenesWindow>("Scenes");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private Vector2 scrollPos;
    private bool isStylesInitDone;
    private List<string> sceneFolderGUIDs = new List<string>();
    private List<string> sceneAssetPaths = new List<string>();

    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;

    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);

    private const string EDITOR_KEY_SCENE_FOLDER_GUIDS = "SCENES_FOLDER_GUIDS";

    private void OnEnable()
    {
        if(EditorPrefs.HasKey(EDITOR_KEY_SCENE_FOLDER_GUIDS))
        {
            string json = EditorPrefs.GetString(EDITOR_KEY_SCENE_FOLDER_GUIDS);
            var data = JsonUtility.FromJson<SceneFolderGUIDData>(json);

            if(data != null && data.guids != null && data.guids.Count > 0)
            {
                sceneFolderGUIDs = data.guids;
            }
        }
    }
    private void OnDisable()
    {
        var data = new SceneFolderGUIDData() { guids = sceneFolderGUIDs };
        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EDITOR_KEY_SCENE_FOLDER_GUIDS, json);
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
                string guid = AssetDatabase.AssetPathToGUID(relativePath);

                if(!string.IsNullOrEmpty(guid) && !sceneFolderGUIDs.Contains(guid))
                {
                    sceneFolderGUIDs.Add(guid);
                }
            }
        }
        if(GUILayout.Button("CLEAR SCENE FOLDERS", buttonStyle) && Event.current.button == 0)
        {
            sceneFolderGUIDs.Clear();
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
