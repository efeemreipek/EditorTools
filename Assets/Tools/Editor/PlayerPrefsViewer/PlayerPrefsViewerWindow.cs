using UnityEditor;
using UnityEngine;

public class PlayerPrefsViewerWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 400f;
        public const float MIN_WINDOW_HEIGHT = 500f;
        public const float SPACE = 5f;
    }

    [MenuItem("Tools/PlayerPrefs Viewer")]
    public static void ShowWindow()
    {
        var win = GetWindow<PlayerPrefsViewerWindow>("PlayerPrefs Viewer");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }
}
