using System;
using UnityEngine;

public enum TurretBossPayload { Grenades, Missiles }

/// <summary>Independent launcher track: body state changes never cancel its in-flight payload.</summary>
public sealed class TurretBossLauncher : MonoBehaviour
{
    private TurretBossAttackField field;
    private Transform target;
    private Vector2 origin;
    private float elapsed;
    private int remaining;
    private int damage;
    private TurretBossPayload payload;
    public bool IsActive { get; private set; }
    public TurretBossAttackState Stage { get; private set; } = TurretBossAttackState.Defeated;
    public event Action<TurretBossAttackState> OnStageChanged;

    public void Initialize(TurretBossAttackField attackField, Transform player)
    {
        field = attackField;
        target = player;
    }

    public void SetOrigin(Vector2 position) { origin = position; }

    public bool TryStart(TurretBossPayload kind, int attackDamage)
    {
        if (IsActive || field == null || target == null) { return false; }
        IsActive = true;
        remaining = 3;
        payload = kind;
        damage = attackDamage;
        Enter(kind == TurretBossPayload.Missiles ? TurretBossAttackState.MissileLaunch : TurretBossAttackState.GrenadeWarning);
        return true;
    }

    public void Advance(float delta)
    {
        if (!IsActive || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta)) { return; }
        elapsed += delta;
        switch (Stage)
        {
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
                    else { Enter(payload == TurretBossPayload.Missiles ? TurretBossAttackState.MissileLaunch : TurretBossAttackState.GrenadeWarning); }
                }
                break;
        }
    }

    private void Enter(TurretBossAttackState next)
    {
        Stage = next;
        elapsed = 0f;
        field.SetBossOrigin(origin, -1f, 0f);
        field.SetState(next, target.position.x, damage);
        OnStageChanged?.Invoke(next);
    }

    public void Cancel()
    {
        IsActive = false;
        Stage = TurretBossAttackState.Defeated;
        if (field != null) { field.Clear(); }
        OnStageChanged?.Invoke(Stage);
    }

    private void OnDisable() { Cancel(); }
}
