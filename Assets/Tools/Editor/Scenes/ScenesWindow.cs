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
        public const float MIN_WINDOW_HEIGHT = 400f;
        public const float SPACE = 5f;
        public const float BUTTON_HEIGHT = 50f;
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
    private bool isFolderViewOpen;

    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;

    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);
    private Color xButtonColor = new Color(0.93f, 0.38f, 0.34f);

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
        if(isFolderViewOpen)
        {
            DrawFoldersList();
        }
        else
        {
            DrawScenesList();
        }
        DrawControlButtons();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);

        EditorGUILayout.LabelField(isFolderViewOpen ? "FOLDERS" : "SCENES", headerStyle);

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
    private void DrawFoldersList()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.SPACE);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawFolders();
        EditorGUILayout.EndScrollView();

        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();
    }
    private void DrawFolders()
    {
        for(int i = 0; i < sceneFolderGUIDs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("Box");

            string folderPath = AssetDatabase.GUIDToAssetPath(sceneFolderGUIDs[i]);
            EditorGUILayout.LabelField(folderPath, EditorStyles.boldLabel);

            GUI.color = xButtonColor;
            if(GUILayout.Button("X", buttonStyle, GUILayout.Width(Layout.BUTTON_HEIGHT * 0.5f), GUILayout.Height(Layout.BUTTON_HEIGHT * 0.5f)) && Event.current.button == 0)
            {
                sceneFolderGUIDs.RemoveAt(i);
                GUIUtility.ExitGUI();
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }
    private void DrawControlButtons()
    {
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.BeginHorizontal();

        GUI.color = buttonColor;

        float buttonWidth = EditorGUIUtility.currentViewWidth * 0.5f - Layout.SPACE * 2f;

        if(GUILayout.Button("ADD SCENE FOLDER", buttonStyle, GUILayout.Height(Layout.BUTTON_HEIGHT), GUILayout.Width(buttonWidth)) && Event.current.button == 0)
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
        if(GUILayout.Button("CLEAR SCENE FOLDERS", buttonStyle, GUILayout.Height(Layout.BUTTON_HEIGHT), GUILayout.Width(buttonWidth)) && Event.current.button == 0)
        {
            sceneFolderGUIDs.Clear();
        }

        EditorGUILayout.EndHorizontal();

        if(GUILayout.Button(isFolderViewOpen ? "VIEW SCENES" : "VIEW FOLDERS", buttonStyle, GUILayout.Height(Layout.BUTTON_HEIGHT * 0.5f)) && Event.current.button == 0)
        {
            isFolderViewOpen = !isFolderViewOpen;
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
        buttonStyle.wordWrap = true;
    }
}
