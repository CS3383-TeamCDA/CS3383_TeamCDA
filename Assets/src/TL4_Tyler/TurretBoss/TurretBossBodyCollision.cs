using UnityEngine;

/// <summary>Solid encounter body, grounded independently of cosmetic controller lift.</summary>
[DefaultExecutionOrder(75)]
public sealed class TurretBossBodyCollision : MonoBehaviour
{
    [SerializeField] private Vector2 worldSize = new Vector2(1.8f, 2.1f);
    private TurretBossController boss;
    private PlayerHealth player;
    private BoxCollider2D shape;
    private Rigidbody2D body;
    private PhysicsMaterial2D material;
    private float floorY;
    public BoxCollider2D Shape => shape;

    public void Initialize(TurretBossController controller, PlayerHealth target, float groundY)
    {
        boss = controller;
        player = target;
        floorY = groundY;
        shape = GetComponent<BoxCollider2D>();
        if (shape == null) { shape = gameObject.AddComponent<BoxCollider2D>(); }
        body = GetComponent<Rigidbody2D>();
        if (body == null) { body = gameObject.AddComponent<Rigidbody2D>(); }
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        material = new PhysicsMaterial2D("Siege Core solid body") { friction = 0f, bounciness = 0f };
        shape.sharedMaterial = material;
        shape.isTrigger = false;
        shape.enabled = true;
        foreach (Collider2D targetShape in player.GetComponentsInChildren<Collider2D>(true))
        {
            Physics2D.IgnoreCollision(shape, targetShape, false);
        }
        boss.BindSolidBody(this);
        SyncShape();
        Physics2D.SyncTransforms();
    }

    public void SyncShape()
    {
        if (shape == null || boss == null) { return; }
        if (boss.IsDefeated)
        {
            shape.enabled = false; // Wreckage is presentation, never a trap or damage source.
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            return;
        }
        transform.rotation = Quaternion.identity;
        Vector3 scale = transform.lossyScale;
        shape.size = new Vector2(worldSize.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            worldSize.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)));
        shape.offset = transform.InverseTransformPoint(new Vector3(transform.position.x,
            floorY + worldSize.y * 0.5f, transform.position.z));
    }

    public Vector3 ConstrainMove(Vector3 desired)
    {
        if (boss == null || boss.IsDefeated || player == null || player.IsDead) { return desired; }
        SyncShape();
        Physics2D.SyncTransforms();
        float start = transform.position.x;
        float dx = desired.x - start;
        foreach (Collider2D targetShape in player.GetComponentsInChildren<Collider2D>())
        {
            if (!targetShape.enabled || targetShape.isTrigger) { continue; }
            Bounds bounds = targetShape.bounds;
            if (bounds.min.y >= floorY + worldSize.y || bounds.max.y <= floorY) { continue; }
            if (dx > 0f && bounds.center.x >= start)
            {
                float limit = bounds.min.x - worldSize.x * 0.5f - 0.015f;
                desired.x = Mathf.Min(desired.x, Mathf.Max(start, limit));
            }
            else if (dx < 0f && bounds.center.x <= start)
            {
                float limit = bounds.max.x + worldSize.x * 0.5f + 0.015f;
                desired.x = Mathf.Max(desired.x, Mathf.Min(start, limit));
            }
        }
        return desired;
    }

    private void FixedUpdate() { SyncShape(); }
    private void LateUpdate() { SyncShape(); }
    private void OnDisable() { if (shape != null) { shape.enabled = false; } }
    private void OnDestroy() { if (material != null) { Destroy(material); } }
}
