using UnityEngine;

// Carries the attack snapshot and resolves player/terrain impacts along its travel.
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
    private bool consumed;
    private readonly System.Collections.Generic.List<RaycastHit2D> hits = new System.Collections.Generic.List<RaycastHit2D>();

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

    /// <summary>Applies this launched projectile's damage once to a living player.</summary>
    public bool TryHitPlayer(Collider2D target)
    {
        if (!launched || consumed || target == null || !target.enabled ||
            !target.gameObject.activeInHierarchy || Damage <= 0) return false;
        if (Owner != null && target.transform.IsChildOf(Owner.transform)) return false;
        var health = target.GetComponentInParent<PlayerHealth>();
        if (health == null || !health.isActiveAndEnabled || health.CurrentHealth <= 0) return false;

        // Consume before notifying health listeners, including any reentrant callbacks.
        consumed = true;
        Destroy(gameObject);
        health.TakeDamage(Damage);
        return true;
    }

    private void Update()
    {
        if (!launched || consumed) return;
        float distance = speed * Time.deltaTime;
        // Sweep the whole frame's travel so fast bolts cannot skip thin terrain.
        if (distance > 0)
        {
            var filter = new ContactFilter2D { useTriggers = true };
            Physics2D.CircleCast(transform.position, collisionRadius, Direction, filter, hits, distance);
            hits.Sort((a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) ||
                    (Owner != null && hit.collider.transform.IsChildOf(Owner.transform))) continue;
                if (TryHitPlayer(hit.collider))
                {
                    transform.position = hit.centroid;
                    return;
                }
                if ((blockingLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    transform.position = hit.centroid;
                    consumed = true;
                    Destroy(gameObject);
                    return;
                }
            }
        }
        transform.position += (Vector3)(Direction * distance);
        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0) Destroy(gameObject);
    }
}
