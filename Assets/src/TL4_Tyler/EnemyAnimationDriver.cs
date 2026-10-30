using UnityEngine;

[RequireComponent(typeof(EnemyBase), typeof(Animator))]
public class EnemyAnimationDriver : MonoBehaviour
{
    private Animator animator;
    private EnemyBase enemy;
    private EnemyRangedAttack shooter;
    private bool hasAimIndex, hasShoot, hasMelee, hasMoving, hasDeath;
    public bool CanPlayRanged => isActiveAndEnabled && !enemy.IsDefeated && enemy.CanRanged && (hasShoot || hasAimIndex);

    public float AttackDuration(EnemyAttackAnimation attack)
    {
        string clipName = attack == EnemyAttackAnimation.Melee ? "Melee" : "Shoot";
        if (animator.runtimeAnimatorController != null)
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == clipName) return Mathf.Max(.1f, clip.length / Mathf.Max(.01f, animator.speed));
        return .7f;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemy = GetComponent<EnemyBase>();
        shooter = GetComponent<EnemyRangedAttack>();
        if (animator.runtimeAnimatorController == null) return;
        foreach (var parameter in animator.parameters)
        {
            hasAimIndex |= parameter.name == "AimIndex" && parameter.type == AnimatorControllerParameterType.Int;
            hasShoot |= parameter.name == "Shoot" && parameter.type == AnimatorControllerParameterType.Trigger;
            hasMelee |= parameter.name == "Melee" && parameter.type == AnimatorControllerParameterType.Trigger;
            hasMoving |= parameter.name == "Moving" && parameter.type == AnimatorControllerParameterType.Bool;
            hasDeath |= parameter.name == "Death" && parameter.type == AnimatorControllerParameterType.Trigger;
        }
    }

    private void OnEnable()
    {
        enemy.Defeated += OnDefeated;
        if (shooter != null) shooter.ShotFired += OnShot;
    }

    private void OnDisable()
    {
        if (enemy != null) enemy.Defeated -= OnDefeated;
        if (shooter != null) shooter.ShotFired -= OnShot;
    }

    public void SetMoving(bool moving)
    {
        if (hasMoving && !enemy.IsDefeated) animator.SetBool("Moving", moving);
    }

    public bool TriggerMelee()
    {
        if (!isActiveAndEnabled || enemy.IsDefeated || !enemy.CanMelee || !hasMelee) return false;
        if (hasShoot) animator.ResetTrigger("Shoot");
        animator.SetTrigger("Melee");
        return true;
    }

    public bool TriggerRanged()
    {
        if (!CanPlayRanged) return false;
        if (hasMelee) animator.ResetTrigger("Melee");
        if (hasShoot) animator.SetTrigger("Shoot");
        return true;
    }

    private void OnShot(Vector2 direction)
    {
        if (enemy.IsDefeated) return;
        SetAimDirection(direction);
        TriggerRanged();
    }

    public void SetAimDirection(Vector2 direction)
    {
        if (isActiveAndEnabled && !enemy.IsDefeated && hasAimIndex)
            animator.SetInteger("AimIndex", Mathf.Clamp(Mathf.RoundToInt(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg / 45f), 0, 4));
    }

    private void OnDefeated(EnemyBase _) { if (hasDeath) animator.SetTrigger("Death"); }
}
