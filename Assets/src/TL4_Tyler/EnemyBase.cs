using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum EnemyMovementType { Static, Flying, Ground }
public enum EnemyAttackMode { MeleeOnly, RangedOnly, Both }
public enum EnemyAggroRange { Close, Medium, Far }
public enum EnemyAwareness { Low, High }

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
    [Header("Aggro detection")]
    [SerializeField] private EnemyAwareness awareness = EnemyAwareness.High;
    [FormerlySerializedAs("awarenessRefreshInterval")]
    [SerializeField, Min(.1f)] private float awarenessUpgradeInterval = 6;
    [SerializeField, Range(0, 1)] private float awarenessUpgradeChance = .5f;
    [Tooltip("Runtime choice: Low uses Close/Medium; High uses Medium/Far.")]
    [SerializeField] private EnemyAggroRange currentAggroRange = EnemyAggroRange.Medium;
    private float awarenessElapsed;
    private Transform aggroTarget;
    public Transform AggroTarget => aggroTarget;
    public virtual Vector3 AttackTargetPosition => aggroTarget != null ? aggroTarget.position : transform.position;
    public virtual bool CanAttackTarget => true;
    public virtual bool CanStartAttack => CanAttackTarget;
    public bool HasAggro => !IsDefeated && aggroTarget != null;
    [SerializeField, Min(0)] private float closeAggroRadius = 6;
    [SerializeField, Min(0)] private float mediumAggroRadius = 16;
    [SerializeField, Min(0)] private float farAggroRadius = 32;
    // Retain the legacy field for existing subclasses; pursuit uses ActiveAggroRadius.
    [SerializeField, HideInInspector] protected float aggroRadius;
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
    public EnemyAggroRange AggroRange => currentAggroRange;
    public EnemyAwareness Awareness => awareness;
    public float CloseAggroRadius => Mathf.Max(0, closeAggroRadius);
    public float MediumAggroRadius => Mathf.Max(CloseAggroRadius, mediumAggroRadius);
    public float FarAggroRadius => Mathf.Max(MediumAggroRadius, farAggroRadius);
    public float ActiveAggroRadius
    {
        get
        {
            switch (currentAggroRange)
            {
                case EnemyAggroRange.Close: return CloseAggroRadius;
                case EnemyAggroRange.Medium: return MediumAggroRadius;
                case EnemyAggroRange.Far: return FarAggroRadius;
                default: return MediumAggroRadius;
            }
        }
    }

    public void SetAwareness(EnemyAwareness value)
    {
        if (!Enum.IsDefined(typeof(EnemyAwareness), value)) throw new ArgumentOutOfRangeException(nameof(value));
        awareness = value;
        ChooseAwarenessRange();
    }

    private void ChooseAwarenessRange()
    {
        bool wider = UnityEngine.Random.value >= .5f;
        currentAggroRange = awareness == EnemyAwareness.High
            ? (wider ? EnemyAggroRange.Far : EnemyAggroRange.Medium)
            : (wider ? EnemyAggroRange.Medium : EnemyAggroRange.Close);
        awarenessElapsed = 0;
    }

    // Awareness category stays fixed. Only the lower range can upgrade, never downgrade.
    public void TickAwareness(float deltaTime)
    {
        if (IsDefeated || currentAggroRange == UpperAwarenessRange || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0) return;
        awarenessElapsed += deltaTime;
        if (awarenessElapsed < Mathf.Max(.1f, awarenessUpgradeInterval)) return;
        awarenessElapsed = 0;
        if (UnityEngine.Random.value < awarenessUpgradeChance) currentAggroRange = UpperAwarenessRange;
    }

    private EnemyAggroRange UpperAwarenessRange => awareness == EnemyAwareness.High
        ? EnemyAggroRange.Far : EnemyAggroRange.Medium;

    // Once acquired, distance can no longer break aggro for this enemy's lifetime.
    public bool TryAcquireTarget(Transform candidate)
    {
        if (IsDefeated) return false;
        if (aggroTarget == null && CanDetectTarget(candidate)) aggroTarget = candidate;
        return HasAggro && aggroTarget.gameObject.activeInHierarchy;
    }

    protected virtual void Update() { TickAwareness(Time.deltaTime); }

    public bool CanDetectTarget(Transform target)
    {
        return !IsDefeated && target != null && target.gameObject.activeInHierarchy &&
            ((Vector2)(target.position - transform.position)).sqrMagnitude <= ActiveAggroRadius * ActiveAggroRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, CloseAggroRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, MediumAggroRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, FarAggroRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, ActiveAggroRadius);
    }

    protected virtual void Awake() { EnsureInitialized(); ChooseAwarenessRange(); }

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
