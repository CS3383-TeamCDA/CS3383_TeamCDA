using UnityEngine;

[RequireComponent(typeof(EnemyBase), typeof(Animator))]
public class EnemyAnimationDriver : MonoBehaviour
{
    private Animator animator;
    private EnemyBase enemy;
    private EnemyRangedAttack shooter;
    private bool hasAimIndex, hasShoot, hasMoving, hasDeath;

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

    private void OnShot(Vector2 direction)
    {
        if (enemy.IsDefeated) return;
        if (hasAimIndex) animator.SetInteger("AimIndex", Mathf.Clamp(Mathf.RoundToInt(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg / 45f), 0, 4));
        if (hasShoot) animator.SetTrigger("Shoot");
    }

    private void OnDefeated(EnemyBase _) { if (hasDeath) animator.SetTrigger("Death"); }
}
