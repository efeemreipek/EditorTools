using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ColoredFoldersDrawer
{
    private static ColoredFoldersData foldersData;
    private static Dictionary<string, Color> colorCache = new Dictionary<string, Color>();

    private static Texture2D folderIcon;
    private static Texture2D folderOpenIcon;
    private static Texture2D folderEmptyIcon;

    static ColoredFoldersDrawer()
    {
        LoadFolderIcons();
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        EditorApplication.update += UpdateProjectBrowser;
        EditorApplication.projectChanged += Refresh;
        RefreshColorCache();
    }

    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if(!AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }
        if(!colorCache.ContainsKey(guid))
        {
            // Debug.Log($"No color defined for folder: {assetPath}"); // Uncomment for debugging
            return;
        }

        string[] contents = AssetDatabase.FindAssets("", new[] { assetPath });
        bool isEmpty = contents.Length == 0;
        bool isOpened = false;
        bool isTreeView = selectionRect.width > selectionRect.height;
        bool isSideView = Mathf.Abs(selectionRect.width - 14) > float.Epsilon;

        if(isTreeView)
        {
            selectionRect.width = selectionRect.height = 16;
            if(!isSideView)
            {
                selectionRect.x += 3f;
            }
            else
            {
                isOpened = ProjectWindowUtil.IsFolderOpened(assetPath);
            }
        }
        else
        {
            selectionRect.height -= 14f;
        }

        Texture2D icon = isEmpty ? folderEmptyIcon : (isOpened ? folderOpenIcon : folderIcon);
        if(icon == null) return;

        DrawColoredIcon(selectionRect, icon, colorCache[guid]);
    }
    private static void LoadFolderIcons()
    {
        folderIcon = EditorGUIUtility.FindTexture("d_Folder Icon");
        folderOpenIcon = EditorGUIUtility.FindTexture("d_FolderOpened Icon");
        folderEmptyIcon = EditorGUIUtility.FindTexture("d_FolderEmpty Icon");

        if(folderIcon == null || folderOpenIcon == null || folderEmptyIcon == null)
        {
            Debug.LogError("Failed to load one or more folder icons.");
        }
    }
    private static void DrawColoredIcon(Rect rect, Texture2D icon, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = previousColor;
    }

    private static void RefreshColorCache()
    {
        colorCache.Clear();

        // Load the data asset
        string dataPath = "Assets/ColoredFolders/ColoredFoldersData.asset";
        foldersData = AssetDatabase.LoadAssetAtPath<ColoredFoldersData>(dataPath);

        if(foldersData == null)
            return;

        // Populate cache
        foreach(var entry in foldersData.Data)
        {
            if(!string.IsNullOrEmpty(entry.GUID))
            {
                colorCache[entry.GUID] = entry.Color;
            }
        }
    }
    private static void UpdateProjectBrowser()
    {
        ProjectWindowUtil.UpdateBrowserFields();
    }

    public static void Refresh()
    {
        RefreshColorCache();
        EditorApplication.RepaintProjectWindow();
    }
}
