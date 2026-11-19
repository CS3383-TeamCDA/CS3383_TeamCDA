
using NUnit.Framework;
using UnityEngine;

public class PlayerProjectileTests
{
    private GameObject _projectile;
    private PlayerProjectile _script;
    private Rigidbody2D _rb;

    [SetUp]
    public void SetUp()
    {
        _projectile = new GameObject("TestProjectile");

        _rb = _projectile.AddComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;

        _script = _projectile.AddComponent<PlayerProjectile>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_projectile);
    }

    [Test]
    public void ProjectileStartsWithZeroVelocity()
    {
        Assert.AreEqual(Vector2.zero, _rb.linearVelocity);
    }

    [Test]
    public void ProjectileMovesInCorrectDirection()
    {
        _script.Initialize(Vector2.right);

        Assert.Greater(_rb.linearVelocity.x, 0f);
        Assert.AreEqual(0f, _rb.linearVelocity.y, 0.001f);
    }

    [Test]
    public void ProjectileSpeedIsNormalized()
    {
        _script.Initialize(new Vector2(3f, 4f));

        Assert.AreEqual(
            30f,
            _rb.linearVelocity.magnitude,
            0.001f
        );
    }

    [Test]
    public void DiagonalProjectileIsNotFaster()
    {
        _script.Initialize(Vector2.right);
        float horizontalSpeed = _rb.linearVelocity.magnitude;

        _script.Initialize(new Vector2(1f, 1f));
        float diagonalSpeed = _rb.linearVelocity.magnitude;

        Assert.AreEqual(
            horizontalSpeed,
            diagonalSpeed,
            0.001f
        );
    }

    [TestCase(1f, 0f)]
    [TestCase(1f, 1f)]
    [TestCase(0f, 1f)]
    [TestCase(-1f, 1f)]
    [TestCase(-1f, 0f)]
    [TestCase(-1f, -1f)]
    [TestCase(0f, -1f)]
    [TestCase(1f, -1f)]
    public void ProjectileTravelsInAllEightDirections(
        float x,
        float y)
    {
        Vector2 direction = new Vector2(x, y);

        _script.Initialize(direction);

        Vector2 expectedVelocity = direction.normalized * 30f;

        Assert.Less(
            Vector2.Distance(_rb.linearVelocity, expectedVelocity),
            0.001f
        );
    }

    [TestCase(1f, 0f, 0f)]
    [TestCase(1f, 1f, 45f)]
    [TestCase(0f, 1f, 90f)]
    [TestCase(-1f, 1f, 135f)]
    [TestCase(-1f, 0f, 180f)]
    [TestCase(-1f, -1f, 225f)]
    [TestCase(0f, -1f, 270f)]
    [TestCase(1f, -1f, 315f)]
    public void ProjectileRotatesToMatchDirection(
        float x,
        float y,
        float expectedAngle)
    {
        _script.Initialize(new Vector2(x, y));

        float actualAngle =
            _projectile.transform.eulerAngles.z;

        Assert.Less(
            Mathf.Abs(Mathf.DeltaAngle(actualAngle, expectedAngle)),
            0.001f
        );
    }

    [Test]
    public void ZeroDirectionProducesZeroVelocity()
    {
        _script.Initialize(Vector2.zero);

        Assert.AreEqual(Vector2.zero, _rb.linearVelocity);
    }

    [Test]
    public void ReinitializingProjectileUpdatesVelocity()
    {
        _script.Initialize(Vector2.right);

        Assert.Greater(_rb.linearVelocity.x, 0f);

        _script.Initialize(Vector2.left);

        Assert.Less(_rb.linearVelocity.x, 0f);
        Assert.AreEqual(0f, _rb.linearVelocity.y, 0.001f);
    }
}
