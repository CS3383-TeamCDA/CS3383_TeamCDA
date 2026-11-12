using UnityEngine;
using TwoHandedHero;

/// <summary>Visual-only flip, impact compression and damage/death response.</summary>
[DefaultExecutionOrder(400)]
public sealed class PlayerSandboxFeedback : MonoBehaviour
{
    Transform pivot;
    TwoHandedRig rig;
    PlayerHealth health;
    SpriteRenderer[] renderers;
    Color[] colors;
    Vector3 rigPosition;
    float flipRemaining, flipDirection, landRemaining, hurtRemaining;
    float previousFallSpeed;
    int previousHealth;
    Rigidbody2D body;
    bool dashing;
    float landDuration, landStrength;
    CapsuleCollider2D capsule;
    Vector2 livingSize, livingOffset;
    CapsuleDirection2D livingDirection;
    float livingDamping, baseGravity;
    int deathFacing;
    bool deathStarted, deathSettled;
    public bool Invulnerable => hurtRemaining > 0 && !health.IsDead;

    public void Initialize(TwoHandedRig visual, PlayerHealth playerHealth)
    {
        rig = visual; health = playerHealth;
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        livingSize = capsule.size; livingOffset = capsule.offset; livingDirection = capsule.direction;
        livingDamping = body.linearDamping; baseGravity = body.gravityScale;
        pivot = new GameObject("Flip and impact pivot").transform;
        pivot.SetParent(transform, false);
        rig.transform.SetParent(pivot, true);
        rigPosition = rig.transform.localPosition;
        renderers = rig.GetComponentsInChildren<SpriteRenderer>();
        colors = new Color[renderers.Length];
        for (int i = 0; i < colors.Length; i++) colors[i] = renderers[i].color;
        previousHealth = health.CurrentHealth;
        health.OnHealthChanged += HealthChanged;
        health.OnPlayerDied += BeginDeath;
    }

    void HealthChanged(int current, int max)
    {
        if (current < previousHealth) hurtRemaining = .7f;
        previousHealth = current;
    }

    public void BeginFlip(int facing)
    {
        flipRemaining = .38f;
        flipDirection = facing >= 0 ? -1f : 1f;
        GetComponent<PlayerMotionEffects>().AirJumpBurst();
    }

    public void Land()
    {
        flipRemaining = 0;
        if (previousFallSpeed < -3f)
        {
            landRemaining = landDuration = Mathf.Clamp(-previousFallSpeed / 80f, .12f, .18f);
            landStrength = Mathf.Lerp(.06f, .12f, Mathf.InverseLerp(3, 15, -previousFallSpeed));
            rig.BeginLanding(landDuration);
        }
    }

    public void BeginDash() { flipRemaining = landRemaining = 0; }
    public void SetDashing(bool value) { dashing = value; }

    void BeginDeath()
    {
        deathStarted = true; deathSettled = false;
        deathFacing = rig.FacingRight ? 1 : -1;
        flipRemaining = landRemaining = hurtRemaining = 0;
        dashing = false;
        rig.SetDeathPose(true);
        // Keep the same bottom edge while giving the horizontal body a low hurtbox.
        capsule.direction = CapsuleDirection2D.Horizontal;
        capsule.size = new Vector2(.7f, .32f);
        capsule.offset = new Vector2(0, -.34f);
        body.gravityScale = baseGravity;
        body.linearDamping = 4f;
        body.linearVelocity = new Vector2(-deathFacing * .8f, Mathf.Min(0, body.linearVelocity.y));
        body.WakeUp();
    }

    void FixedUpdate()
    {
        if (!deathStarted || !health.IsDead || deathSettled || body.linearVelocity.y > 0) return;
        Bounds bounds = capsule.bounds;
        Vector2 foot = new Vector2(bounds.center.x, bounds.min.y);
        bool touching = Physics2D.OverlapBox(foot - Vector2.up * .015f,
            new Vector2(bounds.size.x * .8f, .05f), 0, LayerMask.GetMask("Ground")) != null;
        if (!touching) return;
        GetComponent<PlayerMotionEffects>().DeathImpactBurst();
        var ground = Physics2D.Raycast(foot + Vector2.up * .1f, Vector2.down, .2f, LayerMask.GetMask("Ground"));
        if (ground.collider != null) body.position += Vector2.up * (ground.point.y - foot.y + .005f);
        deathSettled = true;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = 0;
        body.Sleep();
    }

    void Update()
    {
        if (rig == null) return;
        previousFallSpeed = body.linearVelocity.y;
    }

    void LateUpdate()
    {
        if (rig == null) return;
        hurtRemaining = Mathf.Max(0, hurtRemaining - Time.deltaTime);
        landRemaining = Mathf.Max(0, landRemaining - Time.deltaTime);
        flipRemaining = Mathf.Max(0, flipRemaining - Time.deltaTime);
        float angle = flipRemaining > 0 ? flipDirection * 360f * (1 - flipRemaining / .38f) : 0;
        float squash = landRemaining > 0 ? (landRemaining / landDuration) * landStrength : 0;
        // The authored death clip supplies the collapse and rebound. Never
        // rotate the assembled weapon rig or squash a standing sprite sideways.
        if (health.IsDead) { angle = 0; squash = 0; }
        pivot.localRotation = Quaternion.Euler(0, 0, angle);
        pivot.localScale = new Vector3(1 + squash * .5f, 1 - squash, 1);
        if (dashing && !health.IsDead) pivot.localScale = new Vector3(1.08f, .94f, 1);
        // Keep feet planted when compressing; rotation is about the player's center.
        pivot.localPosition = Vector3.down * (.5f * squash);
        rig.transform.localPosition = rigPosition;
        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = health.IsDead ? Color.Lerp(colors[i], Color.gray, .25f) : colors[i];
            if (hurtRemaining > .57f) color = Color.white;
            else if (hurtRemaining > 0) color.a *= Mathf.FloorToInt(hurtRemaining * 18) % 2 == 0 ? .35f : 1;
            renderers[i].color = color;
        }
    }

    public void ResetFeedback()
    {
        flipRemaining = landRemaining = hurtRemaining = 0;
        dashing = false;
        deathStarted = deathSettled = false;
        capsule.direction = livingDirection; capsule.size = livingSize; capsule.offset = livingOffset;
        body.linearDamping = livingDamping;
        body.gravityScale = baseGravity;
        body.WakeUp();
        previousFallSpeed = 0;
        rig.BeginLanding(0);
        rig.SetDeathPose(false);
        pivot.localPosition = Vector3.zero; pivot.localRotation = Quaternion.identity; pivot.localScale = Vector3.one;
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = colors[i];
    }

    void OnDestroy()
    {
        if (health == null) return;
        health.OnHealthChanged -= HealthChanged;
        health.OnPlayerDied -= BeginDeath;
    }
}
