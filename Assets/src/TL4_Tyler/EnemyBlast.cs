using System.Collections.Generic;
using UnityEngine;

public static class EnemyBlast
{
    // Visual placement only: preserve the actual damage origin and blast radius.
    public static Vector2 GroundVisualAnchor(Vector2 center, float radius)
    {
        foreach (var hit in Physics2D.RaycastAll(center + Vector2.up * .25f, Vector2.down,
            Mathf.Max(0, radius) + 2f, (1 << 0) | (1 << 3)))
        {
            var collider = hit.collider;
            if (collider == null || collider.isTrigger || hit.normal.y < .5f ||
                collider.GetComponentInParent<EnemyBase>() != null ||
                collider.GetComponentInParent<PlayerHealth>() != null ||
                (hit.rigidbody != null && hit.rigidbody.bodyType != RigidbodyType2D.Static)) continue;
            return new Vector2(center.x, hit.point.y);
        }
        return center;
    }

    public static bool TryGroundVisualAnchor(Vector2 center, out Vector2 anchor)
    {
        // Only nearby support counts. Never drag an airborne detonation down to a distant floor.
        anchor = GroundVisualAnchor(center, 0);
        return anchor.y <= center.y + .25f && center.y - anchor.y <= .85f && anchor != center;
    }

    public static void Detonate(Vector2 center, float radius, int damage,
        bool damageEnemies = false, EnemyBase owner = null, TurretBossVfxSet vfx = null, bool forceAir = false)
    {
        var players = new HashSet<PlayerHealth>();
        var enemies = new HashSet<EnemyBase>();
        foreach (var hit in Physics2D.OverlapCircleAll(center, radius))
        {
            if (hit == null || Physics2D.Linecast(center, hit.ClosestPoint(center), 1 << 3).collider != null)
                continue;
            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null && health.isActiveAndEnabled && health.CurrentHealth > 0 && players.Add(health))
                health.TakeDamage(damage);
            if (!damageEnemies) continue;
            var enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy != null && enemy != owner && enemy.isActiveAndEnabled && !enemy.IsDefeated && enemies.Add(enemy))
                enemy.TakeDamage(damage);
        }
        bool grounded = TryGroundVisualAnchor(center, out Vector2 groundAnchor);
        if (forceAir || !grounded)
        {
            var air = new GameObject("Air/contact explosion");
            air.transform.position = center;
            air.AddComponent<EnemyAirBurst>().Initialize(radius);
            return;
        }
        if (vfx != null && vfx.explosions != null && vfx.explosions.Length > 0)
        {
            var effect = new GameObject("Enemy explosion (shared Siege Core VFX)");
            effect.transform.position = groundAnchor;
            effect.AddComponent<TurretBossVfxBurst>().Initialize(vfx.explosions, Vector2.one * radius * 2, .65f, anchorBottom: true, transparentBottom: vfx.groundExplosionBottomInset);
            return;
        }
        var flash = new GameObject("Enemy explosion");
        flash.transform.position = center;
        var line = flash.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 32;
        line.startWidth = line.endWidth = .12f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = new Color(1, .45f, .05f);
        line.sortingOrder = 25;
        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.PI * 2 / 32;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        flash.AddComponent<EnemyBlastFlash>();
    }
}

public class EnemyBlastFlash : MonoBehaviour
{
    private float life = .25f;
    private void Update() { life -= Time.deltaTime; if (life <= 0) Destroy(gameObject); }
    private void OnDestroy()
    {
        var line = GetComponent<LineRenderer>();
        if (line != null) Destroy(line.sharedMaterial);
    }
}
