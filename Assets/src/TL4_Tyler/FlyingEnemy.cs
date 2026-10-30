using UnityEngine;

public class FlyingEnemy : EnemyBase
{
    public override EnemyType Type => EnemyType.Flying;
    public override EnemyMovementType MovementType => EnemyMovementType.Flying;
    [Header("Flight pursuit")]
    [SerializeField] private Transform target;
    [Header("Player-relative firing box")]
    [SerializeField] private Vector2 horizontalDistance = new Vector2(3, 6);
    [SerializeField] private Vector2 heightRange = new Vector2(1.5f, 4);
    [SerializeField, Min(.1f)] private float minimumRepositionDistance = 1;
    [SerializeField, Min(1)] private float repickAfterSeconds = 5;
    private Vector2 destinationOffset, lastPosition;
    private bool hasDestination;
    private float distanceTravelled, requiredTravel, repositionElapsed;
    private EnemyAttackAnimations attacks;
    public Vector2 FlightDestination => (Vector2)(HasAggro ? AggroTarget.position : transform.position) + destinationOffset;
    public override Vector3 AttackTargetPosition => base.AttackTargetPosition + Vector3.up * .8f;
    public override bool CanAttackTarget
    {
        get
        {
            if (!HasAggro) return false;
            Vector2 relative = transform.position - AggroTarget.position;
            return Mathf.Abs(relative.x) >= Mathf.Max(0, horizontalDistance.x) &&
                Mathf.Abs(relative.x) <= Mathf.Max(horizontalDistance.x, horizontalDistance.y) &&
                relative.y >= heightRange.x && relative.y <= Mathf.Max(heightRange.x, heightRange.y);
        }
    }
    public override bool CanStartAttack => body != null && CanAttackTarget && hasDestination &&
        Vector2.Distance(body.position, FlightDestination) <= stoppingDistance &&
        distanceTravelled >= requiredTravel && body.linearVelocity.sqrMagnitude < .04f;
    public bool FacingRight => sprite == null || !sprite.flipX == (transform.lossyScale.x >= 0);
    public void SetFacingRight(bool right)
    {
        if (sprite != null) sprite.flipX = right != (transform.lossyScale.x >= 0);
    }
    [SerializeField, Min(0)] private float stoppingDistance = .6f;
    [SerializeField, Min(0)] private float acceleration = 8;
    [SerializeField] private LayerMask terrainLayers = 1 << 3;
    [SerializeField, Min(.1f)] private float obstacleLookAhead = .8f;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private SpriteRenderer sprite;
    private EnemyAnimationDriver animationDriver;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    public void SetTarget(Transform player) { target = player; }

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        animationDriver = GetComponent<EnemyAnimationDriver>();
        attacks = GetComponent<EnemyAttackAnimations>();
        lastPosition = transform.position;
    }

    private void OnEnable()
    {
        if (attacks != null) attacks.SequenceCompleted += Reposition;
    }
    private void OnDisable()
    {
        if (attacks != null) attacks.SequenceCompleted -= Reposition;
    }
    private void Reposition() { ChooseDestination(true); }

    private void ChooseDestination(bool requireMovement)
    {
        if (!HasAggro) return;
        Vector2 relative = transform.position - AggroTarget.position;
        float side = relative.x < 0 ? -1 : 1;
        if (requireMovement && Random.value < .3f) side = -side;
        float near = Mathf.Max(.5f, horizontalDistance.x) + stoppingDistance;
        float far = Mathf.Max(near, horizontalDistance.y - stoppingDistance);
        float low = heightRange.x + stoppingDistance;
        float high = Mathf.Max(low, heightRange.y - stoppingDistance);
        Vector2 chosen = Vector2.zero;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            chosen = new Vector2(side * Random.Range(near, far), Random.Range(low, high));
            Vector2 world = (Vector2)AggroTarget.position + chosen;
            if (Physics2D.OverlapBox(world, new Vector2(1, 1), 0, terrainLayers) != null) continue;
            if (!requireMovement || Vector2.Distance(world, body.position) >= minimumRepositionDistance + stoppingDistance) break;
        }
        // Guarantee a meaningful new perch even if random samples bunch up.
        if (requireMovement && Vector2.Distance((Vector2)AggroTarget.position + chosen, body.position) < minimumRepositionDistance + stoppingDistance)
            chosen = new Vector2(-Mathf.Sign(relative.x == 0 ? 1 : relative.x) * far, high);
        destinationOffset = chosen;
        hasDestination = true;
        distanceTravelled = 0;
        requiredTravel = requireMovement ? minimumRepositionDistance : 0;
        repositionElapsed = 0;
        lastPosition = body.position;
    }

    private void FixedUpdate() { Move(); }

    private bool TerrainAhead(Vector2 direction, float distance)
    {
        if (bodyCollider == null) return false;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(terrainLayers);
        filter.useTriggers = false;
        int count = bodyCollider.Cast(direction, filter, hits, distance);
        for (int i = 0; i < count; i++)
            if (Vector2.Dot(hits[i].normal, direction) < -.5f) return true;
        return false;
    }

    public override void Move()
    {
        if (body == null || !body.simulated || body.bodyType != RigidbodyType2D.Dynamic) return;
        if (attacks != null && attacks.IsMovementLocked)
        {
            body.linearVelocity = Vector2.zero;
            lastPosition = body.position;
            if (animationDriver != null) animationDriver.SetMoving(false);
            return;
        }
        distanceTravelled += Vector2.Distance(body.position, lastPosition);
        lastPosition = body.position;
        Vector2 desired = Vector2.zero;
        bool pursuing = TryAcquireTarget(target);
        if (pursuing)
        {
            if (!hasDestination) ChooseDestination(false);
            repositionElapsed += Time.fixedDeltaTime;
            if (repositionElapsed >= repickAfterSeconds && !CanStartAttack) ChooseDestination(true);
            Vector2 offset = FlightDestination - body.position;
            if (offset.magnitude > stoppingDistance)
            {
                desired = offset.normalized * Mathf.Max(0, speed);
                // Climb terrain faces rather than flying through them.
                if (Mathf.Abs(desired.x) > .05f && TerrainAhead(Vector2.right * Mathf.Sign(desired.x), obstacleLookAhead))
                    desired = Vector2.up * Mathf.Max(0, speed);
                if (TerrainAhead(Vector2.down, .35f)) desired.y = Mathf.Max(desired.y, speed * .5f);
                if (desired.y > 0 && TerrainAhead(Vector2.up, .25f)) desired.y = 0;
            }
            float targetSide = AggroTarget.position.x - transform.position.x;
            if (Mathf.Abs(targetSide) > .05f) SetFacingRight(targetSide > 0);
        }
        body.linearVelocity = IsDefeated ? Vector2.zero : Vector2.MoveTowards(body.linearVelocity,
            desired, Mathf.Max(0, acceleration) * Time.fixedDeltaTime);
        if (animationDriver != null) animationDriver.SetMoving(pursuing && body.linearVelocity.sqrMagnitude > .01f);
    }
}
