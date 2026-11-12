using EscapeThe90s.Contracts;
using UnityEngine;

public class GrenadeBerserkerEnemy : GroundEnemy
{
    [SerializeField, Min(0)] private float rageDistance = 4;
    [SerializeField, Min(0)] private float contactDistance = 1.1f;
    [SerializeField, Min(.1f)] private float rushSpeed = 6;
    [SerializeField, Min(.1f)] private float throwInterval = 2;
    [SerializeField, Min(.1f)] private float grenadeFlightTime = 1.2f;
    [SerializeField, Min(0)] private float grenadeRadius = 2;
    [SerializeField, Min(0)] private float deathBlastRadius = 2.8f;
    [SerializeField] private TurretBossVfxSet attackVfx;
    protected override bool AvoidAllies => !IsBerserk;
    public bool IsBerserk { get; private set; }
    public bool HasExploded { get; private set; }
    private float nextThrow;
    private bool contactDetonation;
    public override EnemyType Type => EnemyType.GrenadeBerserker;
    public override bool CanStartAttack => false; // This enemy owns its attack sequence.

    protected override void Awake()
    {
        base.Awake();
        SetAttackMode(EnemyAttackMode.Both);
        Defeated += ExplodeOnDefeat;
    }
    public void EnterBerserk()
    {
        if (IsDefeated || IsBerserk) return;
        IsBerserk = true;
        speed = rushSpeed;
        IgnoreEnemyCollisions();
        var attacks = GetComponent<EnemyAttackAnimations>();
        if (attacks != null) attacks.enabled = false;
    }
    public override bool TakeDamage(int amount)
    {
        if (amount > 0) EnterBerserk();
        return base.TakeDamage(amount);
    }
    public override void Move()
    {
        if (IsBerserk)
        {
            // Include enemies spawned after rage began. Terrain/player contacts remain solid.
            IgnoreEnemyCollisions();
            base.Move();
        }
        else
        {
            // Acquire using shared pursuit, but hold position while throwing.
            float previousSpeed = speed;
            speed = 0;
            base.Move();
            speed = previousSpeed;
        }
    }
    private void IgnoreEnemyCollisions()
    {
        var ownColliders = GetComponentsInChildren<Collider2D>();
        foreach (var other in FindObjectsByType<EnemyBase>())
        {
            if (other == this) continue;
            foreach (var theirs in other.GetComponentsInChildren<Collider2D>())
                foreach (var ours in ownColliders)
                    Physics2D.IgnoreCollision(ours, theirs);
        }
    }
    protected override void Update()
    {
        base.Update();
        if (IsDefeated || !HasAggro || !AggroTarget.gameObject.activeInHierarchy) return;
        float distance = Vector2.Distance(transform.position, AggroTarget.position);
        if (distance <= rageDistance) EnterBerserk();
        if (IsBerserk)
        {
            if (distance <= contactDistance)
            {
                contactDetonation = true;
                TakeDamage(CurrentHealth);
            }
            return;
        }
        SetFacingRight(AggroTarget.position.x > transform.position.x);
        if (Time.time < nextThrow) return;
        nextThrow = Time.time + throwInterval;
        var driver = GetComponent<EnemyAnimationDriver>();
        if (driver != null) driver.TriggerRanged();
        var grenade = new GameObject("Berserker grenade");
        grenade.transform.position = transform.position + Vector3.up;
        var visual = new GameObject("Visual");
        visual.transform.SetParent(grenade.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = GetComponent<SpriteRenderer>()?.sprite;
        renderer.color = new Color(1, .5f, .1f);
        renderer.sortingOrder = 20;
        visual.transform.localScale = Vector3.one * .18f;
        grenade.AddComponent<BerserkerGrenade>().Launch(AggroTarget.position,
            grenadeFlightTime, grenadeRadius, RangedDamage, attackVfx);
    }
    private void ExplodeOnDefeat(EnemyBase enemy)
    {
        if (HasExploded) return;
        HasExploded = true; // Set before damage callbacks to prevent reentrant blasts.
        EnemyBlast.Detonate(transform.position + Vector3.up * .5f, deathBlastRadius,
            Mathf.Max(MeleeDamage, RangedDamage) * 2, true, this, attackVfx, contactDetonation);
    }
}
