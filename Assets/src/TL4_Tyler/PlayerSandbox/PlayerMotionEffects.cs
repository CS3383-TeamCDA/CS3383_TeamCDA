using UnityEngine;
using TwoHandedHero;

/// <summary>Local sandbox contact shadow, foot dust and airborne afterimages.</summary>
[DefaultExecutionOrder(550)]
public sealed class PlayerMotionEffects : MonoBehaviour
{
    sealed class Puff
    {
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float life, duration, size, opacity;
    }
    readonly Puff[] puffs = new Puff[80];
    Rigidbody2D body;
    Collider2D capsule;
    TwoHandedRig rig;
    SpriteRenderer shadow, bodyRenderer;
    Sprite pixel, oval;
    Texture2D ovalTexture;
    Transform effects;
    int cursor, groundMask;
    bool wasGrounded;
    float stepTime, ghostTime, lastFallSpeed;
    PlayerSandboxMotor motor;
    SpriteRenderer[] characterParts;

    public void Initialize(Rigidbody2D playerBody, TwoHandedRig visual)
    {
        body = playerBody;
        capsule = body.GetComponent<Collider2D>();
        rig = visual;
        bodyRenderer = rig.bodyAnimator.transform.Find("Body").GetComponent<SpriteRenderer>();
        groundMask = LayerMask.GetMask("Ground");
        effects = new GameObject("Player motion effects").transform;
        effects.SetParent(transform.parent, false);
        pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width,
            Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
        ovalTexture = new Texture2D(32, 12, TextureFormat.RGBA32, false);
        ovalTexture.filterMode = FilterMode.Point;
        var colors = new Color[32 * 12];
        for (int y = 0; y < 12; y++) for (int x = 0; x < 32; x++)
        {
            float radius = Mathf.Pow((x - 15.5f) / 16f, 2) + Mathf.Pow((y - 5.5f) / 6f, 2);
            colors[y * 32 + x] = radius <= 1 ? Color.white : Color.clear;
        }
        ovalTexture.SetPixels(colors); ovalTexture.Apply();
        oval = Sprite.Create(ovalTexture, new Rect(0, 0, 32, 12), Vector2.one * .5f, 32);
        shadow = new GameObject("Ground contact shadow", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        shadow.transform.SetParent(effects, false);
        shadow.sprite = oval;
        shadow.sortingOrder = 1;
        for (int i = 0; i < puffs.Length; i++)
        {
            var renderer = new GameObject("Dust / afterimage", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(effects, false);
            renderer.enabled = false;
            puffs[i] = new Puff { renderer = renderer };
        }
    }

    void LateUpdate()
    {
        if (body == null) return;
        if (motor == null) motor = GetComponent<PlayerSandboxMotor>();
        if (characterParts == null) characterParts = rig.GetComponentsInChildren<SpriteRenderer>();
        Vector2 foot = new Vector2(capsule.bounds.center.x, capsule.bounds.min.y);
        var hit = Physics2D.Raycast(foot + Vector2.up * .08f, Vector2.down, 12f, groundMask);
        bool grounded = hit.collider != null && hit.distance <= .16f && body.linearVelocity.y <= .1f;
        shadow.enabled = hit.collider != null;
        if (shadow.enabled)
        {
            float height = Mathf.Max(0, foot.y - hit.point.y);
            shadow.transform.position = hit.point + Vector2.up * .015f;
            shadow.transform.localScale = Vector3.one * Mathf.Lerp(.9f, .35f, Mathf.Clamp01(height / 5));
            shadow.color = new Color(0, 0, 0, Mathf.Lerp(.38f, .08f, Mathf.Clamp01(height / 5)));
        }
        if (wasGrounded && !grounded && body.linearVelocity.y > 1f) Dust(foot, 10, 1f);
        if (!wasGrounded && grounded && lastFallSpeed < -1f) Dust(foot, 16, Mathf.Clamp(-lastFallSpeed / 8, .8f, 1.8f));
        stepTime -= Time.deltaTime;
        if (grounded && Mathf.Abs(body.linearVelocity.x) > 1f && stepTime <= 0)
        {
            Dust(foot - Vector2.right * Mathf.Sign(body.linearVelocity.x) * .15f, 3, .55f);
            stepTime = .11f;
        }
        ghostTime -= Time.deltaTime;
        if (motor != null && motor.IsDashing && ghostTime <= 0)
        {
            foreach (var part in characterParts)
            {
                if (!part.enabled || part.sprite == null || part == rig.muzzleFlash || part.transform.IsChildOf(rig.muzzle)) continue;
                var puff = Next(.16f);
                puff.renderer.sprite = part.sprite;
                puff.renderer.transform.SetPositionAndRotation(part.transform.position, part.transform.rotation);
                puff.renderer.transform.localScale = part.transform.lossyScale;
                puff.renderer.flipX = part.flipX; puff.renderer.flipY = part.flipY;
                puff.renderer.sortingOrder = 3;
                puff.renderer.color = new Color(.3f, .65f, 1f, .3f);
                puff.opacity = .3f;
                puff.size = 0; puff.velocity = Vector2.zero;
            }
            ghostTime = .025f;
        }
        foreach (var puff in puffs)
        {
            if (puff.life <= 0) continue;
            puff.life -= Time.deltaTime;
            puff.renderer.enabled = puff.life > 0;
            puff.renderer.transform.position += (Vector3)(puff.velocity * Time.deltaTime);
            Color color = puff.renderer.color;
            color.a = Mathf.Max(0, puff.life / puff.duration) * puff.opacity;
            puff.renderer.color = color;
            if (puff.size > 0)
            {
                puff.velocity += Vector2.up * .25f * Time.deltaTime;
                puff.renderer.transform.localScale = Vector3.one * puff.size * (1 + (1 - puff.life / puff.duration) * .8f);
            }
        }
        wasGrounded = grounded;
        lastFallSpeed = body.linearVelocity.y;
    }

    Puff Next(float duration)
    {
        var puff = puffs[cursor++ % puffs.Length];
        puff.life = puff.duration = duration;
        puff.opacity = .55f;
        puff.renderer.flipX = puff.renderer.flipY = false;
        puff.renderer.enabled = true;
        return puff;
    }

    public void DashBurst(int direction)
    {
        // A flattened launch ring and short speed streaks remain where the dash began.
        Vector2 origin = body.position - Vector2.right * direction * .25f;
        for (int i = 0; i < 16; i++)
        {
            var puff = Next(.24f);
            float angle = i * Mathf.PI * 2 / 16;
            Vector2 ring = new Vector2(Mathf.Cos(angle) * .12f, Mathf.Sin(angle) * .38f);
            puff.renderer.sprite = pixel;
            puff.renderer.sortingOrder = 5;
            puff.renderer.transform.position = origin + ring;
            puff.renderer.transform.rotation = Quaternion.identity;
            puff.size = .055f;
            puff.velocity = new Vector2(-direction * .8f, ring.y * 2f);
            puff.renderer.color = new Color(.65f, .88f, 1f, .7f);
            puff.opacity = .7f;
        }
        for (int i = 0; i < 6; i++)
        {
            var puff = Next(.15f + i * .012f);
            puff.renderer.sprite = pixel;
            puff.renderer.sortingOrder = 4;
            puff.renderer.transform.position = origin + new Vector2(-direction * Random.Range(.2f, .65f), Random.Range(-.3f, .3f));
            puff.renderer.transform.rotation = Quaternion.identity;
            puff.renderer.transform.localScale = new Vector3(Random.Range(.3f, .65f), .02f, 1);
            puff.size = 0; puff.velocity = Vector2.left * direction * 2;
            puff.renderer.color = new Color(.5f, .8f, 1, .5f);
            puff.opacity = .5f;
        }
    }

    public void AirJumpBurst()
    {
        for (int i = 0; i < 12; i++)
        {
            var puff = Next(.3f);
            float angle = i * Mathf.PI * 2 / 12;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            puff.renderer.sprite = pixel;
            puff.renderer.sortingOrder = 6;
            puff.renderer.transform.position = body.position + direction * .12f;
            puff.renderer.transform.rotation = Quaternion.identity;
            puff.size = .035f;
            puff.velocity = direction * 1.5f;
            puff.renderer.color = new Color(.2f, .8f, 1f, .55f);
        }
    }

    public void DeathImpactBurst()
    {
        Dust(new Vector2(capsule.bounds.center.x, capsule.bounds.min.y), 8, .8f);
    }

    void Dust(Vector2 foot, int count, float strength)
    {
        for (int i = 0; i < count; i++)
        {
            var puff = Next(Random.Range(.2f, .4f));
            puff.renderer.sprite = pixel;
            puff.renderer.sortingOrder = 6;
            puff.renderer.transform.rotation = Quaternion.identity;
            puff.renderer.transform.position = foot + new Vector2(Random.Range(-.15f, .15f), .025f);
            puff.size = Random.Range(.025f, .065f) * strength;
            puff.renderer.transform.localScale = Vector3.one * puff.size;
            puff.velocity = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(.2f, .8f)) * strength;
            puff.renderer.color = new Color(.65f, .65f, .72f, .55f);
        }
    }

    void OnDestroy()
    {
        if (effects != null) Destroy(effects.gameObject);
        if (pixel != null) Destroy(pixel);
        if (oval != null) Destroy(oval);
        if (ovalTexture != null) Destroy(ovalTexture);
    }
}
