using System;
using UnityEngine;

public enum TurretBossPayload { Grenades, Missiles }

/// <summary>Independent launcher track: body state changes never cancel its in-flight payload.</summary>
public sealed class TurretBossLauncher : MonoBehaviour
{
    [SerializeField, Min(0f)] private float minimumShotGap = 0.3f;
    [SerializeField, Min(0f)] private float maximumShotGap = 0.4f;
    private System.Random random;
    private bool variableBarrage;
    private bool singlePayload;
    private readonly System.Collections.Generic.List<TurretBossLauncher> flights = new System.Collections.Generic.List<TurretBossLauncher>();
    public int PlannedShotCount { get; private set; }
    public float NextShotDelay { get; private set; }
    private TurretBossAttackField field;
    private Transform target;
    private Vector2 origin;
    private Transform visualMuzzle;
    public void BindVisualMuzzle(Transform muzzle)
    {
        visualMuzzle = muzzle;
        if (field != null) { field.ShowLauncherModule(muzzle == null); }
    }
    private Vector2 EffectiveOrigin => visualMuzzle != null
        ? (Vector2)visualMuzzle.position - new Vector2(0.5f, 2.2f) : origin;
    private float elapsed;
    private int remaining;
    private int damage;
    private TurretBossPayload payload;
    public bool IsActive { get; private set; }
    public TurretBossAttackState Stage { get; private set; } = TurretBossAttackState.Defeated;
    public event Action<TurretBossAttackState> OnStageChanged;

    public void Initialize(TurretBossAttackField attackField, Transform player, int? seed = null)
    {
        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        field = attackField;
        target = player;
    }

    public void SetOrigin(Vector2 position)
    {
        origin = position;
        if (field != null) { field.SetBossOrigin(EffectiveOrigin, -1f, 0f); }
    }

    public bool TryStart(TurretBossPayload kind, int attackDamage, bool varyBarrage = false)
    {
        if (IsActive || field == null || target == null) { return false; }
        IsActive = true;
        variableBarrage = varyBarrage;
        PlannedShotCount = singlePayload ? 1 : variableBarrage ? random.Next(1, 6) : 3;
        remaining = PlannedShotCount;
        NextShotDelay = 0f;
        payload = kind;
        damage = attackDamage;
        if (variableBarrage)
        {
            elapsed = 0f;
            LaunchPayload();
            return true;
        }
        Enter(kind == TurretBossPayload.Missiles ? TurretBossAttackState.MissileLaunch : TurretBossAttackState.GrenadeWarning);
        return true;
    }

    public void Advance(float delta)
    {
        if (!IsActive || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta)) { return; }
        elapsed += delta;
        if (variableBarrage)
        {
            foreach (TurretBossLauncher flight in flights) { flight.Advance(delta); }
            // Launch cadence is independent of each payload's warning/flight/blast lifetime.
            while (remaining > 0 && elapsed >= NextShotDelay)
            {
                elapsed -= NextShotDelay;
                LaunchPayload();
            }
            bool flying = false;
            foreach (TurretBossLauncher flight in flights) { flying |= flight.IsActive; }
            if (remaining == 0 && !flying) { Cancel(); }
            return;
        }
        switch (Stage)
        {
            case TurretBossAttackState.PayloadWait:
                if (elapsed >= NextShotDelay)
                {
                    Enter(payload == TurretBossPayload.Missiles ? TurretBossAttackState.MissileLaunch : TurretBossAttackState.GrenadeWarning);
                }
                break;
            case TurretBossAttackState.MissileLaunch:
                if (elapsed >= TurretBossAttackField.MissileLaunchSeconds) { Enter(TurretBossAttackState.MissileWarning); }
                break;
            case TurretBossAttackState.MissileWarning:
                if (elapsed >= TurretBossAttackField.MissileWarningSeconds) { Enter(TurretBossAttackState.MissileDescent); }
                break;
            case TurretBossAttackState.MissileDescent:
                if (elapsed >= TurretBossAttackField.MissileDescentSeconds) { Enter(TurretBossAttackState.MissileBlast); }
                break;
            case TurretBossAttackState.GrenadeWarning:
                if (elapsed >= 0.8f) { Enter(TurretBossAttackState.GrenadeFlight); }
                break;
            case TurretBossAttackState.GrenadeFlight:
                if (elapsed >= 1.2f) { Enter(TurretBossAttackState.GrenadeBlast); }
                break;
            case TurretBossAttackState.MissileBlast:
            case TurretBossAttackState.GrenadeBlast:
                if (elapsed >= 0.35f)
                {
                    remaining--;
                    if (remaining == 0) { Cancel(); }
                    else if (variableBarrage)
                    {
                        float minimum = Mathf.Max(0f, minimumShotGap);
                        float maximum = Mathf.Max(minimum, maximumShotGap);
                        NextShotDelay = Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
                        Enter(TurretBossAttackState.PayloadWait);
                    }
                    else { Enter(payload == TurretBossPayload.Missiles ? TurretBossAttackState.MissileLaunch : TurretBossAttackState.GrenadeWarning); }
                }
                break;
        }
    }

    private void LaunchPayload()
    {
        TurretBossAttackField track = field.CreateLauncherField();
        TurretBossLauncher flight = track.gameObject.AddComponent<TurretBossLauncher>();
        flight.singlePayload = true;
        flight.Initialize(track, target, random.Next());
        flight.BindVisualMuzzle(visualMuzzle);
        flight.SetOrigin(origin);
        flight.OnStageChanged += stage =>
        {
            if (stage == TurretBossAttackState.Defeated) { return; }
            Stage = stage;
            OnStageChanged?.Invoke(stage);
        };
        flights.Add(flight);
        flight.TryStart(payload, damage);
        remaining--;
        NextShotDelay = Mathf.Lerp(Mathf.Max(0.01f, minimumShotGap),
            Mathf.Max(Mathf.Max(0.01f, minimumShotGap), maximumShotGap), (float)random.NextDouble());
    }

    private void Enter(TurretBossAttackState next)
    {
        Stage = next;
        elapsed = 0f;
        field.SetBossOrigin(EffectiveOrigin, -1f, 0f);
        field.SetState(next, target.position.x, damage);
        OnStageChanged?.Invoke(next);
    }

    public void Cancel()
    {
        foreach (TurretBossLauncher flight in flights)
        {
            if (flight == null) { continue; }
            flight.Cancel();
            Destroy(flight.gameObject);
        }
        flights.Clear();
        IsActive = false;
        Stage = TurretBossAttackState.Defeated;
        if (field != null) { field.Clear(); }
        OnStageChanged?.Invoke(Stage);
    }

    private void OnDisable() { Cancel(); }
}
