using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class MultiRenamerWindow : EditorWindow
{
    private static class Layout
    {
        public const float MIN_WINDOW_WIDTH = 300f;
        public const float MIN_WINDOW_HEIGHT = 300f;
        public const float SPACE = 5f;
        public const float BUTTON_HEIGHT = 40f;
        public const float PREVIEW_HEIGHT = 40f;
    }
    private enum NumberingStyle
    {
        [InspectorName("{NAME}###")]
        Adjacent,
        [InspectorName("{NAME}_###")]
        Underscore,
        [InspectorName("{NAME} (###)")]
        Parenthesis
    }
    private enum CaseOption
    {
        [InspectorName("lowercase")]
        Lowercase,
        [InspectorName("UPPERCASE")]
        Uppercase,
        [InspectorName("Title Case")]
        Titlecase,
        [InspectorName("camelCase")]
        Camelcase,
        [InspectorName("PascalCase")]
        Pascalcase,
        [InspectorName("kebab-case")]
        Kebabcase,
        [InspectorName("snake_case")]
        Snakecase,
        [InspectorName("UPPER_SNAKE_CASE")]
        UpperSnakecase,
        [InspectorName("Train-Case")]
        Traincase
    }

    [MenuItem("Tools/Multi Renamer")]
    public static void ShowWindow()
    {
        var win = GetWindow<MultiRenamerWindow>("Multi Renamer");
        win.minSize = new Vector2(Layout.MIN_WINDOW_WIDTH, Layout.MIN_WINDOW_HEIGHT);
    }

    private string newName;
    private bool changeOriginalName;
    private string baseName = string.Empty;
    private bool addPrefix;
    private string prefix = string.Empty;
    private bool addSuffix;
    private string suffix = string.Empty;
    private bool addNumbering;
    private string numbering = string.Empty;
    private NumberingStyle numberingStyle;
    private int startNumber = 1;
    private int padding = 2;
    private int maxCharacters = 64;
    private bool useCaseOption;
    private CaseOption caseOption;
    private bool isStylesInitDone;

    private GUIStyle buttonStyle;
    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);
    private GUIStyle previewStyle;

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }
    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }
    private void OnSelectionChanged()
    {
        Repaint();
    }
    private void OnGUI()
    {
        if(!isStylesInitDone) InitializeStyles();

        DrawPreview();
        EditorGUILayout.BeginVertical("Box");
        DrawBaseName();
        GUILayout.Space(Layout.SPACE);
        DrawPrefix();
        GUILayout.Space(Layout.SPACE);
        DrawSuffix();
        GUILayout.Space(Layout.SPACE);
        DrawNumbering();
        GUILayout.Space(Layout.SPACE);
        DrawCaseOption();
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndVertical();
        DrawApplyButton();

        // if clicked on window, deselect, defocus
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }
    private void InitializeStyles()
    {
        isStylesInitDone = true;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 14;
        buttonStyle.normal.textColor = Color.white;

        previewStyle = new GUIStyle(EditorStyles.textArea);
        previewStyle.fontSize = 20;
        previewStyle.fontStyle = FontStyle.Bold;
    }
    private void DrawPreview()
    {
        EditorGUILayout.BeginVertical("Box");
        GUILayout.Label("PREVIEW", EditorStyles.centeredGreyMiniLabel);

        if(Selection.objects.Length > 0)
        {
            string previewName = GetNewName(Selection.objects[0], 0);

            EditorGUILayout.TextArea(previewName, previewStyle, GUILayout.ExpandWidth(true), GUILayout.Height(Layout.PREVIEW_HEIGHT));
        }
        else
        {
            EditorGUILayout.HelpBox("Select objects to preview renaming", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }
    private void DrawBaseName()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        changeOriginalName = EditorGUILayout.ToggleLeft("Change Original Name", changeOriginalName);
        if(changeOriginalName)
        {
            baseName = EditorGUILayout.TextField(new GUIContent("New Base Name"), baseName);
        }
        else
        {
            baseName = string.Empty;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawPrefix()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        addPrefix = EditorGUILayout.ToggleLeft("Add Prefix", addPrefix);
        if(addPrefix)
        {
            prefix = EditorGUILayout.TextField(new GUIContent("Prefix"), prefix);
        }
        else
        {
            prefix = string.Empty;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawSuffix()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        addSuffix = EditorGUILayout.ToggleLeft("Add Suffix", addSuffix);
        if(addSuffix)
        {
            suffix = EditorGUILayout.TextField(new GUIContent("Suffix"), suffix);
        }
        else
        {
            suffix = string.Empty;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawNumbering()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        addNumbering = EditorGUILayout.ToggleLeft("Add Numbering", addNumbering);
        if(addNumbering)
        {
            numberingStyle = (NumberingStyle)EditorGUILayout.EnumPopup("Numbering Style", numberingStyle);
            startNumber = EditorGUILayout.IntField("Start Number", startNumber);
            padding = EditorGUILayout.IntSlider("Number Padding", padding, 1, 5);
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawCaseOption()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        useCaseOption = EditorGUILayout.ToggleLeft("Use Case Option", useCaseOption);
        if(useCaseOption)
        {
            caseOption = (CaseOption)EditorGUILayout.EnumPopup("Case Option", caseOption);
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawApplyButton()
    {
        GUI.color = buttonColor;
        GUI.enabled = Selection.objects.Length > 0;
        EditorGUILayout.BeginVertical("Box");
        if(GUILayout.Button("APPLY", buttonStyle, GUILayout.Height(Layout.BUTTON_HEIGHT)) && Event.current.button == 0)
        {
            if(Selection.objects.Length > 0)
            {
                RenameObjects(Selection.objects);
            }
        }
        EditorGUILayout.EndVertical();
        GUI.enabled = true;
        GUI.color = Color.white;
    }
    private void RenameObjects(Object[] objects)
    {
        Undo.RecordObjects(objects, "Multi Rename");

        for(int i = 0;  i < objects.Length; i++)
        {
            Object obj = objects[i];
            string newObjectName = GetNewName(obj, i);

            if(obj is GameObject)
            {
                obj.name = newObjectName;
            }
            else if(obj is Object)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                AssetDatabase.RenameAsset(path, newName);
            }
        }

        AssetDatabase.Refresh();
    }
    private string GetNewName(Object obj, int index)
    {
        string originalName = obj.name;
        string resultName = originalName;

        if(changeOriginalName)
        {
            resultName = baseName;
        }
        if(addPrefix)
        {
            resultName = prefix + resultName;
        }
        if(addSuffix)
        {
            resultName = resultName + suffix;
        }
        if(resultName.Length > maxCharacters)
        {
            resultName = resultName.Substring(0, maxCharacters);
        }
        if(useCaseOption)
        {
            resultName = ConvertCase(resultName, caseOption);
        }
        if(addNumbering)
        {
            string paddedNumber = (startNumber + index).ToString().PadLeft(padding, '0');

            switch(numberingStyle)
            {
                case NumberingStyle.Adjacent:
                    resultName = resultName + paddedNumber;
                    break;
                case NumberingStyle.Underscore:
                    resultName = resultName + "_" + paddedNumber;
                    break;
                case NumberingStyle.Parenthesis:
                    resultName = resultName + " (" + paddedNumber + ")";
                    break;
            }
        }

        return resultName;
    }
    private string ConvertCase(string text, CaseOption caseOption)
    {
        if(string.IsNullOrEmpty(text)) return text;

        string[] words = SplitIntoWords(text);

        switch(caseOption)
        {
            case CaseOption.Lowercase:
                return text.ToLower();

            case CaseOption.Uppercase:
                return text.ToUpper();

            case CaseOption.Titlecase:
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                return textInfo.ToTitleCase(text.ToLower());

            case CaseOption.Camelcase:
                StringBuilder camelCase = new StringBuilder();
                for(int i = 0; i < words.Length; i++)
                {
                    if(string.IsNullOrEmpty(words[i])) continue;

                    if(i == 0) camelCase.Append(words[i].ToLower());
                    else camelCase.Append(char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower());
                }
                return camelCase.ToString();

            case CaseOption.Pascalcase:
                StringBuilder pascalCase = new StringBuilder();
                foreach(string word in words)
                {
                    if(string.IsNullOrEmpty(word)) continue;

                    pascalCase.Append(char.ToUpper(word[0]) + word.Substring(1).ToLower());
                }
                return pascalCase.ToString();

            case CaseOption.Kebabcase:
                return string.Join("-", ConvertWordsToLower(words));

            case CaseOption.Snakecase:
                return string.Join("_", ConvertWordsToLower(words));

            case CaseOption.UpperSnakecase:
                return string.Join("_", ConvertWordsToUpper(words));

            case CaseOption.Traincase:
                string[] titleCaseWords = new string[words.Length];
                for(int i = 0; i < words.Length; i++)
                {
                    if(string.IsNullOrEmpty(words[i])) continue;

                    titleCaseWords[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
                return string.Join("-", titleCaseWords);
            default:
                return text;
        }
    }
    private string[] SplitIntoWords(string text)
    {
        if(string.IsNullOrEmpty(text)) return new string[0];

        string spaceSeparated = text
            .Replace("_", " ")
            .Replace("-", " ")
            .Replace(".", " ");

        spaceSeparated = Regex.Replace(spaceSeparated, "([a-z])([A-Z])", "$1 $2");

        string[] words = spaceSeparated.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        for(int i = 0;  i < words.Length; i++)
        {
            words[i] = words[i].Trim();
        }

        return words;
    }
    private string[] ConvertWordsToLower(string[] words)
    {
        string[] result = new string[words.Length];
        for(int i = 0; i < words.Length; i++)
        {
            result[i] = words[i].ToLower();
        }
        return result;
    }
    private string[] ConvertWordsToUpper(string[] words)
    {
        string[] result = new string[words.Length];
        for(int i = 0; i < words.Length; i++)
        {
            result[i] = words[i].ToUpper();
        }
        return result;
    }
}
