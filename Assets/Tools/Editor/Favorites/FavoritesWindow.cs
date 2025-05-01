using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

public class FavoritesWindow : EditorWindow
{
    [System.Serializable]
    private class FavoritesData
    {
        public List<string> favoriteGuids = new List<string>();
    }
    private static class Layout
    {
        public const float BUTTON_HEIGHT = 25f;
        public const float HEADER_BUTTON_HEIGHT = 30f;
        public const float ROW_SPACING = 2f;
        public const float X_BUTTON_WIDTH = 35f;
        public const float SCROLLBAR_WIDTH = 16f;
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float HEADER_SPACE = 10f;
    }

    [MenuItem("Tools/Favorites")]
    public static void ShowWindow() => GetWindow<FavoritesWindow>("Favorites");

    private Vector2 scrollPos;
    private List<Object> favorites = new List<Object>();
    private Dictionary<Object, Texture> iconCache = new Dictionary<Object, Texture>();
    private Color colorRed = new Color(0.93f, 0.38f, 0.34f);
    private ReorderableList reorderableList;
    private SerializedObject serializedObjectWrapper;

    private void OnEnable()
    {
        string path = "Assets/Tools/Editor/Favorites/Favorites.json";
        if(File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<FavoritesData>(json);
            favorites = data.favoriteGuids
                .Select(guid => AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(obj => obj != null)
                .ToList();
        }

        InitializeReorderableList();
    }
    private void OnDisable()
    {
        if(favorites.Count == 0) return;

        string path = "Assets/Tools/Editor/Favorites/Favorites.json";
        var data = new FavoritesData
        {
            favoriteGuids = favorites
                .Where(obj => obj != null)
                .Select(obj => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)))
                .ToList()
        };
        File.WriteAllText(path, JsonUtility.ToJson(data));
    }

    private void OnGUI()
    {
        minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);

        EditorGUILayout.BeginVertical("Box");
        GUILayout.Space(Layout.HEADER_SPACE);
        DrawHeaderButtons();
        GUILayout.Space(Layout.HEADER_SPACE);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("Box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        serializedObjectWrapper.Update();
        reorderableList.DoLayoutList();
        serializedObjectWrapper.ApplyModifiedProperties();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawHeaderButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button("Favorite Selected", GUILayout.Height(Layout.HEADER_BUTTON_HEIGHT)))
        {
            foreach(var item in Selection.objects)
            {
                if(!favorites.Contains(item))
                {
                    favorites.Add(item);
                    iconCache.Remove(item);
                }
            }
        }
        if(GUILayout.Button("Clear Favorites", GUILayout.Height(Layout.HEADER_BUTTON_HEIGHT)))
        {
            favorites.Clear();
            iconCache.Clear();
        }

        EditorGUILayout.EndHorizontal();
    }
    private void InitializeReorderableList()
    {
        serializedObjectWrapper = new SerializedObject(this);
        reorderableList = new ReorderableList(favorites, typeof(Object), true, false, false, false);

        reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => DrawFavoriteItem(rect, index);

        reorderableList.elementHeight = Layout.BUTTON_HEIGHT + Layout.ROW_SPACING * 2;
    }

    private void DrawFavoriteItem(Rect rect, int index)
    {
        Object item = favorites[index];
        if(item == null) return;

        if(!iconCache.TryGetValue(item, out Texture icon) || icon == null)
        {
            icon = item is Texture2D
                ? EditorGUIUtility.ObjectContent(null, typeof(Texture2D)).image
                : AssetPreview.GetMiniThumbnail(item) ?? EditorGUIUtility.ObjectContent(null, item.GetType()).image;
            iconCache[item] = icon;
        }

        GUIContent content = new GUIContent(item.name, icon);

        float verticalOffset = (reorderableList.elementHeight - Layout.BUTTON_HEIGHT) * 0.5f;

        Rect buttonRect = new Rect(rect.x, rect.y + verticalOffset, rect.width - Layout.X_BUTTON_WIDTH - 4f, Layout.BUTTON_HEIGHT);
        Rect xButtonRect = new Rect(rect.xMax - Layout.X_BUTTON_WIDTH, rect.y + verticalOffset, Layout.X_BUTTON_WIDTH, Layout.BUTTON_HEIGHT);

        if(GUI.Button(buttonRect, content))
        {
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }

        GUI.color = colorRed;
        if(GUI.Button(xButtonRect, "X"))
        {
            favorites.RemoveAt(index);
            iconCache.Remove(item);
        }
        ChangeColorToNormal();
    }
    private void ChangeColorToNormal() => GUI.color = Color.white;
}