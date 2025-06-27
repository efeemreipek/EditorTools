using UnityEditor;
using UnityEngine;

public class ColoredFoldersContextMenu
{
    [MenuItem("Assets/Color Folder", true)]
    private static bool OpenColoredFoldersWindowValidate()
    {
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
    private static string targetGUID;
    private static ColoredFoldersData foldersData;
    private Color selectedColor = Color.white;


    public static void Open(string guid)
    {
        targetGUID = guid;

        string path = "Assets/ColoredFolders/ColoredFoldersData.asset";
        foldersData = AssetDatabase.LoadAssetAtPath<ColoredFoldersData>(path);
        if(foldersData == null)
        {
            foldersData = ScriptableObject.CreateInstance<ColoredFoldersData>();
            if(!AssetDatabase.IsValidFolder("Assets/ColoredFolders"))
            {
                AssetDatabase.CreateFolder("Assets", "ColoredFolders");
            }
            AssetDatabase.CreateAsset(foldersData, path);
            AssetDatabase.SaveAssets();
        }

        var window = GetWindow<ColoredFoldersWindow>("Colored Folders");
        window.minSize = new Vector2(300f, 300f);
        window.Show();
    }

    private void OnGUI()
    {
        
    }
}
