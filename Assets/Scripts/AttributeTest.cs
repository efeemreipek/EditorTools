using UnityEngine;

public class AttributeTest : MonoBehaviour
{
    [Header("ShowIf")]
    public bool isAlive = false;
    [ShowIf("isAlive")] public float health = 0f;

    [Header("RangeWithStep")]
    [RangeWithStep(0f, 5f, 0.5f)] public float mana = 0f;

    [Header("ReadOnly")]
    [ReadOnly] public Vector2 readOnlyTest;

    [Header("MinMaxSlider")]
    [Button("Click this", "PrivateButtonTest")]
    [MinMaxSlider(0f, 5f)] public Vector2 minMaxTestVector2;
    [MinMaxSlider(0f, 5f)] public Vector2Int minMaxTestVector2Int;

    [Header("Clamp")]
    [Clamp(0f, 10f)] public float clampTest;

    public void PublicButtonTest()
    {
        Debug.Log("PublicButton is clicked");
    }

    private void PrivateButtonTest()
    {
        Debug.Log("PrivateButton is clicked");
    }

    [Header("FoldoutGroup")]
    [FoldoutGroup("Test")] public float foldoutTestFloat = 1f;
    [Button("PublicButtonTest")]
    [FoldoutGroup("Test")] public Vector2 foldoutTestV2;
    [FoldoutGroup("Test")] public string foldoutTestString;


    [Header("ConditionalShow")]
    public bool showAdvanced = false;
    [ConditionalShow("showAdvanced")] public float advancedValue = 1.0f;
    [ConditionalShow("showAdvanced")] public string advancedText = "Hidden by default";

    public enum WeaponType { Melee, Ranged, Magic }
    [Header("Enum Condition")]
    public WeaponType weaponType = WeaponType.Melee;
    [ConditionalShow("weaponType", WeaponType.Ranged)] public float range = 10f;
    [ConditionalShow("weaponType", WeaponType.Magic)] public int manaCost = 5;

    [Header("Integer Condition")]
    public int playerLevel = 1;
    [ConditionalShow("playerLevel", 10)] public string specialAbility = "Unlocked at level 10";

    [Header("Inverse Condition")]
    public bool isPlayer = true;
    [ConditionalShow("isPlayer", inverse: true)] public float aiDifficulty = 0.5f;

    [Header("String Condition")]
    public string gameMode = "Normal";
    [ConditionalShow("gameMode", "Debug")] public bool showDebugInfo = false;

    [Header("Required")]
    [Required("Transform must be assigned!")]
    public Transform playerTransform;
    [Required("Player name cannot be empty!")]
    public string playerName;
    [Required("Health must be greater than 0")]
    public int maxHealth;
    [Required("Speed is required")]
    public float movementSpeed;
}
