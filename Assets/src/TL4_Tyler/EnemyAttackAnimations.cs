using System;
using UnityEngine;

public enum EnemyAttackAnimation { None, Melee, Ranged }
public enum EnemyAttackPhase { Ready, Attacking, FollowUpPause, Recovering }

[RequireComponent(typeof(EnemyAnimationDriver))]
public class EnemyAttackAnimations : MonoBehaviour
{
    [SerializeField, Min(0)] private float closeAttackRange = 1.5f;
    [SerializeField, Min(0)] private float farAttackRange = 8;
    [SerializeField, Min(.1f)] private float meleeCooldown = 1;
    [SerializeField, Min(.1f)] private float rangedCooldown = 1.3f;
    [Header("Melee follow-up")]
    [SerializeField, Range(0, 1)] private float secondMeleeChance = .3f;
    [SerializeField] private Vector2 meleePauseRange = new Vector2(.15f, .25f);
    [SerializeField, Min(0)] private float firstMeleeRecovery = .1f;
    [Header("Ranged follow-up")]
    [SerializeField, Range(0, 1)] private float secondShotChance = .35f;
    [SerializeField] private Vector2 burstPauseRange = new Vector2(.25f, .45f);
    [Header("Recovery and pursuit")]
    [SerializeField] private Vector2 recoveryRange = new Vector2(.3f, .7f);
    [SerializeField] private Vector2 pursuitWindowRange = new Vector2(.5f, .9f);
    private EnemyBase enemy;
    private EnemyAnimationDriver animationDriver;
    private EnemyRangedAttack shooter;
    private Rigidbody2D body;
    private float nextAttackTime, phaseEndsAt;
    private bool followUpPending, hasUsedMelee;
    private EnemyAttackAnimation followUpAttack;
    public EnemyAttackPhase Phase { get; private set; }
    public bool IsMovementLocked => isActiveAndEnabled && Phase != EnemyAttackPhase.Ready && !enemy.IsDefeated;
    public event Action<EnemyAttackAnimation> AttackTriggered;
    public event Action SequenceCompleted;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        animationDriver = GetComponent<EnemyAnimationDriver>();
        shooter = GetComponent<EnemyRangedAttack>();
        body = GetComponent<Rigidbody2D>();
    }

    private void OnEnable() { enemy.Defeated += OnDefeated; }
    private void OnDisable() { enemy.Defeated -= OnDefeated; CancelSequence(); }
    private void OnDefeated(EnemyBase _) { CancelSequence(); }
    private void CancelSequence() { Phase = EnemyAttackPhase.Ready; followUpPending = false; }

    private void Update()
    {
        if (enemy.IsDefeated) return;
        if (Phase == EnemyAttackPhase.Ready) { TryTriggerAttack(); return; }
        StopHorizontalMovement();
        if (Phase == EnemyAttackPhase.FollowUpPause && (!ValidTarget() || SelectAttack() != followUpAttack))
        {
            followUpPending = false;
            BeginRecovery();
            return;
        }
        if (Time.time < phaseEndsAt) return;
        switch (Phase)
        {
            case EnemyAttackPhase.Attacking:
                if (followUpPending && ValidTarget() && SelectAttack() == followUpAttack)
                {
                    Phase = EnemyAttackPhase.FollowUpPause;
                    float pause = followUpAttack == EnemyAttackAnimation.Melee
                        ? RandomDelay(meleePauseRange) : Mathf.Max(RandomDelay(burstPauseRange),
                            shooter != null ? shooter.TimeUntilNextShot : 0);
                    phaseEndsAt = Time.time + pause;
                }
                else
                {
                    followUpPending = false;
                    BeginRecovery();
                }
                break;
            case EnemyAttackPhase.FollowUpPause:
                followUpPending = false;
                // Recheck range and attack type before the second shot or swing.
                if (!ValidTarget() || SelectAttack() != followUpAttack ||
                    !BeginAttack(followUpAttack)) BeginRecovery();
                break;
            case EnemyAttackPhase.Recovering:
                Phase = EnemyAttackPhase.Ready;
                // Allow an actual pursuit interval before selecting the next attack sequence.
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + RandomDelay(pursuitWindowRange));
                SequenceCompleted?.Invoke();
                break;
        }
    }

    private bool ValidTarget() => enemy.HasAggro && enemy.AggroTarget.gameObject.activeInHierarchy;

    private EnemyAttackAnimation SelectAttack()
    {
        if (!enemy.CanAttackTarget) return EnemyAttackAnimation.None;
        float distance = Vector2.Distance(enemy.AggroTarget.position, transform.position);
        if (distance <= Mathf.Max(0, closeAttackRange) && enemy.CanMelee) return EnemyAttackAnimation.Melee;
        if (distance <= Mathf.Max(closeAttackRange, farAttackRange) && enemy.CanRanged) return EnemyAttackAnimation.Ranged;
        return EnemyAttackAnimation.None;
    }

    public EnemyAttackAnimation TryTriggerAttack()
    {
        if (!isActiveAndEnabled || Phase != EnemyAttackPhase.Ready || !ValidTarget() || !enemy.CanStartAttack)
            return EnemyAttackAnimation.None;
        var attack = SelectAttack();
        // The first melee can react immediately after recovery, without the ranged/pursuit delay.
        if (Time.time < nextAttackTime && !(attack == EnemyAttackAnimation.Melee && !hasUsedMelee))
            return EnemyAttackAnimation.None;
        if (attack == EnemyAttackAnimation.None || !BeginAttack(attack)) return EnemyAttackAnimation.None;
        followUpAttack = attack;
        followUpPending = UnityEngine.Random.value <
            (attack == EnemyAttackAnimation.Melee ? secondMeleeChance : secondShotChance);
        return attack;
    }

    private bool BeginAttack(EnemyAttackAnimation attack)
    {
        Vector2 offset = enemy.AttackTargetPosition - transform.position;
        if (enemy is GroundEnemy ground && Mathf.Abs(offset.x) > .05f) ground.SetFacingRight(offset.x > 0);
        if (enemy is FlyingEnemy flying && Mathf.Abs(offset.x) > .05f) flying.SetFacingRight(offset.x > 0);
        bool triggered;
        if (attack == EnemyAttackAnimation.Melee) triggered = animationDriver.TriggerMelee();
        else if (shooter != null)
            triggered = animationDriver.CanPlayRanged && shooter.TryShoot(enemy.AttackTargetPosition, out _);
        else triggered = animationDriver.TriggerRanged();
        if (!triggered) return false;
        if (attack == EnemyAttackAnimation.Melee) hasUsedMelee = true;
        Phase = EnemyAttackPhase.Attacking;
        phaseEndsAt = Time.time + animationDriver.AttackDuration(attack);
        nextAttackTime = Time.time + Mathf.Max(.1f, attack == EnemyAttackAnimation.Melee ? meleeCooldown : rangedCooldown);
        StopHorizontalMovement();
        AttackTriggered?.Invoke(attack);
        return true;
    }

    private void BeginRecovery()
    {
        Phase = EnemyAttackPhase.Recovering;
        bool firstCloseAttack = !hasUsedMelee && ValidTarget() && SelectAttack() == EnemyAttackAnimation.Melee;
        phaseEndsAt = Time.time + (firstCloseAttack ? Mathf.Max(0, firstMeleeRecovery) : RandomDelay(recoveryRange));
    }

    private void StopHorizontalMovement()
    {
        if (body != null && body.simulated)
            body.linearVelocity = enemy.MovementType == EnemyMovementType.Flying ? Vector2.zero : new Vector2(0, body.linearVelocity.y);
        animationDriver.SetMoving(false);
    }

    private static float RandomDelay(Vector2 range)
    {
        float minimum = Mathf.Max(0, range.x);
        return UnityEngine.Random.Range(minimum, Mathf.Max(minimum, range.y));
    }
}
