using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(MonoBehaviour), true)]
public class ButtonAttributeDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MonoBehaviour targetMono = (MonoBehaviour)target;

        MethodInfo[] methods = targetMono.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach(MethodInfo method in methods)
        {
            var attributes = method.GetCustomAttributes(typeof(ButtonAttribute), false);
            if(attributes.Length > 0)
            {
                ButtonAttribute buttonAttribute = (ButtonAttribute)attributes[0];
                string buttonLabel = string.IsNullOrEmpty(buttonAttribute.MethodName) ? method.Name : buttonAttribute.MethodName;

                if(GUILayout.Button(buttonLabel))
                {
                    method.Invoke(target, null);
                }
            }
        }
    }
}
