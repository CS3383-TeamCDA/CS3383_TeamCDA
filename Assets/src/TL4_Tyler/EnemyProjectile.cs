using UnityEngine;

// Carries the attack snapshot; player collision/damage integration can consume Damage and Owner.
public class EnemyProjectile : MonoBehaviour
{
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
        transform.position += (Vector3)(Direction * speed * Time.deltaTime);
        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0) Destroy(gameObject);
    }
}
