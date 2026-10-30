using UnityEngine;

public class TurretHazard : EnemyBase
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 targetAimOffset = new Vector2(0, .9f);
    private EnemyRangedAttack shooter;
    private EnemyAnimationDriver animationDriver;
    public void SetTarget(Transform player) { target = player; }
    public override Vector3 AttackTargetPosition => base.AttackTargetPosition + (Vector3)targetAimOffset;

    protected override void Awake()
    {
        base.Awake();
        shooter = GetComponent<EnemyRangedAttack>();
        animationDriver = GetComponent<EnemyAnimationDriver>();
    }

    protected override void Update()
    {
        base.Update();
        if (TryAcquireTarget(target) && shooter != null && animationDriver != null &&
            TryGetShotDirection(shooter.ShotOrigin, AttackTargetPosition, out var direction))
            animationDriver.SetAimDirection(direction);
    }
    public override EnemyType Type => EnemyType.Turret;
    public override EnemyMovementType MovementType => EnemyMovementType.Static;

    protected override bool CalculateShotDirection(Vector2 origin, Vector2 target, out Vector2 direction)
    {
        Vector2 offset = target - origin;
        direction = Vector2.zero;
        // World-space upper semicircle: both horizontal endpoints are included.
        if (offset.y < 0 || offset.sqrMagnitude < 0.000001f) return false;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        // Midpoints round toward the larger angle (22.5 -> 45, for example).
        int slot = Mathf.Clamp(Mathf.FloorToInt(angle / 45f + 0.5f), 0, 4);
        switch (slot)
        {
            case 0: direction = Vector2.right; break;
            case 1: direction = new Vector2(1, 1).normalized; break;
            case 2: direction = Vector2.up; break;
            case 3: direction = new Vector2(-1, 1).normalized; break;
            default: direction = Vector2.left; break;
        }
        return true;
    }

    public override void Move()
    {
        // Turrets remain fixed at their generated terrain location.
    }

}
