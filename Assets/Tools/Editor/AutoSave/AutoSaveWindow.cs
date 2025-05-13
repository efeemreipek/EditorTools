using UnityEditor;
using UnityEngine;

public class AutoSaveWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float SPACE = 5f;
    }

    [MenuItem("Tools/Auto Save")]
    public static void ShowWindow()
    {
        var win = GetWindow<AutoSaveWindow>("Auto Save");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }
}
