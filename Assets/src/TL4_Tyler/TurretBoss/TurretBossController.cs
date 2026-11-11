using UnityEngine;
using EscapeThe90s.Contracts;

public enum TurretBossAttackState
{
    FloorWarning, FloorVolley, GrenadeWarning, GrenadeFlight, GrenadeBlast,
    BeamCharge, BeamFire, Recovery, Defeated,
    MissileLaunch, MissileWarning, MissileDescent, MissileBlast,
    Transforming, ShortChargeWarning, ShortCharge, StompWarning, Stomping, Planting,
    LongChargeWarning, LongCharge, LauncherWait, PayloadWait, MobileAssess, Turning, Repositioning, FollowThrough, Disengaging
}

/// <summary>Planted Siege Core prototype. All hazards are resolved by the separate attack field.</summary>
public sealed class TurretBossController : EnemyBase
{
    [SerializeField] private float floorWarningSeconds = 0.7f;
    [SerializeField] private float beamChargeSeconds = 1.1f;
    [SerializeField] private float recoverySeconds = 1.6f;
    private Transform target;
    private TurretBossAttackField field;
    private float elapsed;
    private int shotsLaunched;
    private bool missileCycle;
    private TurretBossLauncher launcher;
    public TurretBossLauncher Launcher => launcher;
    public event System.Action<int> DamageTaken;
    public const float LongChargeWarningSeconds = 0.7f;
    public const float LongChargeSeconds = 2.8f;
    [SerializeField, Min(0.1f)] private float chargeTriggerDistance = 3.5f;
    [SerializeField, Min(0.1f)] private float chargeCooldownSeconds = 4f;
    [UnityEngine.Serialization.FormerlySerializedAs("spinSpeed")]
    [SerializeField, Min(0.1f)] private float longChargeSpeed = 4.5f;
    private float chargeCooldown;
    private float facingDirection = -1f;
    private const float LeftMovementLimit = -8.4f;
    private const float RightMovementLimit = 3.3f;
    private bool PlayerTooClose => Mathf.Abs(target.position.x - transform.position.x) <= chargeTriggerDistance;
    [SerializeField, Min(0.05f)] private float assessmentSeconds = 0.22f;
    [SerializeField, Min(0.05f)] private float turnSeconds = 0.4f;
    [SerializeField, Min(0.05f)] private float followThroughSeconds = 0.45f;
    [SerializeField, Range(0f, 1f)] private float comboExtensionChance = 0.5f;
    [SerializeField, Min(0f)] private float heavyRecoveryBonus = 0.4f;
    private System.Random decisions;
    private int comboAttacks;
    private float recoveryDuration;
    private bool finishWithBeam;
    private TurretBossAttackState completedAttack;
    private TurretBossAttackState previousOpening = TurretBossAttackState.Defeated;
    public int ComboAttacks => comboAttacks;
    public float RecoveryDuration => recoveryDuration;
    public const float RepositionSeconds = 0.7f;
    public const float DisengageSeconds = 0.65f;
    private Vector3 shortChargeStart;
    private float chargeDestination;
    private float plantedY;
    private Rigidbody2D bossBody;
    private TurretBossBodyCollision solidBody;
    public void BindSolidBody(TurretBossBodyCollision collision) { solidBody = collision; }
    private TurretBossLegRig legs;
    public bool IsMobilePhase { get; private set; }
    public const float MobilePhaseThreshold = 0.4f;
    public const float TransformationSeconds = 2f;
    public const float ShortChargeWarningSeconds = 0.25f;
    public const float ShortChargeSeconds = 0.3f;
    public const float StompWarningSeconds = 0.8f;
    public const float StompSeconds = 1.5f;
    public const float PlantSeconds = 1f;
    public TurretBossAttackState State { get; private set; }
    public float StateElapsed => elapsed;
    public float FacingDirection => facingDirection;
    public float BeamChargeSeconds => beamChargeSeconds;
    public bool CoreExposed => State == TurretBossAttackState.Recovery && !IsDefeated;
    public override EnemyType Type => EnemyType.Turret;
    public override EnemyMovementType MovementType => IsMobilePhase ? EnemyMovementType.Ground : EnemyMovementType.Static;
    public override bool CanStartAttack => false;

    public void Begin(Transform player, TurretBossAttackField attackField, int level, int? seed = null)
    {
        SetAttackMode(EnemyAttackMode.Both);
        decisions = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        comboAttacks = 0;
        recoveryDuration = recoverySeconds;
        target = player;
        field = attackField;
        missileCycle = false;
        bossBody = GetComponent<Rigidbody2D>();
        legs = GetComponent<TurretBossLegRig>();
        if (legs == null) { legs = gameObject.AddComponent<TurretBossLegRig>(); }
        GameObject launcherObject = new GameObject("Independent launcher");
        launcherObject.transform.SetParent(field.transform);
        launcher = launcherObject.AddComponent<TurretBossLauncher>();
        launcher.Initialize(field.CreateLauncherField(), target, seed);
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
        elapsed += deltaTime;
        switch (State)
        {
            case TurretBossAttackState.MobileAssess:
                legs.SetPose(1f, Mathf.Sin(elapsed * 5f) * 0.1f);
                if (elapsed >= assessmentSeconds)
                {
                    float desired = target.position.x < transform.position.x ? -1f : 1f;
                    ChangeState(desired != facingDirection ? TurretBossAttackState.Turning : SelectOpening());
                }
                break;
            case TurretBossAttackState.Turning:
                legs.SetPose(1f, Mathf.Sin(elapsed / turnSeconds * Mathf.PI) * 0.8f);
                if (elapsed >= turnSeconds) { ChangeState(SelectOpening()); }
                break;
            case TurretBossAttackState.Repositioning:
            case TurretBossAttackState.Disengaging:
                float moveDuration = State == TurretBossAttackState.Repositioning ? RepositionSeconds : DisengageSeconds;
                float moveProgress = Mathf.Clamp01(elapsed / moveDuration);
                MoveBody(new Vector3(Mathf.SmoothStep(shortChargeStart.x, chargeDestination, moveProgress), plantedY + 0.9f, transform.position.z));
                legs.SetPose(1f, Mathf.Sin(moveProgress * Mathf.PI * 4f));
                if (elapsed >= moveDuration)
                {
                    ChangeState(State == TurretBossAttackState.Repositioning ? SelectOpening(false) : TurretBossAttackState.LauncherWait);
                }
                break;
            case TurretBossAttackState.FollowThrough:
                MoveBody(new Vector3(transform.position.x, plantedY + 0.9f - 0.12f * Mathf.Sin(Mathf.Clamp01(elapsed / followThroughSeconds) * Mathf.PI), transform.position.z));
                legs.SetPose(1f, 0f);
                if (elapsed >= followThroughSeconds) { ChooseFollowUp(); }
                break;
            case TurretBossAttackState.FloorWarning:
                if (elapsed >= floorWarningSeconds) { ChangeState(TurretBossAttackState.FloorVolley); }
                break;
            case TurretBossAttackState.FloorVolley:
                if (shotsLaunched < 3 && elapsed >= shotsLaunched * 0.45f)
                {
                    field.SetMobileFloorOrigin(IsMobilePhase);
                    field.SetBossOrigin(new Vector2(transform.position.x, 0f), facingDirection, chargeDestination);
                    field.FireFloorShot(RangedDamage);
                    shotsLaunched++;
                }
                if (elapsed >= 2f)
                {
                    launcher.TryStart(missileCycle ? TurretBossPayload.Missiles : TurretBossPayload.Grenades, RangedDamage);
                    ChangeState(TurretBossAttackState.LauncherWait);
                }
                break;
            case TurretBossAttackState.LauncherWait:
                if (!launcher.IsActive) { ChangeState(IsMobilePhase ? TurretBossAttackState.Planting : TurretBossAttackState.BeamCharge); }
                break;
            case TurretBossAttackState.Transforming:
                MoveBody(new Vector3(transform.position.x, plantedY + 0.9f * Mathf.Clamp01(elapsed / TransformationSeconds), transform.position.z));
                legs.SetPose(Mathf.Clamp01(elapsed / TransformationSeconds), 0f);
                if (elapsed >= TransformationSeconds) { ChangeState(TurretBossAttackState.MobileAssess); }
                break;
            case TurretBossAttackState.ShortChargeWarning:
                MoveBody(new Vector3(transform.position.x, plantedY + 0.9f - 0.12f * Mathf.Clamp01(elapsed / ShortChargeWarningSeconds), transform.position.z));
                legs.SetPose(1f, -0.6f * Mathf.Clamp01(elapsed / ShortChargeWarningSeconds));
                if (elapsed >= ShortChargeWarningSeconds) { ChangeState(TurretBossAttackState.ShortCharge); }
                break;
            case TurretBossAttackState.ShortCharge:
                float previousX = transform.position.x;
                float progress = Mathf.Clamp01(elapsed / ShortChargeSeconds);
                MoveBody(new Vector3(Mathf.Lerp(shortChargeStart.x, chargeDestination, progress), plantedY + 0.9f, transform.position.z));
                legs.SetPose(1f, Mathf.Sin(progress * Mathf.PI * 4f));
                field.ResolveShortCharge(previousX, transform.position.x, MeleeDamage);
                if (elapsed >= ShortChargeSeconds) { CompleteAttack(TurretBossAttackState.ShortCharge); }
                break;
            case TurretBossAttackState.LongChargeWarning:
                legs.SetPose(1f, Mathf.Sin(elapsed * 12f) * 0.3f);
                if (elapsed >= LongChargeWarningSeconds) { ChangeState(TurretBossAttackState.LongCharge); }
                break;
            case TurretBossAttackState.LongCharge:
                float longChargePreviousX = transform.position.x;
                float longChargeProgress = Mathf.Clamp01(elapsed / LongChargeSeconds);
                float longChargeX = Mathf.MoveTowards(longChargePreviousX, chargeDestination, longChargeSpeed * 1.3f * deltaTime);
                MoveBody(new Vector3(longChargeX, plantedY + 0.9f, transform.position.z));
                legs.SetLongChargePose(longChargeProgress);
                field.ResolveLongCharge(longChargePreviousX, transform.position.x, MeleeDamage);
                if (field.ChargeHitPlayer || elapsed >= LongChargeSeconds || Mathf.Abs(longChargeX - chargeDestination) < 0.01f) { CompleteAttack(TurretBossAttackState.LongCharge); }
                break;
            case TurretBossAttackState.StompWarning:
                MoveBody(new Vector3(transform.position.x, plantedY + 0.9f + 0.25f * Mathf.Clamp01(elapsed / StompWarningSeconds), transform.position.z));
                legs.SetPose(1f, -0.3f);
                if (elapsed >= StompWarningSeconds) { ChangeState(TurretBossAttackState.Stomping); }
                break;
            case TurretBossAttackState.Stomping:
                MoveBody(new Vector3(transform.position.x,
                    plantedY + 0.9f - 0.18f * Mathf.Sin(Mathf.Clamp01(elapsed / 0.25f) * Mathf.PI), transform.position.z));
                legs.SetPose(1f, 0f);
                if (elapsed >= StompSeconds) { CompleteAttack(TurretBossAttackState.Stomping); }
                break;
            case TurretBossAttackState.Planting:
                MoveBody(new Vector3(transform.position.x,
                    plantedY + 0.9f * (1f - Mathf.Clamp01(elapsed / PlantSeconds)), transform.position.z));
                legs.SetPose(1f, 0f);
                if (elapsed >= PlantSeconds) { ChangeState(finishWithBeam ? TurretBossAttackState.BeamCharge : TurretBossAttackState.Recovery); }
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
                    MoveBody(new Vector3(transform.position.x, plantedY + 0.9f * Mathf.Clamp01(elapsed / 0.5f), transform.position.z));
                    legs.SetPose(1f, 0f);
                }
                if (elapsed >= recoveryDuration)
                {
                    missileCycle = !missileCycle;
                    ChangeState(IsMobilePhase ? TurretBossAttackState.MobileAssess : TurretBossAttackState.FloorWarning);
                }
                break;
        }
    }

    private void ChangeState(TurretBossAttackState next)
    {
        State = next;
        elapsed = 0f;
        if (next == TurretBossAttackState.MobileAssess)
        {
            comboAttacks = 0;
            recoveryDuration = recoverySeconds;
        }
        if (next == TurretBossAttackState.ShortCharge || next == TurretBossAttackState.LongCharge || next == TurretBossAttackState.Stomping)
        {
            comboAttacks++;
        }
        if (next == TurretBossAttackState.Repositioning || next == TurretBossAttackState.Disengaging)
        {
            shortChargeStart = transform.position;
            float away = target.position.x < transform.position.x ? 1f : -1f;
            chargeDestination = Mathf.Clamp(shortChargeStart.x + away * 2f, LeftMovementLimit, RightMovementLimit);
        }
        if (next == TurretBossAttackState.FloorWarning)
        {
            shotsLaunched = 0;
        }
        if (next == TurretBossAttackState.Transforming || next == TurretBossAttackState.Defeated)
        {
            launcher.Cancel();
        }
        if (next == TurretBossAttackState.StompWarning ||
            (next == TurretBossAttackState.LongCharge && decisions.NextDouble() < 0.5))
        {
            launcher.SetOrigin(transform.position);
            launcher.TryStart(missileCycle ? TurretBossPayload.Grenades : TurretBossPayload.Missiles, RangedDamage, true);
        }
        if (next == TurretBossAttackState.ShortChargeWarning || next == TurretBossAttackState.LongChargeWarning)
        {
            if (next == TurretBossAttackState.ShortChargeWarning) { chargeCooldown = chargeCooldownSeconds; }
            shortChargeStart = transform.position;
            float direction = target.position.x < transform.position.x ? -1f : 1f;
            chargeDestination = next == TurretBossAttackState.LongChargeWarning
                ? (direction < 0f ? LeftMovementLimit : RightMovementLimit)
                : Mathf.MoveTowards(shortChargeStart.x, Mathf.Clamp(shortChargeStart.x + direction * 3f, LeftMovementLimit, RightMovementLimit), 3f);
        }
        // Keep the charge telegraph, firing beam and body pointed the same way.
        if (next != TurretBossAttackState.BeamFire && next != TurretBossAttackState.MobileAssess &&
            next != TurretBossAttackState.FollowThrough)
        {
            facingDirection = target != null && target.position.x > transform.position.x ? 1f : -1f;
        }
        float aim = facingDirection;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) { renderer.flipX = aim < 0f; }
        if (IsMobilePhase && next != TurretBossAttackState.Transforming) { legs.SetPose(1f, 0f); }
        field.SetBossOrigin(new Vector2(transform.position.x, 0f), aim, chargeDestination);
        field.SetState(next, target != null ? target.position.x : 0f,
            next == TurretBossAttackState.Stomping ? MeleeDamage : RangedDamage);
    }

    private TurretBossAttackState SelectOpening(bool allowReposition = true)
    {
        float distance = Mathf.Abs(target.position.x - transform.position.x);
        TurretBossAttackState choice;
        if (allowReposition && distance < 1.5f && chargeCooldown > 0f)
        {
            return TurretBossAttackState.Repositioning;
        }
        // Distance constrains the options; weights add variation without arbitrary attacks.
        float shortWeight = chargeCooldown <= 0f && PlayerTooClose ? 3f : 0f;
        float longWeight = distance > 5f ? 5f : distance > 2.5f ? 2f : 0.4f;
        float stompWeight = distance <= 4f ? 2.5f : 0f;
        if (previousOpening == TurretBossAttackState.ShortChargeWarning) { shortWeight *= 0.25f; }
        if (previousOpening == TurretBossAttackState.LongChargeWarning) { longWeight *= 0.25f; }
        if (previousOpening == TurretBossAttackState.StompWarning) { stompWeight *= 0.25f; }
        float roll = (float)decisions.NextDouble() * (shortWeight + longWeight + stompWeight);
        choice = roll < shortWeight ? TurretBossAttackState.ShortChargeWarning
            : roll < shortWeight + longWeight ? TurretBossAttackState.LongChargeWarning
            : TurretBossAttackState.StompWarning;
        previousOpening = choice;
        return choice;
    }

    private void CompleteAttack(TurretBossAttackState attack)
    {
        completedAttack = attack;
        ChangeState(TurretBossAttackState.FollowThrough);
    }

    private void ChooseFollowUp()
    {
        float distance = Mathf.Abs(target.position.x - transform.position.x);
        // Pursuit can bring the target into charge range midway through a chain.
        // Re-evaluate proximity here rather than waiting until the next opener.
        if (completedAttack == TurretBossAttackState.LongCharge && comboAttacks < 2 &&
            PlayerTooClose && chargeCooldown <= 0f && decisions.NextDouble() < comboExtensionChance)
        {
            ChangeState(TurretBossAttackState.ShortChargeWarning);
            return;
        }
        bool extend = comboAttacks < 2 && completedAttack != TurretBossAttackState.Stomping &&
            decisions.NextDouble() < comboExtensionChance && distance > 1.5f;
        if (extend)
        {
            ChangeState(distance <= 4f ? TurretBossAttackState.StompWarning : TurretBossAttackState.LongChargeWarning);
            return;
        }
        // Every completed chain earns an opening. More commitment means more punish time.
        recoveryDuration = recoverySeconds + heavyRecoveryBonus * comboAttacks +
            (completedAttack == TurretBossAttackState.Stomping ? heavyRecoveryBonus : 0f);
        finishWithBeam = distance > 4f && decisions.NextDouble() < 0.5;
        ChangeState(completedAttack == TurretBossAttackState.LongCharge && distance < 3f
            ? TurretBossAttackState.Disengaging : TurretBossAttackState.LauncherWait);
    }

    public override bool TakeDamage(int amount)
    {
        if (!CoreExposed)
        {
            return IsDefeated;
        }
        int previousHealth = CurrentHealth;
        bool died = base.TakeDamage(amount);
        if (CurrentHealth < previousHealth) { DamageTaken?.Invoke(previousHealth - CurrentHealth); }
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
            ChangeState(TurretBossAttackState.Transforming);
        }
        return died;
    }

    private void MoveBody(Vector3 destination)
    {
        transform.position = solidBody != null ? solidBody.ConstrainMove(destination) : destination;
        if (solidBody != null) { solidBody.SyncShape(); }
    }

    private void LateUpdate()
    {
        float lean = 0f;
        if (State == TurretBossAttackState.ShortChargeWarning) { lean = -facingDirection * 7f * Mathf.Clamp01(elapsed / ShortChargeWarningSeconds); }
        else if (State == TurretBossAttackState.ShortCharge) { lean = facingDirection * 9f; }
        else if (State == TurretBossAttackState.LongChargeWarning) { lean = Mathf.Sin(elapsed * 16f) * 3f; }
        else if (State == TurretBossAttackState.FollowThrough) { lean = facingDirection * 6f * (1f - Mathf.Clamp01(elapsed / followThroughSeconds)); }
        else if (State == TurretBossAttackState.BeamFire) { lean = -facingDirection * 4f * Mathf.Exp(-elapsed * 8f); }
        transform.rotation = solidBody != null ? Quaternion.identity : Quaternion.Euler(0f, 0f, lean);
        if (State != TurretBossAttackState.BeamCharge && State != TurretBossAttackState.BeamFire) { return; }
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) { renderer.flipX = facingDirection < 0f; }
    }

    private void OnDisable()
    {
        transform.rotation = Quaternion.identity;
        if (field != null) { field.Clear(); }
        if (launcher != null) { launcher.Cancel(); }
    }
}
