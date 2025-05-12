using UnityEditor;
using UnityEngine;

public class RandomizerWindow : EditorWindow
{
    private class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
    }

    [MenuItem("Tools/Randomizer")]
    public static void ShowWindow()
    {
        var win = GetWindow<RandomizerWindow>("Randomizer");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private int seed = 0;

    private void OnGUI()
    {
        int newSeed = EditorGUILayout.IntSlider(seed, 0, 100);
        if(newSeed != seed)
        {
            seed = newSeed;
            ApplyRandomRotation();
        }
    }

    private void ApplyRandomRotation()
    {
        Random.InitState(seed);

        foreach(GameObject obj in Selection.gameObjects)
        {
            if(obj == null) return;

            Undo.RecordObject(obj.transform, "Random Rotation");

            Vector3 randomRotation = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));

            obj.transform.rotation = Quaternion.Euler(randomRotation);

            EditorUtility.SetDirty(obj);
        }
    }
}
