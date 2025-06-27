using System.Reflection;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using System;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectWindowUtil
{
    private static Type ProjectBrowserType;

    private static EditorWindow ProjectBrowser;

    private static TreeViewState CurrentAssetTreeViewState;
    private static TreeViewState CurrentFolderTreeViewState;

    // 0 for one column, 1 for two column
    private static int CurrentProjectBrowserMode;

    private static FieldInfo AssetTreeStateField;
    private static FieldInfo FolderTreeStateField;
    private static FieldInfo ProjectBrowserMode;

    static ProjectWindowUtil()
    {
        ProjectBrowserType = Type.GetType("UnityEditor.ProjectBrowser, UnityEditor");
        if(ProjectBrowserType == null)
        {
            Debug.LogError("ProjectBrowser type not found. Ensure Unity Editor version compatibility.");
            return;
        }
        AssetTreeStateField = ProjectBrowserType.GetField("m_AssetTreeState", BindingFlags.NonPublic | BindingFlags.Instance);
        FolderTreeStateField = ProjectBrowserType.GetField("m_FolderTreeState", BindingFlags.NonPublic | BindingFlags.Instance);
        ProjectBrowserMode = ProjectBrowserType.GetField("m_ViewMode", BindingFlags.NonPublic | BindingFlags.Instance);

        if(AssetTreeStateField == null || FolderTreeStateField == null || ProjectBrowserMode == null)
        {
            Debug.LogError("One or more ProjectBrowser fields not found. Check Unity version.");
        }
    }


    public static bool IsFolderOpened(string path)
    {
        var state = CurrentProjectBrowserMode == 0 ? CurrentAssetTreeViewState : CurrentFolderTreeViewState;

        if(state != null)
        {
            var instanceID = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path).GetInstanceID();
            return state.expandedIDs.Contains(instanceID);
        }

        return false;
    }

    public static void UpdateBrowserFields()
    {
        try
        {
            var projectBrowsers = Resources.FindObjectsOfTypeAll(ProjectBrowserType);

            foreach(var obj in projectBrowsers)
            {
                var browser = obj as EditorWindow;
                if(browser.hasFocus)
                {
                    ProjectBrowser = browser;
                }
            }

            CurrentAssetTreeViewState = AssetTreeStateField.GetValue(ProjectBrowser) as TreeViewState;
            CurrentFolderTreeViewState = FolderTreeStateField.GetValue(ProjectBrowser) as TreeViewState;
            CurrentProjectBrowserMode = (int)ProjectBrowserMode.GetValue(ProjectBrowser);
        }
        catch
        {
            CurrentFolderTreeViewState = null;
        }
    }
}
