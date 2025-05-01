using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FavoritesWindow : EditorWindow
{
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
        DrawFavoritesList();
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
    private void DrawFavoritesList()
    {
        List<Object> toRemove = new List<Object>();
        var layout = CalculateLayout();

        EditorGUILayout.BeginVertical();
        foreach(var item in favorites)
        {
            if(item == null)
            {
                toRemove.Add(item);
                continue;
            }

            EditorGUILayout.BeginHorizontal();
            DrawFavoriteItem(item, layout, toRemove);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }
        EditorGUILayout.EndVertical();

        foreach(var item in toRemove)
        {
            favorites.Remove(item);
        }
    }
    private void DrawFavoriteItem(Object item, (float contentButtonWidth, bool verticalScrollbarWillShow) layout, List<Object> toRemove)
    {
        if(!iconCache.TryGetValue(item, out Texture icon) || icon == null)
        {
            icon = item is Texture2D 
                ? EditorGUIUtility.ObjectContent(null, typeof(Texture2D)).image
                : AssetPreview.GetMiniThumbnail(item) ?? EditorGUIUtility.ObjectContent(null, item.GetType()).image;
            iconCache[item] = icon;
        }
        
        GUIContent content = new GUIContent(item.name);
        content.image = icon;

        if(GUILayout.Button(content, GUILayout.Width(layout.contentButtonWidth), GUILayout.Height(Layout.BUTTON_HEIGHT)))
        {
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }

        GUI.color = colorRed;
        if(GUILayout.Button("X", GUILayout.Width(Layout.X_BUTTON_WIDTH), GUILayout.Height(Layout.BUTTON_HEIGHT)))
        {
            toRemove.Add(item);
            iconCache.Remove(item);
        }
        ChangeColorToNormal();
    }
    private (float contentButtonWidth, bool verticalScrollbarWillShow) CalculateLayout()
    {
        float estimatedRowHeight = Layout.BUTTON_HEIGHT + Layout.ROW_SPACING;
        float totalContentHeight = favorites.Count * estimatedRowHeight;
        float scrollViewHeight = position.height - 100f;
        bool verticalScrollbarWillShow = totalContentHeight > scrollViewHeight;
        float scrollbarWidth = verticalScrollbarWillShow ? Layout.SCROLLBAR_WIDTH : 0f;
        float contentButtonWidth = EditorGUIUtility.currentViewWidth - Layout.X_BUTTON_WIDTH - 30f - scrollbarWidth;

        return (contentButtonWidth, verticalScrollbarWillShow);
    }
    private void ChangeColorToNormal() => GUI.color = Color.white;
}