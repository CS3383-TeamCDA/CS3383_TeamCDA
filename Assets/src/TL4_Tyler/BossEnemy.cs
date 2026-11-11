using UnityEngine;

/// <summary>Prototype boss using TL4's grounded movement and existing attack animations.</summary>
public sealed class BossEnemy : GroundEnemy
{
    private float normalSpeed;
    private bool combatActive = true;
    private bool aggressiveOpening;
    private EnemyAttackAnimations bossAttacks;
    public bool IsEnraged => !IsDefeated && CurrentHealth <= MaxHealth / 2;
    public override bool CanAttackTarget => combatActive && base.CanAttackTarget;
    public override bool CanStartAttack => combatActive && base.CanStartAttack;

    protected override void Awake()
    {
        base.Awake();
        normalSpeed = speed;
        bossAttacks = GetComponent<EnemyAttackAnimations>();
    }

    public void Configure(int seed, Transform target)
    {
        aggressiveOpening = (seed & 1) != 0;
        SetTarget(target);
        ApplyPhase();
    }

    public void SetCombatActive(bool active)
    {
        combatActive = active;
    }

    protected override void Update()
    {
        if (!combatActive)
        {
            return;
        }
        base.Update();
        ApplyPhase();
    }

    private void ApplyPhase()
    {
        speed = normalSpeed * (IsEnraged ? 1.4f : 1f);
        if (bossAttacks != null)
        {
            float followUpChance = IsEnraged ? 0.6f : (aggressiveOpening ? 0.4f : 0.2f);
            bossAttacks.ConfigureCadence(IsEnraged ? 0.7f : 1.2f, IsEnraged ? 0.8f : 1.5f, followUpChance);
        }
    }

    public override void Move()
    {
        if (combatActive)
        {
            base.Move();
        }
    }
}
