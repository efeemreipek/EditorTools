using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class MultiRenamer : EditorWindow
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
        public int trimStartAmount;
        public bool trimEnd;
        public int trimEndAmount;
        public bool trimUnityNumbering;

        public bool addNumbering;
        public Numbering numberingStyle;
        public int startNumber;
        public int padding;

        public bool useCaseOption;
        public Case caseStyle;
    }
    [System.Serializable]
    private class PresetsContainer
    {
        public List<RenamerPreset> presets;
    }

    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Tools/UI Toolkit/MultiRenamer")]
    public static void ShowExample()
    {
        MultiRenamer wnd = GetWindow<MultiRenamer>("MultiRenamer");
        wnd.minSize = new Vector2(400, 600);
    }

    private const string EDITOR_KEY_PRESETS = "MULTI_RENAMER_PRESETS";

    private string newName;

    // header
    private GroupBox headerGroup;
    private Label headerPreviewLabel;
    private HelpBox helpBox;

    // scroll view
    private ScrollView scrollView;

    // options
    private Toggle changeOriginalNameToggle, addPrefixToggle, addSuffixToggle, trimmingToggle, addNumberingToggle, useCaseOptionToggle;
    private bool changeOriginalName, addPrefix, addSuffix, trim, addNumbering, useCaseOption;
    private GroupBox changeOriginalNameGroup, addPrefixGroup, addSuffixGroup, trimmingGroup, addNumberingGroup, useCaseOptionGroup;

    // original name
    private string baseName;
    private TextField baseNameText;

    // prefix
    private string prefix;
    private TextField prefixText;

    // suffix
    private string suffix;
    private TextField suffixText;

    // trimming
    private Toggle trimStartToggle, trimEndToggle, trimUnityNumberingToggle;
    private bool trimStart, trimEnd, trimUnityNumbering;
    private SliderInt trimStartSlider, trimEndSlider;
    private int trimStartAmount, trimEndAmount;

    // numbering
    private string numbering;
    private enum Numbering
    {
        [InspectorName("{NAME}###")]
        Adjacent,
        [InspectorName("{NAME}_###")]
        Underscore,
        [InspectorName("{NAME} ###")]
        Space,
        [InspectorName("{NAME} (###)")]
        Parenthesis

    }
    private Numbering numberingStyle;
    private int startNumber = 1;
    private int padding = 2;
    private EnumField numberingDropdown;
    private UnsignedIntegerField startNumberText;
    private SliderInt numberPaddingSlider;

    // case
    private enum Case
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
    private Case caseStyle;
    private EnumField caseDropdown;

    // presets
    private List<RenamerPreset> savedPresets = new List<RenamerPreset>();
    private List<string> presetNames = new List<string>();
    private string newPresetName;
    private int selectedPresetIndex = -1;
    private Foldout presetsFoldout;
    private TextField newPresetNameText;
    private Button saveButton, loadButton, deleteButton;
    private DropdownField savedPresetsDropdown;
    private GroupBox savedPresetsGroup;

    // apply button
    private Button applyButton;

    public void CreateGUI()
    {
        VisualElement root = m_VisualTreeAsset.Instantiate();
        root.style.flexGrow = 1;
        rootVisualElement.Add(root);
        rootVisualElement.style.flexGrow = 1;

        // header
        headerGroup = root.Q<GroupBox>("header");
        headerPreviewLabel = root.Q<Label>("header-preview-label");

        // scroll view
        scrollView = root.Q<ScrollView>("scroll-view");

        // options
        changeOriginalNameToggle = root.Q<Toggle>("change-original-name-toggle");
        addPrefixToggle = root.Q<Toggle>("prefix-toggle");
        addSuffixToggle = root.Q<Toggle>("suffix-toggle");
        trimmingToggle = root.Q<Toggle>("trimming-toggle");
        addNumberingToggle = root.Q<Toggle>("numbering-toggle");
        useCaseOptionToggle = root.Q<Toggle>("case-toggle");
        changeOriginalNameGroup = root.Q<GroupBox>("change-original-name-group");
        addPrefixGroup = root.Q<GroupBox>("prefix-group");
        addSuffixGroup = root.Q<GroupBox>("suffix-group");
        trimmingGroup = root.Q<GroupBox>("trimming-group");
        addNumberingGroup = root.Q<GroupBox>("numbering-group");
        useCaseOptionGroup = root.Q<GroupBox>("case-group");

        changeOriginalNameToggle.RegisterValueChangedCallback(ChangeOriginalNameToggleChanged);
        addPrefixToggle.RegisterValueChangedCallback(AddPrefixToggleChanged);
        addSuffixToggle.RegisterValueChangedCallback(AddSuffixToggleChanged);
        trimmingToggle.RegisterValueChangedCallback(TrimmingToggleChanged);
        addNumberingToggle.RegisterValueChangedCallback(AddNumberingToggleChanged);
        useCaseOptionToggle.RegisterValueChangedCallback(UseCaseOptionToggleChanged);

        // original name
        baseNameText = root.Q<TextField>("change-original-name-text");
        baseNameText.RegisterValueChangedCallback(BaseNameTextChanged);

        // prefix
        prefixText = root.Q<TextField>("prefix-text");
        prefixText.RegisterValueChangedCallback(PrefixTextChanged);

        // suffix
        suffixText = root.Q<TextField>("suffix-text");
        suffixText.RegisterValueChangedCallback(SuffixTextChanged);

        // trimming
        trimStartToggle = root.Q<Toggle>("trim-start-toggle");
        trimEndToggle = root.Q<Toggle>("trim-end-toggle");
        trimUnityNumberingToggle = root.Q<Toggle>("trim-unity-numbering-toggle");
        trimStartSlider = root.Q<SliderInt>("trim-start-slider");
        trimEndSlider = root.Q<SliderInt>("trim-end-slider");

        trimStartToggle.RegisterValueChangedCallback(TrimStartToggleChanged);
        trimEndToggle.RegisterValueChangedCallback(TrimEndToggleChanged);
        trimUnityNumberingToggle.RegisterValueChangedCallback(TrimUnityNumberingToggleChanged);
        trimStartSlider.RegisterValueChangedCallback(TrimStartSliderChanged);
        trimEndSlider.RegisterValueChangedCallback(TrimEndSliderChanged);

        // numbering
        numberingDropdown = root.Q<EnumField>("numbering-style-dropdown");
        startNumberText = root.Q<UnsignedIntegerField>("numbering-start-number-int");
        numberPaddingSlider = root.Q<SliderInt>("numbering-padding-slider");

        numberingDropdown.RegisterValueChangedCallback(NumberingStyleDropdownChanged);
        startNumberText.RegisterValueChangedCallback(StartNumberTextChanged);
        numberPaddingSlider.RegisterValueChangedCallback(NumberPaddingSliderChanged);

        numberingDropdown.value = numberingStyle;
        startNumberText.value = (uint)startNumber;
        numberPaddingSlider.value = padding;

        // case
        caseDropdown = root.Q<EnumField>("case-dropdown");
        caseDropdown.RegisterValueChangedCallback(CaseOptionDropdownChanged);

        caseDropdown.value = caseStyle;

        // presets
        presetsFoldout = root.Q<Foldout>("presets-foldout");
        newPresetNameText = root.Q<TextField>("new-preset-text");
        savedPresetsDropdown = root.Q<DropdownField>("saved-presets-dropdown");
        saveButton = root.Q<Button>("save-preset-button");
        loadButton = root.Q<Button>("load-preset-button");
        deleteButton = root.Q<Button>("delete-preset-button");
        savedPresetsGroup = root.Q<GroupBox>("saved-presets-group");

        newPresetNameText.RegisterValueChangedCallback(NewPresetNameTextChanged);
        savedPresetsDropdown.RegisterValueChangedCallback(SavedPresetsDropdownChanged);
        saveButton.clicked += SaveButtonClicked;
        loadButton.clicked += LoadButtonClicked;
        deleteButton.clicked += DeleteButtonClicked;

        // apply button
        applyButton = root.Q<Button>("apply-button");
        applyButton.clicked += ApplyButtonClicked;

        InitializePresetsUI();

        CreateHelpBox();
        CheckSelection();
    }
    private void Update()
    {
        if(presetsFoldout.value)
        {
            UpdatePresetsUIState();
        }
    }

    private void OnEnable()
    {
        Selection.selectionChanged += CheckSelection;
        LoadPresetsFromEditorPrefs();
    }
    private void OnDisable()
    {
        Selection.selectionChanged -= CheckSelection;
    }

    private void ChangeOriginalNameToggleChanged(ChangeEvent<bool> evt)
    {
        changeOriginalName = evt.newValue;
        changeOriginalNameGroup.style.display = changeOriginalName ? DisplayStyle.Flex : DisplayStyle.None;
        GetPreviewLabelText();
    }
    private void AddPrefixToggleChanged(ChangeEvent<bool> evt)
    {
        addPrefix = evt.newValue;
        addPrefixGroup.style.display = addPrefix ? DisplayStyle.Flex : DisplayStyle.None;
        GetPreviewLabelText();
    }
    private void AddSuffixToggleChanged(ChangeEvent<bool> evt)
    {
        addSuffix = evt.newValue;
        addSuffixGroup.style.display = addSuffix ? DisplayStyle.Flex : DisplayStyle.None;
        GetPreviewLabelText();
    }
    private void TrimmingToggleChanged(ChangeEvent<bool> evt)
    {
        trim = evt.newValue;
        trimmingGroup.style.display = trim ? DisplayStyle.Flex : DisplayStyle.None;
        GetPreviewLabelText();
    }
    private void AddNumberingToggleChanged(ChangeEvent<bool> evt)
    {
        addNumbering = evt.newValue;
        addNumberingGroup.style.display = addNumbering ? DisplayStyle.Flex : DisplayStyle.None;
        GetPreviewLabelText();
    }
    private void UseCaseOptionToggleChanged(ChangeEvent<bool> evt)
    {
        useCaseOption = evt.newValue;
        useCaseOptionGroup.style.display = useCaseOption ? DisplayStyle.Flex : DisplayStyle.None;
        GetPreviewLabelText();
    }
    private void BaseNameTextChanged(ChangeEvent<string> evt)
    {
        baseName = evt.newValue;
        GetPreviewLabelText();
    }
    private void PrefixTextChanged(ChangeEvent<string> evt)
    {
        prefix = evt.newValue;
        GetPreviewLabelText();
    }
    private void SuffixTextChanged(ChangeEvent<string> evt)
    {
        suffix = evt.newValue;
        GetPreviewLabelText();
    }
    private void TrimStartToggleChanged(ChangeEvent<bool> evt)
    {
        trimStart = evt.newValue;
        trimStartSlider.style.display = trimStart ? DisplayStyle.Flex : DisplayStyle.None;
        GetPreviewLabelText();
    }
    private void TrimEndToggleChanged(ChangeEvent<bool> evt)
    {
        trimEnd = evt.newValue;
        trimEndSlider.style.display = trimEnd ? DisplayStyle.Flex : DisplayStyle.None;
        GetPreviewLabelText();
    }
    private void TrimUnityNumberingToggleChanged(ChangeEvent<bool> evt)
    {
        trimUnityNumbering = evt.newValue;
        GetPreviewLabelText();
    }
    private void TrimStartSliderChanged(ChangeEvent<int> evt)
    {
        trimStartAmount = evt.newValue;
        GetPreviewLabelText();
    }
    private void TrimEndSliderChanged(ChangeEvent<int> evt)
    {
        trimEndAmount = evt.newValue;
        GetPreviewLabelText();
    }
    private void NumberingStyleDropdownChanged(ChangeEvent<System.Enum> evt)
    {
        numberingStyle = (Numbering)evt.newValue;
        GetPreviewLabelText();
    }
    private void StartNumberTextChanged(ChangeEvent<uint> evt)
    {
        startNumber = (int)evt.newValue;
        GetPreviewLabelText();
    }
    private void NumberPaddingSliderChanged(ChangeEvent<int> evt)
    {
        padding = evt.newValue;
        GetPreviewLabelText();
    }
    private void CaseOptionDropdownChanged(ChangeEvent<System.Enum> evt)
    {
        caseStyle = (Case)evt.newValue;
        GetPreviewLabelText();
    }
    private void NewPresetNameTextChanged(ChangeEvent<string> evt)
    {
        newPresetName = evt.newValue;
    }
    private void SavedPresetsDropdownChanged(ChangeEvent<string> evt)
    {
        selectedPresetIndex = savedPresetsDropdown.index;
        UpdatePresetsUIState();
    }
    private void SaveButtonClicked()
    {
        SaveCurrentAsPreset();

        newPresetNameText.value = string.Empty;
        selectedPresetIndex = savedPresets.Count - 1;

        UpdatePresetDropdown();
        UpdatePresetsUIState();
    }
    private void LoadButtonClicked()
    {
        if(selectedPresetIndex >= 0 && selectedPresetIndex < savedPresets.Count)
        {
            LoadPreset(savedPresets[selectedPresetIndex]);
        }
    }
    private void DeleteButtonClicked()
    {
        if(selectedPresetIndex >= 0 && selectedPresetIndex < savedPresets.Count)
        {
            savedPresets.RemoveAt(selectedPresetIndex);

            // Adjust selected index after deletion
            if(savedPresets.Count > 0)
            {
                if(selectedPresetIndex >= savedPresets.Count)
                {
                    selectedPresetIndex = savedPresets.Count - 1;
                }
            }
            else
            {
                selectedPresetIndex = -1;
            }

            UpdatePresetDropdown();
            UpdatePresetsUIState();
            SavePresetsToEditorPrefs();
        }
    }
    private void ApplyButtonClicked()
    {
        if(Selection.objects.Length > 0)
        {
            RenameObjects(Selection.objects);
        }
    }

    private void InitializePresetsUI()
    {
        UpdatePresetDropdown();
        UpdatePresetsUIState();
    }
    private void UpdatePresetsUIState()
    {
        bool hasPresets = savedPresets.Count > 0;

        savedPresetsGroup.SetEnabled(hasPresets);
        loadButton.SetEnabled(hasPresets && selectedPresetIndex >= 0 && selectedPresetIndex < savedPresets.Count);
        deleteButton.SetEnabled(hasPresets && selectedPresetIndex >= 0 && selectedPresetIndex < savedPresets.Count);
        deleteButton.style.display = hasPresets ? DisplayStyle.Flex : DisplayStyle.None;
    }
    private void UpdatePresetDropdown()
    {
        bool hasPresets = savedPresets.Count > 0;

        if(hasPresets)
        {
            presetNames = savedPresets.Select(p => p.presetName).ToList();
            savedPresetsDropdown.choices = presetNames;

            // Ensure selected index is valid
            if(selectedPresetIndex >= savedPresets.Count)
            {
                selectedPresetIndex = savedPresets.Count - 1;
            }
            if(selectedPresetIndex < 0 && savedPresets.Count > 0)
            {
                selectedPresetIndex = 0;
            }

            savedPresetsDropdown.index = selectedPresetIndex;
        }
        else
        {
            savedPresetsDropdown.choices = new List<string> { "No presets" };
            savedPresetsDropdown.index = -1;
            selectedPresetIndex = -1;
        }
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
            trimStartAmount = trimStartAmount,
            trimEnd = trimEnd,
            trimEndAmount = trimEndAmount,
            trimUnityNumbering = trimUnityNumbering,

            addNumbering = addNumbering,
            numberingStyle = numberingStyle,
            startNumber = startNumber,
            padding = padding,

            useCaseOption = useCaseOption,
            caseStyle = caseStyle,
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
        trimStartAmount = preset.trimStartAmount;
        trimEnd = preset.trimEnd;
        trimEndAmount = preset.trimEndAmount;
        trimUnityNumbering = preset.trimUnityNumbering;

        addNumbering = preset.addNumbering;
        numberingStyle = preset.numberingStyle;
        startNumber = preset.startNumber;
        padding = preset.padding;

        useCaseOption = preset.useCaseOption;
        caseStyle = preset.caseStyle;

        UpdateUIFromLoadedPreset();
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
    private void UpdateUIFromLoadedPreset()
    {
        // Update toggles
        changeOriginalNameToggle.value = changeOriginalName;
        addPrefixToggle.value = addPrefix;
        addSuffixToggle.value = addSuffix;
        trimmingToggle.value = trim;
        addNumberingToggle.value = addNumbering;
        useCaseOptionToggle.value = useCaseOption;

        // Update text fields
        baseNameText.value = baseName ?? "";
        prefixText.value = prefix ?? "";
        suffixText.value = suffix ?? "";

        // Update trim toggles and sliders
        trimStartToggle.value = trimStart;
        trimEndToggle.value = trimEnd;
        trimUnityNumberingToggle.value = trimUnityNumbering;
        trimStartSlider.value = trimStartAmount;
        trimEndSlider.value = trimEndAmount;

        // Update numbering controls
        numberingDropdown.value = numberingStyle;
        startNumberText.value = (uint)startNumber;
        numberPaddingSlider.value = padding;

        // Update case control
        caseDropdown.value = caseStyle;

        // Update group visibility based on toggle states
        changeOriginalNameGroup.style.display = changeOriginalName ? DisplayStyle.Flex : DisplayStyle.None;
        addPrefixGroup.style.display = addPrefix ? DisplayStyle.Flex : DisplayStyle.None;
        addSuffixGroup.style.display = addSuffix ? DisplayStyle.Flex : DisplayStyle.None;
        trimmingGroup.style.display = trim ? DisplayStyle.Flex : DisplayStyle.None;
        addNumberingGroup.style.display = addNumbering ? DisplayStyle.Flex : DisplayStyle.None;
        useCaseOptionGroup.style.display = useCaseOption ? DisplayStyle.Flex : DisplayStyle.None;

        // Update trim slider visibility
        trimStartSlider.style.display = trimStart ? DisplayStyle.Flex : DisplayStyle.None;
        trimEndSlider.style.display = trimEnd ? DisplayStyle.Flex : DisplayStyle.None;

        // Update preview
        GetPreviewLabelText();
    }
    private void RenameObjects(Object[] objects)
    {
        Undo.RecordObjects(objects, "Multi Rename");

        for(int i = 0; i < objects.Length; i++)
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
            if(trimStart && trimStartAmount > 0 && resultName.Length > trimStartAmount)
            {
                resultName = resultName.Substring(trimStartAmount);
            }
            if(trimEnd && trimEndAmount > 0 && resultName.Length > trimEndAmount)
            {
                resultName = resultName.Substring(0, resultName.Length - trimEndAmount);
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
            resultName = ConvertCase(resultName, caseStyle);
        }
        if(addNumbering)
        {
            string paddedNumber = (startNumber + index).ToString().PadLeft(padding, '0');

            switch(numberingStyle)
            {
                case Numbering.Adjacent:
                    resultName = resultName + paddedNumber;
                    break;
                case Numbering.Underscore:
                    resultName = resultName + "_" + paddedNumber;
                    break;
                case Numbering.Space:
                    resultName = resultName + " " + paddedNumber;
                    break;
                case Numbering.Parenthesis:
                    resultName = resultName + " (" + paddedNumber + ")";
                    break;
            }
        }

        return resultName;
    }
    private string ConvertCase(string text, Case caseOption)
    {
        if(string.IsNullOrEmpty(text)) return text;

        string[] words = SplitIntoWords(text);

        switch(caseOption)
        {
            case Case.Lowercase:
                return text.ToLower();

            case Case.Uppercase:
                return text.ToUpper();

            case Case.Titlecase:
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                return textInfo.ToTitleCase(text.ToLower());

            case Case.Camelcase:
                StringBuilder camelCase = new StringBuilder();
                for(int i = 0; i < words.Length; i++)
                {
                    if(string.IsNullOrEmpty(words[i])) continue;

                    if(i == 0) camelCase.Append(words[i].ToLower());
                    else camelCase.Append(char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower());
                }
                return camelCase.ToString();

            case Case.Pascalcase:
                StringBuilder pascalCase = new StringBuilder();
                foreach(string word in words)
                {
                    if(string.IsNullOrEmpty(word)) continue;

                    pascalCase.Append(char.ToUpper(word[0]) + word.Substring(1).ToLower());
                }
                return pascalCase.ToString();

            case Case.Kebabcase:
                return string.Join("-", ConvertWordsToLower(words));

            case Case.Snakecase:
                return string.Join("_", ConvertWordsToLower(words));

            case Case.UpperSnakecase:
                return string.Join("_", ConvertWordsToUpper(words));

            case Case.Traincase:
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

        for(int i = 0; i < words.Length; i++)
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
    private void CheckSelection()
    {
        if(Selection.objects.Length > 0)
        {
            helpBox.style.display = DisplayStyle.None;
            headerPreviewLabel.style.display = DisplayStyle.Flex;

            scrollView.SetEnabled(true);
            presetsFoldout.SetEnabled(true);

            GetPreviewLabelText();
        }
        else
        {
            headerPreviewLabel.style.display = DisplayStyle.None;
            helpBox.style.display = DisplayStyle.Flex;

            scrollView.SetEnabled(false);
            presetsFoldout.SetEnabled(false);
        }
    }
    private void GetPreviewLabelText()
    {
        string previewName = GetNewName(Selection.objects[0], 0);
        headerPreviewLabel.text = previewName;
    }
    private void CreateHelpBox()
    {
        helpBox = new HelpBox("Select objects to preview renaming", HelpBoxMessageType.Info);
        helpBox.style.display = DisplayStyle.None;
        headerGroup.Add(helpBox);
    }
}
