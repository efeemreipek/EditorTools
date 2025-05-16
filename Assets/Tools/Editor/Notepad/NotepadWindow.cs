using UnityEditor;
using UnityEngine;

public class NotepadWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 1000f;
        public const float MIN_WINDOW_HEIGHT = 500f;
        public const float SPACE = 5f;
    }

    [MenuItem("Tools/Notepad")]
    public static void ShowWindow()
    {
        var win = GetWindow<NotepadWindow>("Notepad");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }
}
