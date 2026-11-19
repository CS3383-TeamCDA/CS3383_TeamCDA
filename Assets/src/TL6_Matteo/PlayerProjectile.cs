using UnityEngine;
using EscapeThe90s.Contracts;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] private float _speed = 30f;
    [SerializeField] private float _lifetime = 3f;
    [SerializeField, Min(1)] private int _baseDamage = 10;

    private Rigidbody2D _rb;
    private int _damage;
    private bool _hasImpacted;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _damage = _baseDamage;
    }

    private void Start()
    {
        Destroy(gameObject, _lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasImpacted)
            return;

        bool hitGround = other.gameObject.layer == LayerMask.NameToLayer("Ground");
        IEnemyDamageable enemy = hitGround ? null : other.GetComponentInParent<IEnemyDamageable>();
        if (!hitGround && enemy == null)
            return;

        _hasImpacted = true;
        enemy?.TakeDamage(_damage);
        Destroy(gameObject);
    }

    public void Initialize(Vector2 direction)
    {
        Initialize(direction, 0);
    }

    public void Initialize(Vector2 direction, int bonusDamage)
    {
        _damage = _baseDamage + bonusDamage;
        _rb.linearVelocity = direction.normalized * _speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
