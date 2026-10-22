using UnityEngine;

[RequireComponent(typeof(EnemyBase))]
public class EnemyRangedAttack : MonoBehaviour
{
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float shotInterval = 1f;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 8f;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 4f;
    public event System.Action<Vector2> ShotFired;
    private float nextShotTime;
    private EnemyBase enemy;

    public void SetTarget(Transform value) { target = value; }
    public void SetProjectilePrefab(EnemyProjectile value) { projectilePrefab = value; }
    public void SetMuzzle(Transform value) { muzzle = value; }

    private void Update()
    {
        if (target != null) TryShoot(target.position, out _);
    }

    public bool TryShoot(Vector2 targetPosition, out EnemyProjectile projectile)
    {
        projectile = null;
        if (!isActiveAndEnabled || projectilePrefab == null || Time.time < nextShotTime) return false;
        if (enemy == null) enemy = GetComponent<EnemyBase>();
        Vector3 origin = muzzle != null ? muzzle.position : transform.position;
        if (!enemy.TryGetShotDirection(origin, targetPosition, out Vector2 direction)) return false;
        projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(enemy, direction, projectileSpeed, projectileLifetime);
        nextShotTime = Time.time + Mathf.Max(0.01f, shotInterval);
        ShotFired?.Invoke(direction);
        return true;
    }
}
