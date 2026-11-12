using UnityEngine;

// Ballistic, terrain-colliding grenade. Damage is captured at launch, survives its owner.
public class BerserkerGrenade : MonoBehaviour
{
    private float fuse;
    private float radius;
    private int damage;
    private bool exploded;
    private TurretBossVfxSet vfx;
    private SpriteRenderer visual;
    private float age;
    public void Launch(Vector2 destination, float flightTime, float blastRadius, int blastDamage, TurretBossVfxSet visuals = null)
    {
        fuse = Mathf.Max(.2f, flightTime);
        radius = blastRadius;
        damage = blastDamage;
        vfx = visuals;
        visual = GetComponentInChildren<SpriteRenderer>();
        Draw();
        var body = gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 1;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.radius = .12f;
        foreach (var enemy in FindObjectsByType<EnemyBase>())
            foreach (var other in enemy.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(collider, other);
        body.linearVelocity = (destination - (Vector2)transform.position) / fuse - Physics2D.gravity * (.5f * fuse);
    }
    private void Update()
    {
        fuse -= Time.deltaTime;
        age += Time.deltaTime;
        Draw();
        if (fuse > 0 || exploded) return;
        exploded = true;
        EnemyBlast.Detonate(transform.position, radius, damage, vfx: vfx);
        Destroy(gameObject);
    }
    private void Draw()
    {
        if (visual == null || vfx == null) return;
        var sprite = TurretBossVfxSet.Frame(vfx.grenades, age * 2.5f, true);
        if (sprite == null) return;
        visual.sprite = sprite;
        visual.color = Color.white;
        visual.transform.localScale = new Vector3(.3f / Mathf.Max(.001f, sprite.bounds.size.x),
            .3f / Mathf.Max(.001f, sprite.bounds.size.y), 1);
    }

}
