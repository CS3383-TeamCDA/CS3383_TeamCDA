#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyProjectileTests
{
    private GameObject root;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        root = new GameObject("Projectile test");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Object.Destroy(root);
        yield return null;
    }

    private GroundEnemy CreateEnemy()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GroundEnemy>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Animated.prefab");
        var enemy = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, root.transform);
        enemy.GetComponent<Rigidbody2D>().simulated = false;
        enemy.GetComponent<EnemyAttackAnimations>().enabled = false;
        return enemy;
    }

    private PlayerHealth CreatePlayer(Vector2 position, out Collider2D collider)
    {
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        player.transform.position = position;
        var health = player.AddComponent<PlayerHealth>();
        var hurtbox = new GameObject("Child hurtbox");
        hurtbox.transform.SetParent(player.transform, false);
        collider = hurtbox.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        return health;
    }

    private EnemyProjectile CreateShot(GroundEnemy owner, float speed = 0)
    {
        var shot = new GameObject("Shot").AddComponent<EnemyProjectile>();
        shot.transform.SetParent(root.transform);
        shot.Launch(owner, Vector2.right, speed, 2);
        return shot;
    }

    [UnityTest]
    public IEnumerator PlayerHitUsesSnapshotAndCannotDamageTwice()
    {
        var owner = CreateEnemy();
        var health = CreatePlayer(Vector2.right * 3, out var collider);
        var shot = CreateShot(owner);
        int damage = shot.Damage;
        owner.SetLevel(owner.Level + 1);
        int calls = 0;
        health.OnHealthChanged += (_, __) =>
        {
            calls++;
            Assert.IsFalse(shot.TryHitPlayer(collider), "Reentrant damage must be rejected.");
        };
        Assert.IsTrue(shot.TryHitPlayer(collider));
        Assert.IsFalse(shot.TryHitPlayer(collider));
        Assert.AreEqual(health.MaxHealth - damage, health.CurrentHealth);
        Assert.AreEqual(1, calls);
        yield return null;
        Assert.IsTrue(shot == null);
    }

    [UnityTest]
    public IEnumerator InvalidTargetsDoNotConsumeShot()
    {
        var owner = CreateEnemy();
        var health = CreatePlayer(Vector2.right * 3, out var collider);
        var shot = CreateShot(owner);
        Assert.IsFalse(shot.TryHitPlayer(null));
        Assert.IsFalse(shot.TryHitPlayer(owner.GetComponent<Collider2D>()));
        collider.enabled = false;
        Assert.IsFalse(shot.TryHitPlayer(collider));
        collider.enabled = true;
        health.TakeDamage(health.MaxHealth);
        Assert.IsFalse(shot.TryHitPlayer(collider));
        var other = CreatePlayer(Vector2.right * 5, out var otherCollider);
        Assert.IsTrue(shot.TryHitPlayer(otherCollider));
        Assert.Less(other.CurrentHealth, other.MaxHealth);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SweepHitsTriggerPlayerButStopsAtEarlierWall()
    {
        foreach (bool blocked in new[] { false, true })
        {
            var owner = CreateEnemy();
            var health = CreatePlayer(Vector2.right * 3, out _);
            GameObject wall = null;
            if (blocked)
            {
                wall = new GameObject("Wall");
                wall.transform.SetParent(root.transform);
                wall.layer = LayerMask.NameToLayer("Ground");
                wall.transform.position = Vector2.right;
                wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 4);
            }
            var shot = CreateShot(owner, 1000);
            int damage = shot.Damage;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.05f);
            Assert.IsTrue(shot == null);
            Assert.AreEqual(health.MaxHealth - (blocked ? 0 : damage), health.CurrentHealth);
            Object.Destroy(health.gameObject);
            Object.Destroy(owner.gameObject);
            if (wall != null) Object.Destroy(wall);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator VisibleBoltsFireFromCorrectSideTravelAndStopAtTerrain()
    {
        foreach (bool right in new[] { true, false })
        {
            var enemy = CreateEnemy();
            enemy.SetFacingRight(right);
            float direction = right ? 1 : -1;
            Assert.IsTrue(enemy.GetComponent<EnemyRangedAttack>().TryShoot(Vector2.right * direction * 10, out var shot));
            Assert.AreEqual(enemy, shot.Owner);
            Assert.Greater(shot.transform.position.x * direction, .4f, "Muzzle must be outside the enemy on the firing side.");
            var renderer = shot.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer.sprite);
            Assert.IsTrue(renderer.enabled);
            Assert.Greater(renderer.sortingOrder, enemy.GetComponent<SpriteRenderer>().sortingOrder);
            float start = shot.transform.position.x;
            yield return new WaitForSeconds(.1f);
            Assert.Greater((shot.transform.position.x - start) * direction, .4f);

            var wall = new GameObject("Blocking terrain");
            wall.transform.SetParent(root.transform);
            wall.layer = LayerMask.NameToLayer("Ground");
            wall.transform.position = shot.transform.position + Vector3.right * direction;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 4);
            yield return new WaitForSeconds(.3f);
            Assert.IsTrue(shot == null, "Projectile must stop at a thin terrain wall.");
            Object.Destroy(wall);
            Object.Destroy(enemy.gameObject);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator BoltsExpireWithoutHittingAnything()
    {
        var enemy = CreateEnemy();
        var prefab = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Projectile.prefab");
        var shot = Object.Instantiate(prefab, root.transform);
        shot.Launch(enemy, Vector2.right, 0, .1f);
        yield return new WaitForSeconds(.2f);
        Assert.IsTrue(shot == null);
    }
}
#endif
