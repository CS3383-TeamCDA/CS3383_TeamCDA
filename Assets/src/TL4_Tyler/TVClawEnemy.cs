using EscapeThe90s.Contracts;
using UnityEngine;

public enum TVClawCombatState { Pursuing, SwipeWindup, BiteWindup, PounceWindup, Pouncing, Recovery }

// Four-legged hunter combat; TVClawArtDriver follows these timings.
public class TVClawEnemy : GroundEnemy
{
    [SerializeField, Min(0)] private float biteRange = .65f;
    [SerializeField] private Vector2 biteMouthOffset = new Vector2(.8f, .65f);
    [SerializeField, Min(0)] private float swipeRange = 2.2f;
    [SerializeField] private Vector2 pounceRange = new Vector2(2.8f, 7f);
    [SerializeField, Min(0)] private float baitTime = .7f;
    [SerializeField, Min(0)] private float swipeWindup = .65f;
    [SerializeField, Min(0)] private float biteWindup = .25f;
    [SerializeField, Min(0)] private float pounceWindup = .55f;
    [SerializeField, Min(0)] private float pounceSpeed = 10f;
    [SerializeField, Min(0)] private float pounceJump = 4f;
    [SerializeField, Min(0)] private float pounceDuration = .55f;
    [SerializeField, Min(0)] private float pounceCooldown = 3.5f;
    public TVClawCombatState CombatState { get; private set; }
    public float CombatElapsed => elapsed;
    public float SwipeWindup => swipeWindup;
    public float BiteWindup => biteWindup;
    public float PounceWindup => pounceWindup;
    public float PounceDuration => pounceDuration;
    public TVClawCombatState LastAttack { get; private set; }
    public float LockedDirection { get; private set; }
    protected override bool AvoidAllies => false;
    public override EnemyType Type => EnemyType.TVClaw;
    public override bool CanStartAttack => false; // Own sequence; no duplicate generic melee hits.
    private Rigidbody2D physicsBody;
    private Collider2D hurtbox;
    private EnemyAnimationDriver animations;
    private float elapsed, baitElapsed, cooldown;
    private bool hit;
    private int attackDamage;
    private SpriteRenderer visual;
    private Color normalColor;

    protected override void Awake()
    {
        base.Awake();
        SetAttackMode(EnemyAttackMode.MeleeOnly);
        physicsBody = GetComponent<Rigidbody2D>();
        hurtbox = GetComponent<Collider2D>();
        animations = GetComponent<EnemyAnimationDriver>();
        visual = GetComponent<SpriteRenderer>();
        if (visual != null) normalColor = visual.color;
        var generic = GetComponent<EnemyAttackAnimations>();
        if (generic != null) generic.enabled = false;
        Defeated += _ => { ResetVisual(); StopHorizontal(); };
    }

    protected override void Update() { base.Update(); TickCombat(Time.deltaTime); }

    public void TickCombat(float delta)
    {
        if (delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta) || IsDefeated) return;
        cooldown = Mathf.Max(0, cooldown - delta);
        elapsed += delta;
        if (CombatState == TVClawCombatState.Recovery)
        {
            if (elapsed >= .8f) Enter(TVClawCombatState.Pursuing);
            return;
        }
        if (!HasAggro || !AggroTarget.gameObject.activeInHierarchy)
        {
            if (CombatState != TVClawCombatState.Pursuing) Recover();
            baitElapsed = 0;
            return;
        }
        float distance = Vector2.Distance(transform.position, AggroTarget.position);
        switch (CombatState)
        {
            case TVClawCombatState.Pursuing:
                float direction = AggroTarget.position.x >= transform.position.x ? 1 : -1;
                bool biteReach = CanBite(direction, out _);
                if (biteReach || distance <= swipeRange)
                {
                    LockAttack();
                    Enter(biteReach ? TVClawCombatState.BiteWindup : TVClawCombatState.SwipeWindup);
                }
                else if (distance >= pounceRange.x && distance <= pounceRange.y && cooldown <= 0 && Grounded())
                {
                    baitElapsed += delta;
                    if (baitElapsed >= baitTime) { LockAttack(); Enter(TVClawCombatState.PounceWindup); }
                }
                else baitElapsed = 0;
                break;
            case TVClawCombatState.SwipeWindup:
            case TVClawCombatState.BiteWindup:
                bool bite = CombatState == TVClawCombatState.BiteWindup;
                if (elapsed >= (bite ? biteWindup : swipeWindup))
                {
                    if (animations != null) animations.TriggerMelee();
                    Strike(bite ? biteRange : swipeRange, bite ? Mathf.Max(1, attackDamage / 2) : attackDamage, bite ? 1.5f : 5f, bite);
                    Recover();
                }
                break;
            case TVClawCombatState.PounceWindup:
                if (elapsed >= pounceWindup)
                {
                    if (!Grounded()) { Recover(); break; }
                    Enter(TVClawCombatState.Pouncing);
                    cooldown = pounceCooldown;
                    if (physicsBody != null && physicsBody.simulated)
                        physicsBody.linearVelocity = new Vector2(LockedDirection * pounceSpeed, pounceJump);
                    if (animations != null) animations.TriggerMelee();
                }
                break;
            case TVClawCombatState.Pouncing:
                Strike(1.6f, attackDamage, 5f);
                if (hit || elapsed >= pounceDuration) Recover();
                break;
        }
    }

    public override bool TakeDamage(int amount)
    {
        bool defeated = base.TakeDamage(amount);
        if (!defeated && amount > 0)
        {
            var art = GetComponent<TVClawArtDriver>();
            if (art != null) art.ShowHit();
        }
        return defeated;
    }

    private void LockAttack()
    {
        LockedDirection = AggroTarget.position.x >= transform.position.x ? 1 : -1;
        SetFacingRight(LockedDirection > 0);
        hit = false;
        attackDamage = MeleeDamage;
        baitElapsed = 0;
    }
    private void Enter(TVClawCombatState state)
    {
        if (state == TVClawCombatState.SwipeWindup || state == TVClawCombatState.BiteWindup || state == TVClawCombatState.PounceWindup)
            LastAttack = state;
        CombatState = state;
        elapsed = 0;
        if (visual != null)
            visual.color = GetComponent<TVClawArtDriver>() == null && state == TVClawCombatState.PounceWindup ? new Color(1, .65f, .3f) : normalColor;
    }
    private void ResetVisual() { if (visual != null) visual.color = normalColor; }
    private void Recover() { Enter(TVClawCombatState.Recovery); StopHorizontal(); }
    private void StopHorizontal()
    {
        if (physicsBody != null && physicsBody.simulated)
            physicsBody.linearVelocity = new Vector2(0, physicsBody.linearVelocity.y);
        if (animations != null) animations.SetMoving(false);
    }
    private bool Grounded()
    {
        if (hurtbox == null) return false;
        var bounds = hurtbox.bounds;
        foreach (var contact in Physics2D.RaycastAll(new Vector2(bounds.center.x, bounds.min.y + .05f),
            Vector2.down, .2f, (1 << 0) | (1 << 3)))
            if (Terrain(contact) && contact.normal.y > .5f) return true;
        return false;
    }
    private static bool Terrain(RaycastHit2D contact)
    {
        return contact.collider != null && !contact.collider.isTrigger &&
            contact.collider.GetComponentInParent<EnemyBase>() == null &&
            contact.collider.GetComponentInParent<PlayerHealth>() == null &&
            (contact.rigidbody == null || contact.rigidbody.bodyType == RigidbodyType2D.Static);
    }
    public Vector2 BiteMouthPosition(float direction)
    {
        Vector3 scale = transform.lossyScale;
        return (Vector2)transform.position + new Vector2(direction * biteMouthOffset.x * Mathf.Abs(scale.x),
            biteMouthOffset.y * Mathf.Abs(scale.y));
    }
    private bool CanBite(float direction, out Vector2 point)
    {
        point = HasAggro ? (Vector2)AggroTarget.position : (Vector2)transform.position;
        if (!HasAggro) return false;
        Vector2 mouth = BiteMouthPosition(direction);
        float nearest = float.PositiveInfinity;
        var health = AggroTarget.GetComponentInParent<PlayerHealth>();
        if (health == null || !health.isActiveAndEnabled || health.CurrentHealth <= 0) return false;
        bool hasCollider = false;
        foreach (var collider in health.GetComponentsInChildren<Collider2D>())
        {
            if (!collider.enabled || !collider.gameObject.activeInHierarchy) continue;
            hasCollider = true;
            Vector2 candidate = collider.ClosestPoint(mouth);
            float squared = (candidate - mouth).sqrMagnitude;
            if (squared < nearest) { nearest = squared; point = candidate; }
        }
        if (!hasCollider) nearest = (point - mouth).sqrMagnitude;
        return nearest <= biteRange * biteRange &&
            (AggroTarget.position.x - transform.position.x) * direction >= -.05f;
    }
    private void Strike(float range, int damage, float knockback, bool bite = false)
    {
        if (hit || !HasAggro) return;
        Vector2 target = AggroTarget.position;
        Vector2 origin = (Vector2)transform.position + Vector2.up * .5f;
        if (bite)
        {
            if (!CanBite(LockedDirection, out target)) return;
            origin = BiteMouthPosition(LockedDirection);
        }
        if ((!bite && Vector2.Distance(transform.position, target) > range) ||
            (target.x - transform.position.x) * LockedDirection < -.05f) return;
        foreach (var obstacle in Physics2D.LinecastAll(origin,
            bite ? target : target + Vector2.up * .5f, (1 << 0) | (1 << 3)))
            if (Terrain(obstacle)) return;
        var health = AggroTarget.GetComponentInParent<PlayerHealth>();
        if (health == null || !health.isActiveAndEnabled || health.CurrentHealth <= 0) return;
        hit = true;
        health.TakeDamage(damage);
        var body = health.GetComponent<Rigidbody2D>();
        if (body != null && body.simulated && body.bodyType == RigidbodyType2D.Dynamic)
            body.linearVelocity = new Vector2(LockedDirection * knockback, Mathf.Max(body.linearVelocity.y, 2));
    }
    private void IgnoreEnemyBodies()
    {
        // EnemyBase remains on corpses, so this covers living allies and dead bodies.
        var own = GetComponentsInChildren<Collider2D>();
        foreach (var enemy in FindObjectsByType<EnemyBase>())
        {
            if (enemy == this) continue;
            foreach (var other in enemy.GetComponentsInChildren<Collider2D>())
                foreach (var collider in own) Physics2D.IgnoreCollision(collider, other);
        }
    }
    public override void Move()
    {
        IgnoreEnemyBodies();
        if (CombatState == TVClawCombatState.Pursuing) { base.Move(); return; }
        if (CombatState != TVClawCombatState.Pouncing || IsDefeated) { StopHorizontal(); return; }
        // Stop at terrain walls instead of grinding into them until the timer expires.
        if (hurtbox != null && physicsBody != null)
        {
            foreach (var obstacle in Physics2D.RaycastAll(hurtbox.bounds.center,
                Vector2.right * LockedDirection, hurtbox.bounds.extents.x + pounceSpeed * Time.fixedDeltaTime,
                (1 << 0) | (1 << 3)))
                if (Terrain(obstacle) && Mathf.Abs(obstacle.normal.x) > .5f) { Recover(); break; }
        }
    }
}
