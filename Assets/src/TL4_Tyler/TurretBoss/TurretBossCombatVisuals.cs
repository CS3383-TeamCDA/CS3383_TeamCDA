using UnityEngine;

/// <summary>TL4 presentation adapter. Combat state and damage remain owned by the controller.</summary>
[DefaultExecutionOrder(50)]
public sealed class TurretBossCombatVisuals : MonoBehaviour
{
    private TurretBossController boss;
    private TurretBossVisualRig rig;
    private TurretBossLauncher launcher;
    private float floorY;
    private TurretBossAttackState previousState;
    private bool previousCore;
    private bool bound;

    public void Initialize(TurretBossController controller, TurretBossVisualRig visuals,
        TurretBossAttackField field, float arenaFloorY)
    {
        Unbind();
        boss = controller;
        rig = visuals;
        floorY = arenaFloorY;
        if (boss == null || rig == null || field == null) { return; }
        launcher = boss.Launcher;
        if (launcher == null) { return; }
        rig.SetForm(false);
        rig.SetExposed(boss.CoreExposed);
        rig.BodyHeightOffset = -0.15f; // Align barrel height with the existing crouchable beam.
        FollowController();
        SpriteRenderer oldSprite = boss.GetComponent<SpriteRenderer>();
        if (oldSprite != null) { oldSprite.enabled = false; }
        Animator oldAnimator = boss.GetComponent<Animator>();
        if (oldAnimator != null) { oldAnimator.enabled = false; }
        TurretBossLegRig oldLegs = boss.GetComponent<TurretBossLegRig>();
        if (oldLegs != null) { oldLegs.PresentationVisible = false; }
        field.BindBeamMuzzle(rig.Muzzle);
        field.BindBodyAnchors(rig);
        launcher.BindVisualMuzzle(rig.LauncherMuzzle);
        launcher.OnStageChanged += OnLauncherStage;
        boss.DamageTaken += OnDamageTaken;
        rig.SetHealthFraction((float)boss.CurrentHealth / Mathf.Max(1, boss.MaxHealth));
        previousState = boss.State;
        previousCore = boss.CoreExposed;
        bound = true;
    }

    private void Update()
    {
        if (!bound || boss == null || rig == null) { return; }
        if (boss.IsDefeated)
        {
            rig.BeginDeath();
            Unbind();
            return;
        }
        rig.SetHealthFraction((float)boss.CurrentHealth / Mathf.Max(1, boss.MaxHealth));
        FollowController();
        TurretBossAttackState state = boss.State;
        if (state != previousState)
        {
            if (state == TurretBossAttackState.Transforming) { rig.TransitionToForm(true); }
            else if (boss.IsMobilePhase && !rig.IsMobile) { rig.SetForm(true); }
            previousState = state;
        }
        if (boss.CoreExposed != previousCore)
        {
            previousCore = boss.CoreExposed;
            rig.AnimateCore(previousCore);
        }
        bool beam = state == TurretBossAttackState.BeamCharge || state == TurretBossAttackState.BeamFire;
        // The mobile legs crouch under the lowered cannon without returning to the abandoned base.
        rig.BodyHeightOffset = boss.IsMobilePhase && beam ? -1.23f : -0.15f;
        rig.GroundedWalkingEnabled = true;
        rig.SyncCombatMotion(state, boss.StateElapsed);
        if (state == TurretBossAttackState.BeamCharge)
        {
            rig.SyncCannonTimeline(1.5f * Mathf.Clamp01(boss.StateElapsed / Mathf.Max(0.01f, boss.BeamChargeSeconds)));
        }
        else if (state == TurretBossAttackState.BeamFire) { rig.SyncCannonTimeline(1.5f + boss.StateElapsed); }
        else { rig.SyncCannonTimeline(-1f); }

    }

    private void FollowController()
    {
        rig.transform.position = new Vector3(boss.transform.position.x, floorY, 0f);
        rig.transform.localScale = new Vector3(boss.FacingDirection * 0.5f, 0.5f, 1f);
    }

    private void OnLauncherStage(TurretBossAttackState stage)
    {
        if (rig == null) { return; }
        if (stage == TurretBossAttackState.MissileLaunch || stage == TurretBossAttackState.GrenadeFlight)
        {
            rig.PlayLauncherShot(true);
        }
    }

    private void OnDamageTaken(int amount)
    {
        if (rig != null && boss != null)
        {
            rig.PlayDamageFeedback((float)boss.CurrentHealth / Mathf.Max(1, boss.MaxHealth));
        }
    }

    private void Unbind()
    {
        if (launcher != null) { launcher.OnStageChanged -= OnLauncherStage; }
        if (boss != null) { boss.DamageTaken -= OnDamageTaken; }
        bound = false;
    }

    private void OnDisable()
    {
        Unbind();
        if (rig != null) { rig.CancelWeaponAnimations(); }
    }
}
