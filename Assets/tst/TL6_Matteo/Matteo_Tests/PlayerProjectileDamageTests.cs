using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s.Contracts;

// A fake enemy that only implements our shared contract.
public class ProjectileDamageTestEnemy : MonoBehaviour, IEnemyDamageable
{
    public int DamageReceived { get; private set; }
    public int HitCount { get; private set; }

    public bool TakeDamage(int amount)
    {
        DamageReceived += amount;
        HitCount++;
        return DamageReceived >= 100;
    }
}

public class PlayerProjectileDamageTests
{
    private readonly List<GameObject> _objects = new();

    // Keep test objects away from existing scene geometry.
    private static readonly Vector2 Origin =
        new Vector2(1000f, 1000f);

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (GameObject obj in _objects)
        {
            if (obj != null)
                Object.Destroy(obj);
        }

        _objects.Clear();
        yield return null;
    }

    private GameObject Track(GameObject obj)
    {
        _objects.Add(obj);
        return obj;
    }

    private PlayerProjectile CreateProjectile(
        int bonusDamage = 0)
    {
        GameObject obj = Track(
            new GameObject("Test Projectile")
        );

        obj.transform.position = Origin;

        int layer = LayerMask.NameToLayer("PlayerProjectile");
        if (layer >= 0)
            obj.layer = layer;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider =
            obj.AddComponent<CircleCollider2D>();

        collider.isTrigger = true;
        collider.radius = 0.15f;

        PlayerProjectile projectile =
            obj.AddComponent<PlayerProjectile>();

        projectile.Initialize(Vector2.right, bonusDamage);

        return projectile;
    }

    private ProjectileDamageTestEnemy CreateEnemy(float x)
    {
        GameObject obj = Track(
            new GameObject("Test Enemy")
        );

        obj.transform.position =
            Origin + Vector2.right * x;

        ProjectileDamageTestEnemy enemy =
            obj.AddComponent<ProjectileDamageTestEnemy>();

        GameObject hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(obj.transform, false);

        BoxCollider2D collider =
            hitbox.AddComponent<BoxCollider2D>();

        collider.size = new Vector2(1.5f, 1.5f);

        return enemy;
    }

    private IEnumerator WaitForPhysics()
    {
        for (int i = 0; i < 8; i++)
            yield return new WaitForFixedUpdate();

        // Allow deferred Destroy calls to finish.
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProjectileDealsBaseDamage()
    {
        var enemy = CreateEnemy(1f);
        CreateProjectile();

        yield return WaitForPhysics();

        Assert.AreEqual(10, enemy.DamageReceived);
        Assert.AreEqual(1, enemy.HitCount);
    }

    [UnityTest]
    public IEnumerator ProjectileAppliesBonusDamage()
    {
        var enemy = CreateEnemy(1f);

        CreateProjectile(bonusDamage: 5);

        yield return WaitForPhysics();

        Assert.AreEqual(15, enemy.DamageReceived);
    }

    [UnityTest]
    public IEnumerator ProjectileHitsEnemyThroughChildCollider()
    {
        var enemy = CreateEnemy(1f);
        CreateProjectile();

        yield return WaitForPhysics();

        Assert.AreEqual(10, enemy.DamageReceived);
    }

    [UnityTest]
    public IEnumerator ProjectileOnlyDamagesEnemyOnce()
    {
        var enemy = CreateEnemy(1f);

        // Add another overlapping hitbox.
        var hitbox = enemy.GetComponentInChildren<BoxCollider2D>();
        hitbox.gameObject.AddComponent<BoxCollider2D>();

        CreateProjectile();

        yield return WaitForPhysics();

        Assert.AreEqual(1, enemy.HitCount);
        Assert.AreEqual(10, enemy.DamageReceived);
    }

    [UnityTest]
    public IEnumerator ProjectileIgnoresNonDamageableObjects()
    {
        GameObject obstacle = Track(
            new GameObject("Non Damageable")
        );

        obstacle.transform.position =
            Origin + Vector2.right;

        BoxCollider2D collider =
            obstacle.AddComponent<BoxCollider2D>();

        collider.size = new Vector2(1f, 1f);

        var projectile = CreateProjectile();

        yield return WaitForPhysics();

        Assert.IsTrue(projectile != null);
    }

    [UnityTest]
    public IEnumerator ProjectileDestroyedWhenHittingGround()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");

        if (groundLayer < 0)
            Assert.Ignore("Ground layer not configured.");

        GameObject ground = Track(
            new GameObject("Test Ground")
        );

        ground.layer = groundLayer;
        ground.transform.position =
            Origin + Vector2.right;

        BoxCollider2D collider =
            ground.AddComponent<BoxCollider2D>();

        collider.size = new Vector2(1.5f, 1.5f);

        var projectile = CreateProjectile();

        yield return WaitForPhysics();

        Assert.IsTrue(projectile == null);
    }
}
