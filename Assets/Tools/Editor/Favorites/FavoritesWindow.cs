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
        public List<string> globalIDs = new List<string>();
    }
    private static class Layout
    {
        public const float BUTTON_HEIGHT = 30f;
        public const float HEADER_BUTTON_HEIGHT = 40f;
        public const float ROW_SPACING = 2f;
        public const float X_BUTTON_WIDTH = 35f;
        public const float SCROLLBAR_WIDTH = 16f;
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float HEADER_SPACE = 5f;
        public const float DROP_MARGIN = 5f;
    }

    [MenuItem("Tools/Favorites")]
    public static void ShowWindow() => GetWindow<FavoritesWindow>("Favorites");

    private Vector2 scrollPos;
    private List<Object> favorites = new List<Object>();
    private Dictionary<Object, Texture> iconCache = new Dictionary<Object, Texture>();
    private Color xButtonColor = new Color(0.93f, 0.38f, 0.34f);
    private Color headerButtonColor = new Color(0.74f, 0.74f, 0.74f);
    private ReorderableList reorderableList;
    private SerializedObject serializedObjectWrapper;
    private int? pendingRemoveIndex = null;
    private int previousFavoritesCount = 0;
    private bool isDragging;
    private bool isStyleInitDone;

    private GUIStyle headerButtonStyle;
    private GUIStyle dropLabelStyle;
    private GUIStyle centeredLabelStyle;
    private GUIStyle xButtonStyle;

    private void OnEnable()
    {
        string path = "Assets/Tools/Editor/Favorites/Favorites.json";
        if(File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<FavoritesData>(json);
            favorites = data.globalIDs
                .Select(idStr =>
                {
                    if(GlobalObjectId.TryParse(idStr, out var id))
                    {
                        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                    }
                    return null;
                })
                .Where(obj => obj != null)
                .ToList();

            previousFavoritesCount = favorites.Count;
        }

        InitializeReorderableList();
    }
    private void OnDisable()
    {
        if(previousFavoritesCount == 0 && favorites.Count == 0) return;

        string path = "Assets/Tools/Editor/Favorites/Favorites.json";
        var data = new FavoritesData
        {
            globalIDs = favorites
                .Where(obj => obj != null)
                .Select(obj =>
                {
                    var id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                    return id.ToString();
                })
                .ToList()
        };
        File.WriteAllText(path, JsonUtility.ToJson(data));
    }
    private void OnGUI()
    {
        if(!isStyleInitDone) InitializeStyles();

        minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);

        Rect windowRect = new Rect(0f, 0f, position.width, position.height);

        // drag and drop logic
        if(Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
        {
            if(windowRect.Contains(Event.current.mousePosition))
            {
                isDragging = true;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                Repaint();

                if(Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach(var dragged in DragAndDrop.objectReferences)
                    {
                        if(dragged != null && !favorites.Contains(dragged))
                        {
                            favorites.Add(dragged);
                            iconCache.Remove(dragged);
                        }
                    }
                    isDragging = false;
                    Event.current.Use();
                    Repaint();
                }
                Event.current.Use();
                return;
            }
        }
        else if(Event.current.type == EventType.DragExited)
        {
            isDragging = false;
            Repaint();
        }

        if(isDragging)
        {
            DrawDropArea(windowRect);
            return;
        }

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

        // if favorite needs to be removed then remove
        if(pendingRemoveIndex.HasValue)
        {
            int i = pendingRemoveIndex.Value;
            if(i >= 0 && i < favorites.Count)
            {
                Object item = favorites[i];
                favorites.RemoveAt(i);
                iconCache.Remove(item);
                reorderableList.index = -1;
            }
            pendingRemoveIndex = null;
            GUI.FocusControl(null);
            Repaint();
        }

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            reorderableList.index = -1;
            Repaint();
        }
    }
    private void OnLostFocus()
    {
        reorderableList.index = -1;
        Repaint();
    }

    private void DrawDropArea(Rect windowRect)
    {
        GUI.Box(windowRect, string.Empty);

        GUI.color = Color.black;

        Rect dropRect = new Rect(
            windowRect.x + Layout.DROP_MARGIN,
            windowRect.y + Layout.DROP_MARGIN,
            windowRect.width - Layout.DROP_MARGIN * 2,
            windowRect.height - Layout.DROP_MARGIN * 2
            );

        GUI.Box(dropRect, string.Empty);

        ChangeColorToNormal();

        GUI.Label(dropRect, "DROP HERE\nTO FAVORITE", dropLabelStyle);
    }
    private void DrawHeaderButtons()
    {
        GUI.color = headerButtonColor;

        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button("FAVORITE", headerButtonStyle, GUILayout.Height(Layout.HEADER_BUTTON_HEIGHT)) && Event.current.button == 0)
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
        if(GUILayout.Button("CLEAR", headerButtonStyle, GUILayout.Height(Layout.HEADER_BUTTON_HEIGHT)) && Event.current.button == 0)
        {
            favorites.Clear();
            iconCache.Clear();
        }

        EditorGUILayout.EndHorizontal();
        ChangeColorToNormal();
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

        // calculate button position
        float verticalOffset = (reorderableList.elementHeight - Layout.BUTTON_HEIGHT) * 0.5f;

        Rect buttonRect = new Rect(rect.x, rect.y + verticalOffset, rect.width - Layout.X_BUTTON_WIDTH - 4f, Layout.BUTTON_HEIGHT);
        Rect xButtonRect = new Rect(rect.xMax - Layout.X_BUTTON_WIDTH, rect.y + verticalOffset, Layout.X_BUTTON_WIDTH, Layout.BUTTON_HEIGHT);

        // draw empty button
        if(GUI.Button(buttonRect, GUIContent.none) && Event.current.button == 0)
        {
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }

        // calculate icon position
        float iconSize = Layout.BUTTON_HEIGHT - 4f;
        float iconPadding = 4f;
        Rect iconRect = new Rect(buttonRect.x + iconPadding, buttonRect.y + (buttonRect.height - iconSize) * 0.5f, iconSize, iconSize);

        // calculate text position
        float labelStartX = iconRect.xMax + iconPadding;
        Rect labelRect = new Rect(labelStartX, buttonRect.y, buttonRect.width - (labelStartX - buttonRect.x) - 2f, buttonRect.height);

        // draw icon
        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

        // draw text
        GUI.Label(labelRect, item.name, centeredLabelStyle);

        // draw X button
        GUI.color = xButtonColor;
        if(GUI.Button(xButtonRect, "X", xButtonStyle) && Event.current.button == 0)
        {
            pendingRemoveIndex = index;
        }
        ChangeColorToNormal();
    }
    private void InitializeStyles()
    {
        isStyleInitDone = true;

        headerButtonStyle = new GUIStyle(GUI.skin.button);
        headerButtonStyle.alignment = TextAnchor.MiddleCenter;
        headerButtonStyle.fontStyle = FontStyle.Bold;
        headerButtonStyle.fontSize = 14;
        headerButtonStyle.normal.textColor = Color.white;

        centeredLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            fontStyle = FontStyle.Bold
        };

        dropLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };

        xButtonStyle = new GUIStyle(GUI.skin.button);
        xButtonStyle.alignment = TextAnchor.MiddleCenter;
        xButtonStyle.fontStyle = FontStyle.Bold;
    }
    private void ChangeColorToNormal() => GUI.color = Color.white;
}