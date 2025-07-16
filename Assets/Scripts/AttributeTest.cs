using UnityEngine;

public class AttributeTest : MonoBehaviour
{
    [SerializeField] private bool isAlive = false;
    [SerializeField, ShowIf("isAlive")] private float health = 0f;
    [SerializeField, RangeWithStep(0f, 5f, 0.5f)] private float mana = 0f;
}
