using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class MultiRenamerWindow : EditorWindow
{
    [System.Serializable]
    private class RenamerPreset
    {
        public string presetName;

        public bool changeOriginalName;
        public string baseName;

        public bool addPrefix;
        public string prefix;

        public bool addSuffix;
        public string suffix;

        public bool trim;
        public bool trimStart;
        public int trimStartChars;
        public bool trimEnd;
        public int trimEndChars;
        public bool trimUnityNumbering;

        public bool addNumbering;
        public NumberingStyle numberingStyle;
        public int startNumber;
        public int padding;

        public bool useCaseOption;
        public CaseOption caseOption;
    }
    [System.Serializable]
    private class PresetsContainer
    {
        public List<RenamerPreset> presets;
    }
    private static class Layout
    {
        public const float MIN_WINDOW_WIDTH = 400f;
        public const float MIN_WINDOW_HEIGHT = 500f;
        public const float SPACE = 5f;
        public const float BUTTON_HEIGHT = 40f;
        public const float PREVIEW_HEIGHT = 55f;
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
    private bool trim;
    private bool trimStart;
    private int trimStartChars;
    private bool trimEnd;
    private int trimEndChars;
    private bool trimUnityNumbering;
    private bool addNumbering;
    private string numbering = string.Empty;
    private NumberingStyle numberingStyle;
    private int startNumber = 1;
    private int padding = 2;
    private bool useCaseOption;
    private CaseOption caseOption;
    private bool isStylesInitDone;
    private Vector2 scrollPos;

    private List<RenamerPreset> savedPresets = new List<RenamerPreset>();
    private string newPresetName = "New Preset";
    private int selectedPresetIndex = -1;
    private bool presetsUnfolded = true;

    private GUIStyle buttonStyle;
    private Color buttonColor = new Color(0.74f, 0.74f, 0.74f);
    private GUIStyle previewStyle;
    private Color xButtonColor = new Color(0.93f, 0.38f, 0.34f);

    private const string EDITOR_KEY_PRESETS = "MULTI_RENAMER_PRESETS";

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
        LoadPresetsFromEditorPrefs();
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
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawBaseName();
        GUILayout.Space(Layout.SPACE);
        DrawPrefix();
        GUILayout.Space(Layout.SPACE);
        DrawSuffix();
        GUILayout.Space(Layout.SPACE);
        DrawTrimming();
        GUILayout.Space(Layout.SPACE);
        DrawNumbering();
        GUILayout.Space(Layout.SPACE);
        DrawCaseOption();
        GUILayout.Space(Layout.SPACE);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        DrawPresets();
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
        previewStyle.alignment = TextAnchor.MiddleCenter;
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
    private void DrawPresets()
    {
        EditorGUILayout.BeginVertical("Box");
        presetsUnfolded = EditorGUILayout.Foldout(presetsUnfolded, "Presets", true);
        if(presetsUnfolded)
        {
            // Main preset controls area
            EditorGUILayout.BeginHorizontal();

            // Left side - inputs and dropdown
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // New preset name field
            EditorGUILayout.BeginHorizontal();
            newPresetName = EditorGUILayout.TextField("New Preset Name", newPresetName);
            EditorGUILayout.EndHorizontal();

            // Saved presets dropdown with delete button
            EditorGUILayout.BeginHorizontal();

            string[] presetNames = savedPresets.Count > 0 ? savedPresets.Select(p => p.presetName).ToArray() : new string[] { "No presets" };

            bool hasPresets = savedPresets.Count > 0;
            GUI.enabled = hasPresets;

            int newSelectedIndex = EditorGUILayout.Popup("Saved Presets", selectedPresetIndex >= 0 ? selectedPresetIndex : 0, presetNames, GUILayout.ExpandWidth(true));

            if(hasPresets && newSelectedIndex != selectedPresetIndex)
            {
                selectedPresetIndex = newSelectedIndex;
            }

            // Delete button
            if(hasPresets)
            {
                GUI.enabled = selectedPresetIndex >= 0;
                GUI.color = xButtonColor;
                if(GUILayout.Button("X", GUILayout.Width(25f), GUILayout.Height(20f)))
                {
                    if(selectedPresetIndex >= 0 && selectedPresetIndex < savedPresets.Count)
                    {
                        savedPresets.RemoveAt(selectedPresetIndex);
                        if(savedPresets.Count > 0)
                        {
                            selectedPresetIndex = 0;
                        }
                        else
                        {
                            selectedPresetIndex = -1;
                        }
                        SavePresetsToEditorPrefs();
                    }
                }
                GUI.color = Color.white;
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Right side - action buttons
            EditorGUILayout.BeginVertical(GUILayout.Width(74f)); // Fixed width for buttons + spacing

            GUI.color = buttonColor;
            if(GUILayout.Button("SAVE", buttonStyle, GUILayout.Width(70f), GUILayout.Height(20f)))
            {
                SaveCurrentAsPreset();
            }

            GUI.enabled = hasPresets && selectedPresetIndex >= 0;
            if(GUILayout.Button("LOAD", buttonStyle, GUILayout.Width(70f), GUILayout.Height(20f)))
            {
                if(selectedPresetIndex >= 0 && selectedPresetIndex < savedPresets.Count)
                {
                    LoadPreset(savedPresets[selectedPresetIndex]);
                }
            }
            GUI.enabled = true;
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawBaseName()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        changeOriginalName = EditorGUILayout.ToggleLeft("Change Original Name", changeOriginalName);
        if(changeOriginalName)
        {
            EditorGUI.indentLevel++;
            baseName = EditorGUILayout.TextField(new GUIContent("New Base Name"), baseName);
            EditorGUI.indentLevel--;
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
            EditorGUI.indentLevel++;
            prefix = EditorGUILayout.TextField(new GUIContent("Prefix"), prefix);
            EditorGUI.indentLevel--;
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
            EditorGUI.indentLevel++;
            suffix = EditorGUILayout.TextField(new GUIContent("Suffix"), suffix);
            EditorGUI.indentLevel--;
        }
        else
        {
            suffix = string.Empty;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawTrimming()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        trim = EditorGUILayout.ToggleLeft("Trimming", trim);
        if(trim)
        {
            EditorGUI.indentLevel++;
            trimStart = EditorGUILayout.Toggle("Trim Start", trimStart);
            if(trimStart)
            {
                EditorGUI.indentLevel++;
                trimStartChars = EditorGUILayout.IntSlider("Amount", trimStartChars, 0, 10);
                EditorGUI.indentLevel--;
            }
            trimEnd = EditorGUILayout.Toggle("Trim End", trimEnd);
            if(trimEnd)
            {
                EditorGUI.indentLevel++;
                trimEndChars = EditorGUILayout.IntSlider("Amount", trimEndChars, 0, 10);
                EditorGUI.indentLevel--;
            }
            trimUnityNumbering = EditorGUILayout.Toggle("Trim Unity Numbering", trimUnityNumbering);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawNumbering()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        addNumbering = EditorGUILayout.ToggleLeft("Add Numbering", addNumbering);
        if(addNumbering)
        {
            EditorGUI.indentLevel++;
            numberingStyle = (NumberingStyle)EditorGUILayout.EnumPopup("Numbering Style", numberingStyle);
            startNumber = EditorGUILayout.IntField("Start Number", startNumber);
            padding = EditorGUILayout.IntSlider("Number Padding", padding, 1, 5);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawCaseOption()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        useCaseOption = EditorGUILayout.ToggleLeft("Use Case Option", useCaseOption);
        if(useCaseOption)
        {
            EditorGUI.indentLevel++;
            caseOption = (CaseOption)EditorGUILayout.EnumPopup("Case Option", caseOption);
            EditorGUI.indentLevel--;
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
        if(trim)
        {
            if(trimUnityNumbering)
            {
                resultName = Regex.Replace(resultName, @"^(.*?)(\s*\(\d+\))$", "$1");
            }
            if(trimStart && trimStartChars > 0 && resultName.Length > trimStartChars)
            {
                resultName = resultName.Substring(trimStartChars);
            }
            if(trimEnd && trimEndChars > 0 && resultName.Length > trimEndChars)
            {
                resultName = resultName.Substring(0, resultName.Length - trimEndChars);
            }
        }
        if(addPrefix)
        {
            resultName = prefix + resultName;
        }
        if(addSuffix)
        {
            resultName = resultName + suffix;
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
    private void SaveCurrentAsPreset()
    {
        if(string.IsNullOrEmpty(newPresetName))
        {
            EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid preset name.", "OK");
            return;
        }

        var preset = new RenamerPreset
        {
            presetName = newPresetName,

            changeOriginalName = changeOriginalName,
            baseName = baseName,

            addPrefix = addPrefix,
            prefix = prefix,

            addSuffix = addSuffix,
            suffix = suffix,

            trim = trim,
            trimStart = trimStart,
            trimStartChars = trimStartChars,
            trimEnd = trimEnd,
            trimEndChars = trimEndChars,
            trimUnityNumbering = trimUnityNumbering,

            addNumbering = addNumbering,
            numberingStyle = numberingStyle,
            startNumber = startNumber,
            padding = padding,

            useCaseOption = useCaseOption,
            caseOption = caseOption,
        };

        int existingIndex = savedPresets.FindIndex(p => p.presetName == newPresetName);
        if(existingIndex >= 0)
        {
            bool replace = EditorUtility.DisplayDialog("Preset Already Exists",
            $"A preset with the name '{newPresetName}' already exists. Do you want to replace it?",
            "Replace", "Cancel");

            if(replace)
            {
                savedPresets[existingIndex] = preset;
                selectedPresetIndex = existingIndex;
            }
            else
            {
                return;
            }
        }
        else
        {
            savedPresets.Add(preset);
            selectedPresetIndex = savedPresets.Count - 1;
        }

        SavePresetsToEditorPrefs();
    }
    private void LoadPreset(RenamerPreset preset)
    {
        changeOriginalName = preset.changeOriginalName;
        baseName = preset.baseName;

        addPrefix = preset.addPrefix;
        prefix = preset.prefix;

        addSuffix = preset.addSuffix;
        suffix = preset.suffix;

        trim = preset.trim;
        trimStart = preset.trimStart;
        trimStartChars = preset.trimStartChars;
        trimEnd = preset.trimEnd;
        trimEndChars = preset.trimEndChars;
        trimUnityNumbering = preset.trimUnityNumbering;

        addNumbering = preset.addNumbering;
        numberingStyle = preset.numberingStyle;
        startNumber = preset.startNumber;
        padding = preset.padding;

        useCaseOption = preset.useCaseOption;
        caseOption = preset.caseOption;
    }
    private void SavePresetsToEditorPrefs()
    {
        var container = new PresetsContainer { presets = savedPresets };
        string json = JsonUtility.ToJson(container);
        EditorPrefs.SetString(EDITOR_KEY_PRESETS, json);
    }
    private void LoadPresetsFromEditorPrefs()
    {
        if(EditorPrefs.HasKey(EDITOR_KEY_PRESETS))
        {
            string json = EditorPrefs.GetString(EDITOR_KEY_PRESETS);
            var container = JsonUtility.FromJson<PresetsContainer>(json);

            if(container != null && container.presets != null)
            {
                savedPresets = container.presets;
            }
        }
    }
}