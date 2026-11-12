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
    float flipRemaining, flipDirection, landRemaining, hurtRemaining, deathTime;
    float previousFallSpeed;
    int previousHealth;
    Rigidbody2D body;
    public bool Invulnerable => hurtRemaining > 0 && !health.IsDead;

    public void Initialize(TwoHandedRig visual, PlayerHealth playerHealth)
    {
        rig = visual; health = playerHealth;
        body = GetComponent<Rigidbody2D>();
        pivot = new GameObject("Flip and impact pivot").transform;
        pivot.SetParent(transform, false);
        rig.transform.SetParent(pivot, true);
        rigPosition = rig.transform.localPosition;
        renderers = rig.GetComponentsInChildren<SpriteRenderer>();
        colors = new Color[renderers.Length];
        for (int i = 0; i < colors.Length; i++) colors[i] = renderers[i].color;
        previousHealth = health.CurrentHealth;
        health.OnHealthChanged += HealthChanged;
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
        if (previousFallSpeed < -3f) landRemaining = Mathf.Clamp(-previousFallSpeed / 80f, .09f, .18f);
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
        float squash = landRemaining > 0 ? Mathf.Sin(Mathf.Clamp01(landRemaining / .18f) * Mathf.PI) * .14f : 0;
        if (health.IsDead)
        {
            deathTime = Mathf.Min(.3f, deathTime + Time.deltaTime);
            angle = Mathf.Lerp(0, rig.FacingRight ? -85f : 85f, deathTime / .3f);
            squash = .25f;
        }
        else deathTime = 0;
        pivot.localRotation = Quaternion.Euler(0, 0, angle);
        pivot.localScale = new Vector3(1 + squash * .5f, 1 - squash, 1);
        // Keep feet planted when compressing; rotation is about the player's center.
        pivot.localPosition = Vector3.down * (.5f * squash + (health.IsDead ? deathTime : 0));
        rig.transform.localPosition = rigPosition;
        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = health.IsDead ? Color.Lerp(colors[i], Color.gray, .65f) : colors[i];
            if (hurtRemaining > .57f) color = Color.white;
            else if (hurtRemaining > 0) color.a *= Mathf.FloorToInt(hurtRemaining * 18) % 2 == 0 ? .35f : 1;
            renderers[i].color = color;
        }
    }

    public void ResetFeedback()
    {
        flipRemaining = landRemaining = hurtRemaining = deathTime = 0;
        previousFallSpeed = 0;
        pivot.localPosition = Vector3.zero; pivot.localRotation = Quaternion.identity; pivot.localScale = Vector3.one;
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = colors[i];
    }

    void OnDestroy() { if (health != null) health.OnHealthChanged -= HealthChanged; }
}
