using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Favorites : EditorWindow
{
    [System.Serializable]
    private class FavoritesData
    {
        public List<string> globalIDs = new List<string>();
    }

    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Tools/UI Toolkit/Favorites")]
    public static void ShowWindow()
    {
        Favorites wnd = GetWindow<Favorites>("Favorites");
        wnd.minSize = new Vector2(300, 300);
    }

    private const string EDITOR_KEY_FAVORITES = "FAVORITES_LIST";

    private VisualElement rootArea;
    private VisualElement dropArea;
    private Button favoriteButton;
    private Button clearButton;
    private ListView favoritesListView;

    private List<Object> favorites = new List<Object>();
    private Dictionary<Object, Texture> iconCache = new Dictionary<Object, Texture>();
    private int previousFavoritesCount = 0;

    public void CreateGUI()
    {
        VisualElement root = m_VisualTreeAsset.Instantiate();
        root.style.flexGrow = 1;
        rootVisualElement.Add(root);
        rootVisualElement.style.flexGrow = 1;

        rootArea = root.Q<VisualElement>("root-area");
        dropArea = root.Q<VisualElement>("drop-area");
        favoriteButton = root.Q<Button>("favorite-button");
        clearButton = root.Q<Button>("clear-button");
        favoritesListView = root.Q<ListView>("favorites-list-view");

        favoriteButton.clicked += FavoriteButton_Clicked;
        clearButton.clicked += ClearButton_Clicked;

        SetupListView();
        SetupDragAndDrop();

        LoadFavorites();
        RefreshFavoritesList();
    }
    private void OnEnable()
    {
        LoadFavorites();
    }
    private void OnDisable()
    {
        SaveFavorites();
    }

    private void FavoriteButton_Clicked()
    {
        foreach(var item in Selection.objects)
        {
            if(!favorites.Contains(item))
            {
                favorites.Add(item);
                iconCache.Remove(item);
            }
        }
        RefreshFavoritesList();
    }
    private void ClearButton_Clicked()
    {
        favorites.Clear();
        iconCache.Clear();
        RefreshFavoritesList();
    }
    private void SaveFavorites()
    {
        if(previousFavoritesCount == 0 && favorites.Count == 0)
        {
            EditorPrefs.DeleteKey(EDITOR_KEY_FAVORITES);
            return;
        }

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

        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EDITOR_KEY_FAVORITES, json);
    }
    private void LoadFavorites()
    {
        string json = EditorPrefs.GetString(EDITOR_KEY_FAVORITES, "");
        if(!string.IsNullOrEmpty(json))
        {
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
    }
    private void SetupDragAndDrop()
    {
        rootVisualElement.RegisterCallback<DragUpdatedEvent>(evt =>
        {
            if(DragAndDrop.objectReferences.Length > 0)
            {
                var sourceData = DragAndDrop.GetGenericData("source");
                if(sourceData != null && sourceData.ToString() == "Favorites")
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
                    return;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                ShowDropArea();
                evt.StopPropagation();
            }
        });

        rootVisualElement.RegisterCallback<DragPerformEvent>(evt =>
        {
            var sourceData = DragAndDrop.GetGenericData("source");
            if(sourceData != null && sourceData.ToString() == "Favorites")
            {
                return;
            }

            foreach(var dragged in DragAndDrop.objectReferences)
            {
                if(dragged != null && !favorites.Contains(dragged))
                {
                    favorites.Add(dragged);
                    iconCache.Remove(dragged);
                }
            }
            HideDropArea();
            RefreshFavoritesList();
            DragAndDrop.AcceptDrag();
            evt.StopPropagation();
        });

        rootVisualElement.RegisterCallback<DragExitedEvent>(evt =>
        {
            HideDropArea();

            DragAndDrop.AcceptDrag();
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new Object[0];
            DragAndDrop.SetGenericData("source", null);

            evt.StopPropagation();
        });

        rootVisualElement.RegisterCallback<DragLeaveEvent>(evt =>
        {
            HideDropArea();
            evt.StopPropagation();
        });
    }
    private void ShowDropArea()
    {
        rootArea.style.display = DisplayStyle.None;
        dropArea.style.display = DisplayStyle.Flex;
    }

    private void HideDropArea()
    {
        dropArea.style.display = DisplayStyle.None;
        rootArea.style.display = DisplayStyle.Flex;
    }
    private void SetupListView()
    {
        favoritesListView.itemsSource = favorites;
        favoritesListView.fixedItemHeight = 40f;
        favoritesListView.reorderable = true;
        favoritesListView.reorderMode = ListViewReorderMode.Animated;
        favoritesListView.showBorder = true;
        favoritesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

        favoritesListView.makeItem = () => CreateListViewItem();
        favoritesListView.bindItem = (element, index) => BindListViewItem(element, index);

        favoritesListView.selectionChanged += OnSelectionChanged;
        favoritesListView.itemsSourceChanged += OnItemsSourceChanged;
    }
    private VisualElement CreateListViewItem()
    {
        var container = new VisualElement();
        container.AddToClassList("favorite-item");

        var mainButton = new Button();
        mainButton.AddToClassList("favorite-main-button");

        var iconElement = new VisualElement();
        iconElement.AddToClassList("favorite-icon");

        var label = new Label();
        label.AddToClassList("favorite-label");

        var removeButton = new Button();
        removeButton.AddToClassList("favorite-remove-button");
        removeButton.text = "X";

        mainButton.Add(iconElement);
        mainButton.Add(label);
        container.Add(mainButton);
        container.Add(removeButton);

        return container;
    }
    private void BindListViewItem(VisualElement element, int index)
    {
        if(index >= favorites.Count) return;

        var item = favorites[index];
        if(item == null) return;

        var mainButton = element.Q<Button>(className: "favorite-main-button");
        var iconElement = element.Q<VisualElement>(className: "favorite-icon");
        var label = element.Q<Label>(className: "favorite-label");
        var removeButton = element.Q<Button>(className: "favorite-remove-button");

        label.text = item.name;

        if(!iconCache.TryGetValue(item, out Texture icon) || icon == null)
        {
            icon = item is Texture2D
                ? EditorGUIUtility.ObjectContent(null, typeof(Texture2D)).image
                : AssetPreview.GetMiniThumbnail(item) ?? EditorGUIUtility.ObjectContent(null, item.GetType()).image;
            iconCache[item] = icon;
        }

        iconElement.style.backgroundImage = new StyleBackground(icon as Texture2D);

        mainButton.clicked -= () => OnFavoriteItemClicked(item);
        removeButton.clicked -= () => RemoveFavorite(index);

        mainButton.clicked += () => OnFavoriteItemClicked(item);
        removeButton.clicked += () => RemoveFavorite(index);

        SetupItemDragAndDrop(mainButton, item);
    }
    private void OnSelectionChanged(IEnumerable<object> selectedItems)
    {
        // Handle selection if needed - for now we don't need special selection behavior
    }
    private void OnItemsSourceChanged()
    {
        SaveFavorites();
    }
    private void SetupItemDragAndDrop(VisualElement element, Object item)
    {
        bool isDragging = false;
        Vector2 startMousePosition = Vector2.zero;
        const float dragThreshold = 5f;

        element.RegisterCallback<MouseDownEvent>(evt =>
        {
            if(evt.button == 0)
            {
                startMousePosition = evt.mousePosition;
                isDragging = false;
                element.CaptureMouse();
            }
        });

        element.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if(!element.HasMouseCapture() || isDragging) return;

            if(Vector2.Distance(evt.mousePosition, startMousePosition) > dragThreshold)
            {
                isDragging = true;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { item };
                DragAndDrop.SetGenericData("source", "Favorites");
                DragAndDrop.StartDrag($"Dragging {item.name}");

                element.ReleaseMouse();
            }
        });

        element.RegisterCallback<MouseUpEvent>(evt =>
        {
            if(element.HasMouseCapture())
            {
                element.ReleaseMouse();

                if(!isDragging && evt.button == 0)
                {
                    OnFavoriteItemClicked(item);
                }

                // Reset drag state
                DragAndDrop.AcceptDrag();
                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.SetGenericData("source", null);

                isDragging = false;

                favoritesListView.Rebuild();
            }
        });

        // IMPORTANT: cleanup when leaving element (in case drag never completed)
        element.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            if(element.HasMouseCapture())
            {
                element.ReleaseMouse();

                // Reset drag state
                DragAndDrop.AcceptDrag();
                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.SetGenericData("source", null);

                isDragging = false;

                favoritesListView.Rebuild();
            }
        });
    }
    private void OnFavoriteItemClicked(Object item)
    {
        Selection.activeObject = item;
        EditorGUIUtility.PingObject(item);
    }

    private void RemoveFavorite(int index)
    {
        if(index >= 0 && index < favorites.Count)
        {
            Object item = favorites[index];
            favorites.RemoveAt(index);
            iconCache.Remove(item);
            RefreshFavoritesList();
        }
    }
    private void RefreshFavoritesList()
    {
        if(favoritesListView == null) return;

        favoritesListView.itemsSource = null;
        favoritesListView.itemsSource = favorites;
        favoritesListView.Rebuild();
    }
}
