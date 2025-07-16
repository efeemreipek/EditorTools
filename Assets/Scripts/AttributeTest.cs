using UnityEngine;

public class AttributeTest : MonoBehaviour
{
    [SerializeField] private bool isAlive = false;
    [SerializeField, ShowIf("isAlive")] private float health = 0f;
    [SerializeField, RangeWithStep(0f, 5f, 0.5f)] private float mana = 0f;
    [SerializeField, ReadOnly] private Vector2 readOnlyTest;
    [SerializeField, MinMaxSlider(0f, 5f)] private Vector2 minMaxTestVector2;
    [SerializeField, MinMaxSlider(0f, 5f)] private Vector2Int minMaxTestVector2Int;
    [SerializeField, Clamp(0f, 10f)] private float clampTest;
}
