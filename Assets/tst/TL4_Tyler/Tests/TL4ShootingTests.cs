using NUnit.Framework;
using UnityEngine;

public class TL4ShootingTests
{
    private GameObject instance;
    [SetUp] public void SetUp() { instance = new GameObject("Shooting test"); }
    [TearDown] public void TearDown() { Object.DestroyImmediate(instance); }

    [TestCase(0, 0)] [TestCase(10, 0)] [TestCase(30, 45)]
    [TestCase(45, 45)] [TestCase(70, 90)] [TestCase(90, 90)]
    [TestCase(120, 135)] [TestCase(135, 135)] [TestCase(170, 180)] [TestCase(180, 180)]
    public void TurretSnapsToClosestOfFiveAngles(float targetAngle, float expectedAngle)
    {
        var turret = instance.AddComponent<TurretHazard>();
        var origin = new Vector2(4, 3);
        Vector2 offset = new Vector2(Mathf.Cos(targetAngle * Mathf.Deg2Rad), Mathf.Sin(targetAngle * Mathf.Deg2Rad));
        // Avoid floating-point sin(180) being just below the horizontal boundary.
        if (targetAngle == 180) offset = Vector2.left;
        Assert.IsTrue(turret.TryGetShotDirection(origin, origin + offset * 10, out var direction));
        Assert.That(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, Is.EqualTo(expectedAngle).Within(.001f));
        Assert.That(direction.magnitude, Is.EqualTo(1).Within(.0001f));
    }

    [Test] public void TurretRejectsBelowArcAndCoincidentTargets()
    {
        var turret = instance.AddComponent<TurretHazard>();
        Assert.IsFalse(turret.TryGetShotDirection(Vector2.zero, new Vector2(3, -1), out _));
        Assert.IsFalse(turret.TryGetShotDirection(Vector2.zero, Vector2.zero, out _));
    }

    [Test] public void GroundShotsStayHorizontalWithoutTurningTowardTarget()
    {
        var ground = instance.AddComponent<GroundEnemy>();
        ground.SetFacingRight(true);
        Assert.IsTrue(ground.TryGetShotDirection(Vector2.zero, new Vector2(-4, 10), out var right));
        Assert.AreEqual(Vector2.right, right);
        ground.SetFacingRight(false);
        Assert.IsTrue(ground.TryGetShotDirection(Vector2.zero, new Vector2(4, -10), out var left));
        Assert.AreEqual(Vector2.left, left);
    }

    [Test] public void GroundFacingFollowsSpriteFlipAndMirroredTransform()
    {
        var sprite = instance.AddComponent<SpriteRenderer>();
        var ground = instance.AddComponent<GroundEnemy>();
        sprite.flipX = true;
        Assert.IsFalse(ground.FacingRight);
        instance.transform.localScale = new Vector3(-1, 1, 1);
        Assert.IsTrue(ground.FacingRight);
        ground.SetFacingRight(false);
        Assert.IsFalse(ground.FacingRight);
    }

    [Test] public void MeleeOnlyAndDefeatedEnemiesCannotFire()
    {
        var ground = instance.AddComponent<GroundEnemy>();
        ground.SetAttackMode(EnemyAttackMode.MeleeOnly);
        Assert.IsFalse(ground.TryGetShotDirection(Vector2.zero, Vector2.right, out _));
        ground.SetAttackMode(EnemyAttackMode.RangedOnly);
        Assert.IsTrue(ground.TryGetShotDirection(Vector2.zero, Vector2.right, out _));
        ground.TakeDamage(ground.MaxHealth);
        Assert.IsFalse(ground.TryGetShotDirection(Vector2.zero, Vector2.right, out _));
    }

    [Test] public void ShooterUsesMuzzleDirectionScaledDamageAndCooldown()
    {
        var ground = instance.AddComponent<GroundEnemy>();
        ground.SetLevel(3);
        ground.SetFacingRight(false);
        var shooter = instance.AddComponent<EnemyRangedAttack>();
        var templateObject = new GameObject("Projectile template");
        var muzzleObject = new GameObject("Muzzle");
        EnemyProjectile shot = null;
        try
        {
            shooter.SetProjectilePrefab(templateObject.AddComponent<EnemyProjectile>());
            muzzleObject.transform.position = new Vector3(2, 3, 0);
            shooter.SetMuzzle(muzzleObject.transform);
            Assert.IsTrue(shooter.TryShoot(new Vector2(10, 20), out shot));
            Assert.AreEqual(Vector2.left, shot.Direction);
            Assert.AreEqual(12, shot.Damage);
            Assert.AreSame(ground, shot.Owner);
            Assert.AreEqual(muzzleObject.transform.position, shot.transform.position);
            Assert.IsFalse(shooter.TryShoot(Vector2.right, out _));
        }
        finally
        {
            if (shot != null) Object.DestroyImmediate(shot.gameObject);
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(muzzleObject);
        }
    }
}
