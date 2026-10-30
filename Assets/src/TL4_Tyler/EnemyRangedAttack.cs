using UnityEngine;

[RequireComponent(typeof(EnemyBase))]
public class EnemyRangedAttack : MonoBehaviour
{
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float shotInterval = 1f;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 7.2f;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 4f;
    public event System.Action<Vector2> ShotFired;
    private float nextShotTime;
    private EnemyBase enemy;
    public float TimeUntilNextShot => Mathf.Max(0, nextShotTime - Time.time);
    public Vector3 ShotOrigin
    {
        get
        {
            var owner = enemy != null ? enemy : GetComponent<EnemyBase>();
            Vector3 origin = muzzle != null ? muzzle.position : transform.position;
            // SpriteRenderer.flipX does not mirror child transforms such as the muzzle.
            if (owner is GroundEnemy ground && muzzle != null && muzzle.IsChildOf(transform))
                origin.x = transform.position.x + Mathf.Abs(origin.x - transform.position.x) * (ground.FacingRight ? 1 : -1);
            if (owner is FlyingEnemy flying && muzzle != null && muzzle.IsChildOf(transform))
                origin.x = transform.position.x + Mathf.Abs(origin.x - transform.position.x) * (flying.FacingRight ? 1 : -1);
            return origin;
        }
    }

    public void SetTarget(Transform value) { target = value; }
    public void SetProjectilePrefab(EnemyProjectile value) { projectilePrefab = value; }
    public void SetMuzzle(Transform value) { muzzle = value; }

    private void Update()
    {
        // A sequence controller owns timing when present; avoid a second automatic fire loop.
        if (target != null && GetComponent<EnemyAttackAnimations>() == null) TryShoot(target.position, out _);
    }

    public bool TryShoot(Vector2 targetPosition, out EnemyProjectile projectile)
    {
        projectile = null;
        if (!isActiveAndEnabled || projectilePrefab == null || Time.time < nextShotTime) return false;
        if (enemy == null) enemy = GetComponent<EnemyBase>();
        Vector3 origin = ShotOrigin;
        if (!enemy.TryGetShotDirection(origin, targetPosition, out Vector2 direction)) return false;
        projectile = Instantiate(projectilePrefab, origin, Quaternion.identity, transform.parent);
        projectile.Launch(enemy, direction, projectileSpeed, projectileLifetime);
        nextShotTime = Time.time + Mathf.Max(0.01f, shotInterval);
        ShotFired?.Invoke(direction);
        return true;
    }
}
