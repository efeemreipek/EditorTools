using UnityEditor;
using UnityEngine;

public class ColorPaletteManagerWindow : EditorWindow
{
    private static class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 400f;
    }

    [MenuItem("Tools/Color Palette Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<ColorPaletteManagerWindow>("Color Palette Manager");
        window.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }
}
