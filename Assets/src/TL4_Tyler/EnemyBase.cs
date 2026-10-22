using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum EnemyMovementType { Static, Flying, Ground }
public enum EnemyAttackMode { MeleeOnly, RangedOnly, Both }

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Combat identity")]
    [SerializeField] private EnemyAttackMode attackMode = EnemyAttackMode.Both;
    [SerializeField, Min(1)] private int level = 1;

    [Header("Base stats (level 1)")]
    [FormerlySerializedAs("hp"), FormerlySerializedAs("healthPoints")]
    [SerializeField, Min(1)] private int baseHealth = 100;
    [SerializeField, Min(0)] private int baseCoins = 5;
    [SerializeField, Min(0)] private int baseMeleeDamage = 10;
    [SerializeField, Min(0)] private int baseRangedDamage = 8;

    [Header("Added per level")]
    [SerializeField, Min(0)] private int healthPerLevel = 20;
    [SerializeField, Min(0)] private int coinsPerLevel = 2;
    [SerializeField, Min(0)] private int meleeDamagePerLevel = 2;
    [SerializeField, Min(0)] private int rangedDamagePerLevel = 2;

    [Header("Movement")]
    [SerializeField] protected float speed;
    [SerializeField] protected float aggroRadius;
    protected int hp;
    private bool initialized;
    private bool defeated;

    public event Action<EnemyBase> Defeated;
    public virtual EnemyType Type => EnemyType.Ground;
    public virtual EnemyMovementType MovementType => EnemyMovementType.Ground;
    public EnemyAttackMode AttackMode => attackMode;
    public int Level => Mathf.Max(1, level);
    public int MaxHealth => Scale(baseHealth, healthPerLevel, 1);
    public int CurrentHealth { get { EnsureInitialized(); return hp; } }
    public int CoinReward => Scale(baseCoins, coinsPerLevel, 0);
    public bool CanMelee => attackMode == EnemyAttackMode.MeleeOnly || attackMode == EnemyAttackMode.Both;
    public bool CanRanged => attackMode == EnemyAttackMode.RangedOnly || attackMode == EnemyAttackMode.Both;
    public int MeleeDamage => CanMelee ? Scale(baseMeleeDamage, meleeDamagePerLevel, 0) : 0;
    public int RangedDamage => CanRanged ? Scale(baseRangedDamage, rangedDamagePerLevel, 0) : 0;
    public bool IsDefeated => defeated;

    protected virtual void Awake() { EnsureInitialized(); }

    private void EnsureInitialized()
    {
        if (initialized) return;
        hp = MaxHealth;
        initialized = true;
    }

    // Linear scaling from the original base values, never from previously scaled stats.
    private int Scale(int baseline, int increment, int minimum)
    {
        long value = Math.Max(minimum, baseline) + (long)Math.Max(0, increment) * (Level - 1);
        return (int)Math.Min(int.MaxValue, value);
    }

    // Retain the health percentage when difficulty changes; never resurrect a defeated enemy.
    public void SetLevel(int value)
    {
        if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
        EnsureInitialized();
        int oldMaximum = MaxHealth;
        level = value;
        hp = defeated ? 0 : (int)Math.Min(MaxHealth, Math.Ceiling((double)hp / oldMaximum * MaxHealth));
    }

    public void SetAttackMode(EnemyAttackMode value)
    {
        if (!Enum.IsDefined(typeof(EnemyAttackMode), value)) throw new ArgumentOutOfRangeException(nameof(value));
        attackMode = value;
    }

    // The shared gate prevents melee-only or defeated enemies from firing.
    public bool TryGetShotDirection(Vector2 origin, Vector2 target, out Vector2 direction)
    {
        direction = Vector2.zero;
        if (!CanRanged || IsDefeated || !Finite(origin) || !Finite(target)) return false;
        return CalculateShotDirection(origin, target, out direction);
    }

    protected virtual bool CalculateShotDirection(Vector2 origin, Vector2 target, out Vector2 direction)
    {
        direction = (target - origin).normalized;
        return direction.sqrMagnitude > 0;
    }

    private static bool Finite(Vector2 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
    }

    public virtual void Move()
    {
        // TODO: Read the TL6 player position when that integration is available.
    }

    public virtual string Attack()
    {
        // Gameplay chooses an allowed attack and applies MeleeDamage or RangedDamage to TL6.
        return name + " attacks";
    }

    // CoinReward is available to Defeated listeners; this class does not credit a wallet.
    public bool TakeDamage(int amount)
    {
        EnsureInitialized();
        if (defeated) return true;
        hp = Mathf.Max(0, hp - Mathf.Max(0, amount));
        if (hp == 0)
        {
            defeated = true;
            Defeated?.Invoke(this);
        }
        return defeated;
    }
}
