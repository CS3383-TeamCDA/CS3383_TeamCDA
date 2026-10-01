using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] private int healthPoints = 100;
    [SerializeField] private float speed;
    [SerializeField] private float aggroRadius;

    public virtual string Attack()
    {
        return name + " attacks";
    }

    // Returns true when the enemy has no health remaining.
    public bool TakeDamage(int damageAmount)
    {
        healthPoints = Mathf.Max(0, healthPoints - Mathf.Max(0, damageAmount));
        return healthPoints == 0;
    }
}
