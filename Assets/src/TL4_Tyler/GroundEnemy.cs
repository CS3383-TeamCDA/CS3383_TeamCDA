using UnityEngine;

public class GroundEnemy : EnemyBase
{
    public override EnemyType Type => EnemyType.Ground;
    public override EnemyMovementType MovementType => EnemyMovementType.Ground;

    [SerializeField] private bool facingRight = true;
    [Header("Player pursuit")]
    [SerializeField] private Transform target;
    [SerializeField, Min(0)] private float stoppingDistance = 1;
    [SerializeField, Min(0)] private float acceleration = 20;
    [SerializeField, Min(0)] private float jumpSpeed = 9;
    [SerializeField] private LayerMask groundLayers = 1 << 3;
    [SerializeField, Min(0)] private float crowdGap = .08f;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private EnemyAnimationDriver animationDriver;
    private EnemyAttackAnimations attacks;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    public void SetTarget(Transform player) { target = player; }

    // Read the actual displayed facing, including a mirrored parent transform.
    public bool FacingRight
    {
        get
        {
            var sprite = GetComponent<SpriteRenderer>();
            return sprite != null ? !sprite.flipX == (transform.lossyScale.x >= 0) : facingRight;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animationDriver = GetComponent<EnemyAnimationDriver>();
        attacks = GetComponent<EnemyAttackAnimations>();
        SetFacingRight(facingRight);
    }

    public void SetFacingRight(bool right)
    {
        facingRight = right;
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.flipX = right != (transform.lossyScale.x >= 0);
    }

    protected override bool CalculateShotDirection(Vector2 origin, Vector2 target, out Vector2 direction)
    {
        direction = FacingRight ? Vector2.right : Vector2.left;
        return true;
    }

    private void FixedUpdate() { Move(); }

    private bool HasTerrain(Vector2 direction, float distance, bool floor)
    {
        if (bodyCollider == null) return false;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers);
        filter.useTriggers = false;
        int count = bodyCollider.Cast(direction, filter, hits, distance);
        for (int i = 0; i < count; i++)
            if (floor ? hits[i].normal.y > .5f : Mathf.Abs(hits[i].normal.x) > .5f) return true;
        return false;
    }

    public override void Move()
    {
        // Logic-only enemies and animation previews do not need physics.
        if (body == null || !body.simulated || body.bodyType != RigidbodyType2D.Dynamic) return;
        Vector2 velocity = body.linearVelocity;
        if (attacks != null && attacks.IsMovementLocked)
        {
            body.linearVelocity = new Vector2(0, velocity.y);
            if (animationDriver != null) animationDriver.SetMoving(false);
            return;
        }
        bool pursuing = TryAcquireTarget(target);
        float difference = pursuing ? AggroTarget.position.x - transform.position.x : 0;
        bool moving = pursuing && Mathf.Abs(difference) > stoppingDistance;
        float direction = Mathf.Sign(difference);
        if (moving) SetFacingRight(direction > 0);
        float desiredSpeed = moving ? direction * Mathf.Max(0, speed) : 0;
        // The narrow body allows sprite overlap; match a nearby ally instead of pushing it.
        bool crowded = false;
        if (moving && bodyCollider != null)
        {
            var filter = ContactFilter2D.noFilter;
            filter.useTriggers = false;
            int count = bodyCollider.Cast(Vector2.right * direction, filter, hits,
                crowdGap + Mathf.Abs(velocity.x) * Time.fixedDeltaTime);
            for (int i = 0; i < count; i++)
            {
                var ally = hits[i].collider.GetComponentInParent<GroundEnemy>();
                if (ally == null || ally == this || hits[i].normal.x * direction >= -.5f) continue;
                var allyBody = hits[i].rigidbody;
                float allySpeed = allyBody != null ? allyBody.linearVelocity.x * direction : 0;
                desiredSpeed = direction * Mathf.Min(Mathf.Abs(desiredSpeed), Mathf.Max(0, allySpeed));
                crowded = true;
            }
        }
        velocity.x = IsDefeated ? 0 : Mathf.MoveTowards(velocity.x,
            desiredSpeed, acceleration * Time.fixedDeltaTime);
        if (crowded) velocity.x = desiredSpeed;
        // Jump only from the ground when a vertical terrain face blocks pursuit.
        if (moving && velocity.y <= .1f && HasTerrain(Vector2.down, .12f, true) &&
            HasTerrain(Vector2.right * direction, .25f, false)) velocity.y = jumpSpeed;
        body.linearVelocity = velocity;
        if (animationDriver != null) animationDriver.SetMoving(moving && Mathf.Abs(velocity.x) > .05f);
    }
}
