using UnityEngine;

public class AttributeTest : MonoBehaviour
{
    [SerializeField] private bool isAlive = false;
    [SerializeField, ShowIf("isAlive")] private float health = 0f;
    [SerializeField] private float mana = 0f;
}
