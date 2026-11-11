using UnityEngine;
using EscapeThe90s.Contracts;

public enum TurretBossAttackState
{
    FloorWarning, FloorVolley, GrenadeWarning, GrenadeFlight, GrenadeBlast,
    BeamCharge, BeamFire, Recovery, Defeated,
    MissileLaunch, MissileWarning, MissileDescent, MissileBlast,
    Transforming, LungeWarning, Lunging, StompWarning, Stomping, Planting,
    SpinWarning, Spinning, LauncherWait, PayloadWait
}

/// <summary>Planted Siege Core prototype. All hazards are resolved by the separate attack field.</summary>
public sealed class TurretBossController : EnemyBase
{
    [SerializeField] private float floorWarningSeconds = 1f;
    [SerializeField] private float beamChargeSeconds = 1.5f;
    [SerializeField] private float recoverySeconds = 2.5f;
    private Transform target;
    private TurretBossAttackField field;
    private float elapsed;
    private int shotsLaunched;
    private bool missileCycle;
    private TurretBossLauncher launcher;
    public TurretBossLauncher Launcher => launcher;
    public const float SpinWarningSeconds = 1.2f;
    public const float SpinSeconds = 4f;
    [SerializeField, Min(0.1f)] private float chargeTriggerDistance = 3.5f;
    [SerializeField, Min(0.1f)] private float chargeCooldownSeconds = 4f;
    [SerializeField, Min(0.1f)] private float spinSpeed = 4.5f;
    private float chargeCooldown;
    private float facingDirection = -1f;
    private const float LeftMovementLimit = -8.4f;
    private const float RightMovementLimit = 3.3f;
    private bool PlayerTooClose => Mathf.Abs(target.position.x - transform.position.x) <= chargeTriggerDistance;
    private TurretBossAttackState NextMobileAttack => PlayerTooClose && chargeCooldown <= 0f
        ? TurretBossAttackState.LungeWarning : TurretBossAttackState.SpinWarning;
    private Vector3 lungeStart;
    private float lungeDestination;
    private float plantedY;
    private Rigidbody2D bossBody;
    private TurretBossLegRig legs;
    public bool IsMobilePhase { get; private set; }
    public const float MobilePhaseThreshold = 0.4f;
    public const float TransformationSeconds = 2f;
    public const float LungeWarningSeconds = 0.25f;
    public const float LungeSeconds = 0.3f;
    public const float StompWarningSeconds = 0.8f;
    public const float StompSeconds = 1.5f;
    public const float PlantSeconds = 1f;
    public TurretBossAttackState State { get; private set; }
    public float StateElapsed => elapsed;
    public bool CoreExposed => State == TurretBossAttackState.Recovery && !IsDefeated;
    public override EnemyType Type => EnemyType.Turret;
    public override EnemyMovementType MovementType => IsMobilePhase ? EnemyMovementType.Ground : EnemyMovementType.Static;
    public override bool CanStartAttack => false;

    public void Begin(Transform player, TurretBossAttackField attackField, int level)
    {
        target = player;
        field = attackField;
        missileCycle = false;
        bossBody = GetComponent<Rigidbody2D>();
        legs = GetComponent<TurretBossLegRig>();
        if (legs == null) { legs = gameObject.AddComponent<TurretBossLegRig>(); }
        GameObject launcherObject = new GameObject("Independent launcher");
        launcherObject.transform.SetParent(field.transform);
        launcher = launcherObject.AddComponent<TurretBossLauncher>();
        launcher.Initialize(field.CreateLauncherField(), target);
        SetLevel(level);
        ChangeState(TurretBossAttackState.FloorWarning);
    }

    protected override void Update()
    {
        if (IsDefeated || target == null || field == null || Time.deltaTime <= 0f)
        {
            return;
        }
        Advance(Time.deltaTime);
    }

    public void Advance(float deltaTime)
    {
        if (IsDefeated || target == null || field == null || deltaTime <= 0f ||
            float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
        {
            return;
        }
        launcher.SetOrigin(transform.position);
        launcher.Advance(deltaTime);
        chargeCooldown = Mathf.Max(0f, chargeCooldown - deltaTime);
        if (IsMobilePhase && chargeCooldown <= 0f && PlayerTooClose &&
            State == TurretBossAttackState.SpinWarning)
        {
            ChangeState(TurretBossAttackState.LungeWarning);
        }
        elapsed += deltaTime;
        switch (State)
        {
            case TurretBossAttackState.FloorWarning:
                if (elapsed >= floorWarningSeconds) { ChangeState(TurretBossAttackState.FloorVolley); }
                break;
            case TurretBossAttackState.FloorVolley:
                if (shotsLaunched < 3 && elapsed >= shotsLaunched * 0.45f)
                {
                    field.FireFloorShot(RangedDamage);
                    shotsLaunched++;
                }
                if (elapsed >= 3.5f)
                {
                    launcher.TryStart(missileCycle ? TurretBossPayload.Missiles : TurretBossPayload.Grenades, RangedDamage);
                    ChangeState(TurretBossAttackState.LauncherWait);
                }
                break;
            case TurretBossAttackState.LauncherWait:
                if (!launcher.IsActive) { ChangeState(IsMobilePhase ? TurretBossAttackState.Planting : TurretBossAttackState.BeamCharge); }
                break;
            case TurretBossAttackState.Transforming:
                transform.position = new Vector3(transform.position.x, plantedY + 0.9f * Mathf.Clamp01(elapsed / TransformationSeconds), transform.position.z);
                legs.SetPose(Mathf.Clamp01(elapsed / TransformationSeconds), 0f);
                if (elapsed >= TransformationSeconds) { ChangeState(NextMobileAttack); }
                break;
            case TurretBossAttackState.LungeWarning:
                if (elapsed >= LungeWarningSeconds) { ChangeState(TurretBossAttackState.Lunging); }
                break;
            case TurretBossAttackState.Lunging:
                float previousX = transform.position.x;
                float progress = Mathf.Clamp01(elapsed / LungeSeconds);
                transform.position = new Vector3(Mathf.Lerp(lungeStart.x, lungeDestination, progress), plantedY + 0.9f, transform.position.z);
                legs.SetPose(1f, Mathf.Sin(progress * Mathf.PI * 4f));
                field.ResolveLunge(previousX, transform.position.x, MeleeDamage);
                if (elapsed >= LungeSeconds) { ChangeState(TurretBossAttackState.SpinWarning); }
                break;
            case TurretBossAttackState.SpinWarning:
                legs.SetPose(1f, Mathf.Sin(elapsed * 12f) * 0.3f);
                if (elapsed >= SpinWarningSeconds) { ChangeState(TurretBossAttackState.Spinning); }
                break;
            case TurretBossAttackState.Spinning:
                float spinPreviousX = transform.position.x;
                float spinProgress = Mathf.Clamp01(elapsed / SpinSeconds);
                float spinX = Mathf.MoveTowards(spinPreviousX, lungeDestination, spinSpeed * deltaTime);
                transform.position = new Vector3(spinX, plantedY + 0.9f, transform.position.z);
                legs.SetSpinPose(spinProgress);
                field.ResolveSpin(spinPreviousX, transform.position.x, MeleeDamage);
                if (elapsed >= SpinSeconds || Mathf.Abs(spinX - lungeDestination) < 0.01f) { ChangeState(TurretBossAttackState.StompWarning); }
                break;
            case TurretBossAttackState.StompWarning:
                if (elapsed >= StompWarningSeconds) { ChangeState(TurretBossAttackState.Stomping); }
                break;
            case TurretBossAttackState.Stomping:
                transform.position = new Vector3(transform.position.x,
                    plantedY + 0.9f - 0.18f * Mathf.Sin(Mathf.Clamp01(elapsed / 0.25f) * Mathf.PI), transform.position.z);
                legs.SetPose(1f, 0f);
                if (elapsed >= StompSeconds) { ChangeState(TurretBossAttackState.LauncherWait); }
                break;
            case TurretBossAttackState.Planting:
                transform.position = new Vector3(transform.position.x,
                    plantedY + 0.9f * (1f - Mathf.Clamp01(elapsed / PlantSeconds)), transform.position.z);
                legs.SetPose(1f, 0f);
                if (elapsed >= PlantSeconds) { ChangeState(TurretBossAttackState.BeamCharge); }
                break;
            case TurretBossAttackState.BeamCharge:
                if (elapsed >= beamChargeSeconds) { ChangeState(TurretBossAttackState.BeamFire); }
                break;
            case TurretBossAttackState.BeamFire:
                if (elapsed >= 0.7f) { ChangeState(TurretBossAttackState.Recovery); }
                break;
            case TurretBossAttackState.Recovery:
                if (IsMobilePhase)
                {
                    transform.position = new Vector3(transform.position.x, plantedY + 0.9f * Mathf.Clamp01(elapsed / 0.5f), transform.position.z);
                    legs.SetPose(1f, 0f);
                }
                if (elapsed >= recoverySeconds)
                {
                    missileCycle = !missileCycle;
                    ChangeState(IsMobilePhase ? NextMobileAttack : TurretBossAttackState.FloorWarning);
                }
                break;
        }
    }

    private void ChangeState(TurretBossAttackState next)
    {
        State = next;
        elapsed = 0f;
        if (next == TurretBossAttackState.FloorWarning)
        {
            shotsLaunched = 0;
        }
        if (next == TurretBossAttackState.Transforming || next == TurretBossAttackState.Defeated)
        {
            launcher.Cancel();
        }
        if (next == TurretBossAttackState.StompWarning)
        {
            launcher.SetOrigin(transform.position);
            launcher.TryStart(missileCycle ? TurretBossPayload.Grenades : TurretBossPayload.Missiles, RangedDamage, true);
        }
        if (next == TurretBossAttackState.LungeWarning || next == TurretBossAttackState.SpinWarning)
        {
            if (next == TurretBossAttackState.LungeWarning) { chargeCooldown = chargeCooldownSeconds; }
            lungeStart = transform.position;
            float direction = target.position.x < transform.position.x ? -1f : 1f;
            lungeDestination = next == TurretBossAttackState.SpinWarning
                ? (direction < 0f ? LeftMovementLimit : RightMovementLimit)
                : Mathf.MoveTowards(lungeStart.x, Mathf.Clamp(lungeStart.x + direction * 3f, LeftMovementLimit, RightMovementLimit), 3f);
        }
        // Keep the charge telegraph, firing beam and body pointed the same way.
        if (next != TurretBossAttackState.BeamFire)
        {
            facingDirection = target != null && target.position.x > transform.position.x ? 1f : -1f;
        }
        float aim = facingDirection;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) { renderer.flipX = aim < 0f; }
        if (IsMobilePhase && next != TurretBossAttackState.Transforming) { legs.SetPose(1f, 0f); }
        field.SetBossOrigin(new Vector2(transform.position.x, 0f), aim, lungeDestination);
        field.SetState(next, target != null ? target.position.x : 0f,
            next == TurretBossAttackState.Stomping ? MeleeDamage : RangedDamage);
    }

    public override bool TakeDamage(int amount)
    {
        if (!CoreExposed)
        {
            return IsDefeated;
        }
        bool died = base.TakeDamage(amount);
        if (died)
        {
            ChangeState(TurretBossAttackState.Defeated);
            if (legs != null) { legs.SetPose(0f, 0f); }
            if (bossBody != null) { bossBody.bodyType = RigidbodyType2D.Dynamic; }
        }
        else if (!IsMobilePhase && CurrentHealth <= MaxHealth * MobilePhaseThreshold)
        {
            IsMobilePhase = true;
            plantedY = transform.position.y;
            if (bossBody != null)
            {
                bossBody.linearVelocity = Vector2.zero;
                bossBody.bodyType = RigidbodyType2D.Kinematic;
            }
            if (target != null)
            {
                foreach (Collider2D own in GetComponentsInChildren<Collider2D>())
                {
                    foreach (Collider2D other in target.GetComponentsInChildren<Collider2D>())
                    {
                        Physics2D.IgnoreCollision(own, other);
                    }
                }
            }
            ChangeState(TurretBossAttackState.Transforming);
        }
        return died;
    }

    private void LateUpdate()
    {
        if (State != TurretBossAttackState.BeamCharge && State != TurretBossAttackState.BeamFire) { return; }
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) { renderer.flipX = facingDirection < 0f; }
    }

    private void OnDisable()
    {
        if (field != null) { field.Clear(); }
        if (launcher != null) { launcher.Cancel(); }
    }
}
