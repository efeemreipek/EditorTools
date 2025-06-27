using UnityEditor;
using UnityEngine;

public class ColoredFoldersContextMenu
{
    [MenuItem("Assets/Color Folder", true)]
    private static bool OpenColoredFoldersWindowValidate()
    {
        // Only show for folders
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return AssetDatabase.IsValidFolder(path);
    }

    [MenuItem("Assets/Color Folder")]
    private static void OpenColoredFoldersWindow()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string guid = AssetDatabase.AssetPathToGUID(path);
        ColoredFoldersWindow.Open(guid);
    }
}

public class ColoredFoldersWindow : EditorWindow
{
    public static void Open(string guid)
    {
        var window = GetWindow<ColoredFoldersWindow>("Colored Folders");
        window.minSize = new Vector2(300f, 300f);
        window.Show();
    }
}
