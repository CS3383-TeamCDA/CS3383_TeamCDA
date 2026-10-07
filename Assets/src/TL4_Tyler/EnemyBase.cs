using System;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class EnemyBase : MonoBehaviour
{
    [FormerlySerializedAs("healthPoints")]
    [SerializeField] protected int hp = 100;
    [SerializeField] protected float speed;
    [SerializeField] protected float aggroRadius;
    private bool defeated;

    public event Action<EnemyBase> Defeated;
    public EnemyType Type { get; internal set; }

    public virtual void Move()
    {
        // TODO: Read the TL6 player position when that integration is available.
    }

    public virtual string Attack()
    {
        // TODO: Call TL6 PlayerHealth.TakeDamage when combat is connected.
        return name + " attacks";
    }

    // Returns true when defeated; raises the notification only on the first defeat.
    public bool TakeDamage(int amount)
    {
        if (defeated)
        {
            return true;
        }

        hp = Mathf.Max(0, hp - Mathf.Max(0, amount));
        if (hp == 0)
        {
            defeated = true;
            Defeated?.Invoke(this);
        }

        return defeated;
    }
}
