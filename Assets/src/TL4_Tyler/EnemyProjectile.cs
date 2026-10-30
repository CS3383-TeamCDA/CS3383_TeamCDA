using UnityEngine;

// Carries the attack snapshot; player collision/damage integration can consume Damage and Owner.
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private LayerMask blockingLayers = 1 << 3;
    [SerializeField, Min(0)] private float collisionRadius = .05f;
    public EnemyBase Owner { get; private set; }
    public int Damage { get; private set; }
    public Vector2 Direction { get; private set; }
    private float speed;
    private float remainingLife;
    private bool launched;

    public void Launch(EnemyBase owner, Vector2 direction, float movementSpeed, float lifetime)
    {
        Owner = owner;
        Damage = owner.RangedDamage;
        Direction = direction.normalized;
        speed = Mathf.Max(0, movementSpeed);
        remainingLife = Mathf.Max(0.01f, lifetime);
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg);
        launched = true;
    }

    private void Update()
    {
        if (!launched) return;
        float distance = speed * Time.deltaTime;
        // Sweep the whole frame's travel so fast bolts cannot skip thin terrain.
        if (distance > 0 && blockingLayers.value != 0)
        {
            var hit = Physics2D.CircleCast(transform.position, collisionRadius, Direction, distance, blockingLayers);
            if (hit.collider != null)
            {
                transform.position = hit.centroid;
                Destroy(gameObject);
                return;
            }
        }
        transform.position += (Vector3)(Direction * distance);
        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0) Destroy(gameObject);
    }
}
