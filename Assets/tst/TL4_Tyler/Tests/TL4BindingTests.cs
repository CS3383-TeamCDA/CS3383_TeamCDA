using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TL4BindingTests
{
    private GameObject spawnerObject;
    private EnemyBase enemy1;
    private EnemyBase enemy2;

    [SetUp]
    public void SetUp()
    {
        spawnerObject = new GameObject("Test Enemy Spawner");
        EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
        spawner.SetSpawnRate(5.0f);

        enemy1 = spawner.CreateEnemy(EnemyType.Ground);
        enemy1.name = "Enemy 1";
        enemy2 = spawner.CreateEnemy(EnemyType.Flying);
        enemy2.name = "Enemy 2";
    }

    [TearDown]
    public void TearDown()
    {
        if (enemy1 != null)
        {
            Object.DestroyImmediate(enemy1.gameObject);
        }

        if (enemy2 != null)
        {
            Object.DestroyImmediate(enemy2.gameObject);
        }

        if (spawnerObject != null)
        {
            Object.DestroyImmediate(spawnerObject);
        }
    }

    [Test]
    public void BaseRef_ToDifferentEnemy_AttackUsesCurrentEnemy()
    {
        EnemyBase current = enemy1;
        Assert.AreEqual("Enemy 1 attacks", current.Attack());

        current = enemy2;
        Assert.AreEqual("Enemy 2 attacks", current.Attack());

        current = enemy1;
        Assert.AreEqual("Enemy 1 attacks", current.Attack());
    }

    [Test]
    public void BaseRef_ToSameEnemy_AttackDoesNotChange()
    {
        EnemyBase baseline = enemy1;
        EnemyBase current = enemy1;

        Assert.AreEqual("Enemy 1 attacks", current.Attack());
        Assert.AreEqual(baseline.Attack(), current.Attack());
    }

    [Test]
    public void DynamicBinding_BaseReference_UsesSubclassOverride()
    {
        EnemyBase current = spawnerObject.AddComponent<TL4OverrideEnemy>();

        Assert.AreEqual("Override attack", current.Attack());
        Assert.AreNotEqual(spawnerObject.name + " attacks", current.Attack());
    }

    [Test]
    public void DynamicBinding_HiddenMethod_DoesNotOverrideBaseMethod()
    {
        TL4HiddenAttackEnemy derived = spawnerObject.AddComponent<TL4HiddenAttackEnemy>();
        EnemyBase current = derived;

        Assert.AreEqual("Hidden attack", derived.Attack());
        Assert.AreEqual(spawnerObject.name + " attacks", current.Attack());
    }

    [Test]
    public void DynamicBinding_FailDemo_ExpectsBaseInsteadOfOverride()
    {
        EnemyBase current = spawnerObject.AddComponent<TL4OverrideEnemy>();

        // Intentionally wrong: the runtime type selects the override, even through EnemyBase.
        Assert.AreEqual(spawnerObject.name + " attacks", current.Attack());
    }
    [TestCase(EnemyType.Ground, typeof(GroundEnemy), "Ground movement")]
    [TestCase(EnemyType.Flying, typeof(FlyingEnemy), "Flying movement")]
    [TestCase(EnemyType.Turret, typeof(TurretHazard), "Turret stationary")]
    [TestCase(EnemyType.Aim, typeof(AimEnemy), "Aim targeting")]
    public void Factory_CreatesSubtype_AndMoveUsesOverride(EnemyType type, System.Type expectedType, string message)
    {
        EnemyBase enemy = spawnerObject.GetComponent<EnemySpawner>().CreateEnemy(type);
        try
        {
            Assert.AreEqual(expectedType, enemy.GetType());
            LogAssert.Expect(LogType.Log, message);
            enemy.Move();
        }
        finally
        {
            Object.DestroyImmediate(enemy.gameObject);
        }
    }

    [Test]
    public void TakeDamage_ReportsDefeatTypeAndPosition_OnlyOnce()
    {
        EnemySpawner spawner = spawnerObject.GetComponent<EnemySpawner>();
        int notifications = 0;
        EnemyType reportedType = EnemyType.Turret;
        Vector3 reportedPosition = Vector3.zero;
        enemy1.transform.position = new Vector3(2, 3, 0);
        spawner.OnEnemyDefeated += (type, position) =>
        {
            notifications++;
            reportedType = type;
            reportedPosition = position;
        };

        Assert.IsFalse(enemy1.TakeDamage(-10));
        Assert.IsFalse(enemy1.TakeDamage(99));
        Assert.AreEqual(0, notifications);
        Assert.IsTrue(enemy1.TakeDamage(1));
        Assert.IsTrue(enemy1.TakeDamage(1));
        Assert.AreEqual(1, notifications);
        Assert.AreEqual(EnemyType.Ground, reportedType);
        Assert.AreEqual(enemy1.transform.position, reportedPosition);
    }

    [Test]
    public void Factory_RejectsUnknownEnemyType()
    {
        EnemySpawner spawner = spawnerObject.GetComponent<EnemySpawner>();
        Assert.Throws<System.ArgumentOutOfRangeException>(() => spawner.CreateEnemy((EnemyType)999));
    }
}
